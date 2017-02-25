using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace hypercube
{

    public enum touchScreenOrientation
    {
        INVALID_TOUCHSCREEN = -1,

        //if a device has multiple touch screens, this is the way to distinguish between them, and give appropriate world coords
        FRONT_TOUCHSCREEN = 0,
        BACK_TOUCHSCREEN,
        LEFT_TOUCHSCREEN, //relative to the device itself
        RIGHT_TOUCHSCREEN,
        TOP_TOUCHSCREEN,
        BOTTOM_TOUCHSCREEN,

        OTHER_TOUCHSCREEN_1, //possible non-space related screens
        OTHER_TOUCHSCREEN_2
    }


    public class rawTouchData
    {
        public int id = 0;
        public touchScreenOrientation o;
        public float x = 0;
        public float y = 0;
        public uint iX = 0;
        public uint iY = 0;
    }


public class touchScreenInputManager  : streamedInputManager
{

    touchScreen[] touchScreens = null;
    public touchScreen getPanel(touchScreenOrientation p)
    {
        int intP = (int)p;
        if (intP < 0 || intP >= touchScreens.Length)
            return null;

        return touchScreens[intP];
    }
    public touchScreen front { get {return touchScreens[0]; } } //quick access
    public touchScreen back { get {return touchScreens[1]; } }

    public touchScreen this[int screenIndex] //index access to a particular touch screen
    {
        get
        {
            if (screenIndex < 0 || screenIndex >= touchScreens.Length)
                return null;
            return touchScreens[screenIndex];
        }
    }
    public touchScreen this[touchScreenOrientation o] //index access to a particular touch screen
    {
        get
        {
            return getPanel(o);
        }
    }

    //easy accessors to the data of screen 0, which will be what is used 95% of the time.
    public touch[] touches { get {return touchScreens[0].touches;}}
    public uint touchCount { get {return touchScreens[0].touchCount;}}
    public Vector2 averagePos { get {return touchScreens[0].averagePos;}} //The 0-1 normalized average position of all touches on touch screen 0
    public Vector2 averageDiff { get {return touchScreens[0].averageDiff;}} //The normalized distance the touch moved 0-1 on touch screen 0
    public Vector2 averageDist { get {return touchScreens[0].averageDist;}} //The distance the touch moved, in Centimeters  on touch screen 0
    public float twist { get {return touchScreens[0].twist;}}
    public float pinch { get {return touchScreens[0].pinch;}}//0-1
    public float touchSize { get {return touchScreens[0].touchSize;}} //0-1
    public float touchSizeCm { get {return touchScreens[0].touchSizeCm;}}


#if HYPERCUBE_INPUT
	float projectionWidth = 20f; //the physical size of the projection, in centimeters
	float projectionHeight = 12f;
	float projectionDepth = 20f;

    static readonly byte[] emptyByte = new byte[] { 0 };

    //constructor
    public touchScreenInputManager( SerialController _serial) : base(_serial, new byte[]{255,255}, 100000)
    {
        touchScreens = new touchScreen[8]; 
        for (int t = 0; t < touchScreens.Length; t++)
        {
            touchScreens[t] = new touchScreen((touchScreenOrientation)t);
        }      
    }

    public void setTouchScreenDims(dataFileDict d)
    {
        if (d == null)
            return;

        if (!d.hasKey("projectionCentimeterWidth") ||
            !d.hasKey("projectionCentimeterHeight") ||
            !d.hasKey("projectionCentimeterDepth") 
            )
            Debug.LogWarning("Volume config file lacks touch screen hardware specs!"); //these must be manually entered, so we should warn if they are missing.


        projectionWidth = d.getValueAsFloat("projectionCentimeterWidth", projectionWidth);
        projectionHeight = d.getValueAsFloat("projectionCentimeterHeight", projectionHeight);
        projectionDepth = d.getValueAsFloat("projectionCentimeterDepth", projectionDepth);

        touchScreens[0]._init(0, d, projectionWidth, projectionHeight); //front
        touchScreens[1]._init(1, d, projectionWidth, projectionHeight); //back
        touchScreens[2]._init(2, d, projectionDepth, projectionHeight); //left
        touchScreens[3]._init(3, d, projectionDepth, projectionHeight); //right
        touchScreens[4]._init(4, d, projectionWidth, projectionDepth); //top
        touchScreens[5]._init(5, d, projectionWidth, projectionDepth); //bottom
        touchScreens[6]._init(6, d, 0f, 0f);
        touchScreens[7]._init(7, d, 0f, 0f);
    }

     public override void update(bool debug)
    {
        if (!serial)
            return;

        string data = serial.ReadSerialMessage();
        while (data != null && data != "")
        {
            if (debug)
                Debug.Log("touchScreenInputMgr: "+ data);


            addData(System.Text.Encoding.Unicode.GetBytes(data));
            data = serial.ReadSerialMessage();
        }

        for (int t = 0; t < touchScreens.Length; t++)
        {
            if (touchScreens[t].active)
                touchScreens[t].postProcessData();
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
     
        uint touchScreenId = dataChunk[0]; //if a device has multiple screens, this is how we distinguish them. (this also tells us the orientation of the given screen)
        uint totalTouches = dataChunk[1];

        if (dataChunk.Length != (totalTouches * 5) + 2)  //unexpected chunk length! Assume it is corrupted, and dump it.
        return;

        //assume no one is touched.
        //this code assumes that touches for all touch screens are sent together during every frame (not a Unity frame, but a hardware frame).
        //as opposed to touch panel A sends something frame 1, then panel B sends it's own thing the next frame.  This will not work.
        for (int t = 0; t < touchScreens.Length; t++)
        {
            if (touchScreens[t].active)
                touchScreens[t].prepareNextFrame();
        }

        rawTouchData d = new rawTouchData();

        for (int i = 2; i < dataChunk.Length; i = i + 5) //start at 1 and jump 5 each time.
        {
            d.id = dataChunk[i];
            d.o = (touchScreenOrientation) touchScreenId;
            d.iX = System.BitConverter.ToUInt16(dataChunk, i + 1);
            d.iY = System.BitConverter.ToUInt16(dataChunk, i + 3);
            d.x = (float)d.iX;
            d.y = (float)d.iY;

            touchScreens[touchScreenId]._interface(d);
        }

    }


#endif



    }



}
