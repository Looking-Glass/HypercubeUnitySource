using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace hypercube
{

public class touchScreenInputManager  : streamedInputManager
{
    //current touches, updated every frame
    public touch[] touches { get; private set; } 
    public uint touchCount { get; private set; }
    

#if HYPERCUBE_INPUT
  public readonly string deviceName;

    //is this the front touch screen?
    public readonly bool isFront;

    //we don't know the architecture of software that will use this code. So I chose a pool instead of a factory pattern here to avoid affecting garbage collection in any way 
    //at the expense of a some kb of memory (touchPoolSize * sizeof(touch)).  The problem with using a pool, is that a pointer to a touch might be held, meanwhile it gets recycled by touchScreenInputManager 
    //to represent a new touch.  To avoid this, I implemented the 'destroyed' state on touches, and when any access occurs on it while 'destroyed', the touch throws an error.  
    //The other part of trying to keep touches difficult to misuse is to make the poolSize large enough so that some time will pass before recycling, 
    //giving any straggling pointers to touches a chance to throw those errors and let the dev know that their design needs change.
    //so in short: DON'T HOLD POINTERS TO THE TOUCHES FOR MORE THAN THE UPDATE
    const int touchPoolSize = 128; 
    touch[] touchPool = new touch[touchPoolSize];
    int touchPoolItr = 0;

    touchInterface[] interfaces = new touchInterface[touchPoolSize]; //these are used to update the touches internally, allowing us to expose all data but not any controls to anything outside of this class or the touch class.

    const int maxTouches = 12;
    int[] touchIdMap = new int[maxTouches];  //this maps the touchId coming from hardware to its arrayPosition in the touchPool;
    System.UInt16 currentTouchID = 0; //strictly for external convenience

 
    //external interface..
    public Vector2 averagePos { get; private set; } //0-1
    public Vector2 averageDiff { get; private set; } //0-1
    public Vector2 averageDist {get;private set;} //in centimeters

    public float twist {get;private set;}
    public float pinch { get; private set; }//0-1
    public float touchSize {get;private set;} //the ave distance between the farthest 2 touches in 1 axis, in centimeters
    private float lastSize = 0f; //0-1

    public Vector3 getAverageTouchWorldPos(hypercubeCamera c) { return c.transform.TransformPoint(getAverageTouchLocalPos()); }
    public Vector3 getAverageTouchLocalPos()
    {
          if (isFront)
                return new Vector3(averagePos.x + .5f, averagePos.y + .5f, -.5f);
            else
                return new Vector3((1f - averagePos.x) + .5f, averagePos.y + .5f, .5f);
    }

    //these variables map the raw touches into coherent positional data that is consistent across devices.
    float screenResX = 800f; //these are not public, as the touchscreen res can vary from device to device.  We abstract this for the dev as 0-1.
    float screenResY = 450f;
    float projectionWidth = 20f; //the physical size of the projection, in centimeters
    float projectionHeight = 12f;
    float touchScreenWidth = 0f; // physical size of the touchscreen, in centimeters
    float touchScreenHeight = 0f;

    float widthOffset = 0f; //any difference between the physical centers of the touchscreen and projection
    float heightOffset = 0f;  //set as touchScreen - projection

    float touchAspectX = 1f; //screenSizeX / touchScreenSizeX
    float touchAspectY = 1f;

    static readonly byte[] emptyByte = new byte[] { 0 };

    public touchScreenInputManager(string _deviceName, SerialController _serial, bool _isFrontTouchScreen) : base(_serial, new byte[]{255,255}, 1024)
    {
        touches = null;
        touchCount = 0;
        deviceName = _deviceName;
        isFront = _isFrontTouchScreen;

        for (int i = 0; i < touchPool.Length; i++ )
        {
            touchPool[i] = new touch(isFront);
        }
        for (int i = 0; i < interfaces.Length; i++)
        {
            interfaces[i] = new touchInterface();
        }
        for (int i = 0; i < touchIdMap.Length; i++)
        {
            touchIdMap[i] = 0;
        }

    }

    public void setTouchScreenDims(float _resX, float _resY, float _projectionWidth, float _projectionHeight, float _touchScreenWidth, float _touchScreenHeight, float _widthOffset, float _heightOffset)
    {
        screenResX = _resX;
        screenResY = _resY;
        projectionWidth = _projectionWidth;
        projectionHeight = _projectionHeight;
        touchScreenWidth = _touchScreenWidth;
        touchScreenHeight = _touchScreenHeight;
        widthOffset = _widthOffset;
        heightOffset = _heightOffset;

        touchAspectX =  touchScreenWidth / projectionWidth;
        touchAspectY = projectionHeight / touchScreenHeight;
    }

    public override void update(bool debug)
    {
            string data = serial.ReadSerialMessage();
            while (data != null)
            {
                if (debug)
                    Debug.Log(deviceName +": "+ data);

                if (serial.readDataAsString)
                {
                    if (data == "init:done" || data.Contains("init:done"))
                    {
                        serial.readDataAsString = false; //start capturing data
                        Debug.Log(deviceName + " is ready and initialized.");
                    }

                    return; //still initializing
                }

                //byte[] bytes = new byte[data.Length * sizeof(char)];
                //System.Buffer.BlockCopy(data.ToCharArray(), 0, bytes, 0, bytes.Length);
                //addData(bytes); //inherited from base class. Will process our data given the delimiter.
                addData(System.Text.Encoding.Unicode.GetBytes(data));
     
                data = serial.ReadSerialMessage();
            }
    }


    protected override void processData(byte[] dataChunk)
    {
        /*  the expected data here is ..
         * 1 byte = total touches
         * 
         * 1 byte = touch id
         * 2 bytes = touch x
         * 2 bytes = touch y
         * 
         *  1 byte = touch id for next touch  (optional)
         *  ... etc
         *  
         * */

        if (dataChunk == emptyByte)
            return;

        uint totalTouches = dataChunk[0];

        if (dataChunk.Length != (totalTouches * 5) + 1)  //unexpected chunk length! Assume it is corrupted, and dump it.
            return;

        //assume no one is touched.
        for (int i = 0; i < touchPoolSize; i++)
        {
            interfaces[i].active = false;
        }     

        float x = 0;
        float y = 0;
        
        touchCount = 0; 
        for (int i = 1; i < dataChunk.Length; i= i + 5) //start at 1 and jump 5 each time.
        {
            int id = dataChunk[i];
            x = (float)System.BitConverter.ToUInt16(dataChunk, i + 1);
            y = (float)System.BitConverter.ToUInt16(dataChunk, i + 3);

            //sometimes the hardware sends us funky data.
            //if the stats are funky, throw it out.
            if (id == 0 || id >= maxTouches)
                continue; 
            if (x < 0 || x > screenResX)
                continue;
            if (y < 0 || y > screenResY)
                continue;
        
            //is this a new touch?  If so, assign it to a new item in the pool, and update our iterators.
            if (touchPool[touchIdMap[id]].state < touch.activationState.ACTIVE ) //a new touch!  Point it to a new element in our touchPool  (we know it is new because the place where the itr is pointing to is deactivated. Hence, it must have gone through at least 1 frame where no touch info was received for it.)
            {             
                touchIdMap[id] = touchPoolItr; //point the id to the current iterator 

                currentTouchID++;
                interfaces[touchIdMap[id]]._id = currentTouchID; //this id, is purely for external convenience and does not affect our functions here.

                touchPoolItr++;
                if (touchPoolItr >= touchPoolSize)
                    touchPoolItr = 0;
            }

            touchCount++;
            interfaces[touchIdMap[id]].active = true;

            interfaces[touchIdMap[id]].normalizedPos.x =
                touchAspectX * ((x / screenResX)  //mapping if the projection is centered with the touchscreen (including the * touchAspectX)
                + (widthOffset/touchScreenWidth))  //physical offset between the center of the touchscreen and the projection
                ;
            interfaces[touchIdMap[id]].normalizedPos.y = 
                ((y / screenResY) 
                +(heightOffset / touchScreenHeight)) * touchAspectY
                ;

            interfaces[touchIdMap[id]].physicalPos.x = (x / screenResX) * touchScreenWidth;
            interfaces[touchIdMap[id]].physicalPos.y = (y / screenResY) * touchScreenHeight;
        }

        float averageDiffX = 0f;
        float averageDiffY = 0f;
        float averageDistX = 0f;
        float averageDistY = 0f;
        float averagePosX = 0f;
        float averagePosY = 0f;

        touchInterface highX = null;
        touchInterface lowX = null;
        touchInterface highY = null;
        touchInterface lowY = null;

        //apply all,  notify touchScreenTargets, and do post processing on touches
        touches = new touch[touchCount];
        int t = 0;
        for (int i = 0; i < touchPoolSize; i++)
        {

            touchPool[i]._interface(interfaces[i]); //update the touch.          
            if (interfaces[i].active)
            {
                touches[t] = touchPool[i];//these are the touches that can be queried from hypercube.input.front.touches
                t++;

                averagePosX += touchPool[i].posX;
                averagePosY += touchPool[i].posY;
                averageDiffX += touchPool[i].diffX;
                averageDiffY += touchPool[i].diffY;
                averageDistX += touchPool[i].distX;
                averageDistY += touchPool[i].distY;

                if (highX == null || interfaces[i].physicalPos.x > highX.physicalPos.x)
                    highX = interfaces[i];
                if (lowX == null || interfaces[i].physicalPos.x < lowX.physicalPos.x)
                    lowX = interfaces[i];
                if (highY == null || interfaces[i].physicalPos.y > highY.physicalPos.y)
                    highY = interfaces[i];
                if (lowY == null || interfaces[i].physicalPos.y < lowY.physicalPos.y)
                    lowY = interfaces[i];
            }           
        }

        twist = 0f;
        if (touchCount < 2)
        {
            touchSize = 0f;           
            pinch = 0f;
            averageDiff = averageDist = Vector2.zero;
            if (touchCount == 0)
                averagePos = Vector2.zero;
            else //only 1 touch
                averagePos.Set(averagePosX, averagePosY); //these will contain the correct value, since only 1 was added.
        }
        else
        {
            averagePos = new Vector2(averagePosX / (float)touchCount, averagePosY / (float)touchCount);
            averageDiff = new Vector2(averageDiffX / (float)touchCount, averageDiffY / (float)touchCount);
            averageDist = new Vector2(averageDistX / (float)touchCount, averageDistY / (float)touchCount);

            //touchSize / pinch
            touchSize = (highX.physicalPos.x - lowX.physicalPos.x) > (highY.physicalPos.y - lowY.physicalPos.y) ? highX.getPhysicalDistance(lowX) : highY.getPhysicalDistance(lowY); //use the bigger of the two differences, and then use the true distance
            if (lastSize == 0f)
                pinch = 0f;
            else
                pinch = touchSize / lastSize;   
        }

        lastSize = touchSize;


        //twist  ... this is only possible to calculate after we have the ave position or touch size, so we do 1 more itr here
        foreach (touch tch in touches)
        {
            if (tch.posY > averagePos.y)
                twist += tch.diffX;
            else
                twist -= tch.diffX;

            if (tch.posX > averagePos.x)
                twist += tch.diffY;
            else
                twist -= tch.diffY;
        }
        //turn twist into a proper degree of rotation rather than just a distance of average rotational movement
        twist = Vector2.Angle(Vector2.zero, new Vector2(touchSize, twist));

        //finally send off the events.
        foreach (touch tch in touches)
        {
            input._processTouchScreenEvent(tch);
        }
    }

#endif
}

}
