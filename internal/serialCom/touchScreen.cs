using UnityEngine;
using System.Collections;

//this is a helper class for touchScreenInputManager
//if a device has multiple touchscreens, all the input comes through the single serial port to the touchScreenInputManager
//touchScreenInputManager then uses this class to isolate off input from the different touch screens to manage them.

namespace hypercube
{

    public class touchScreen
    {
        public readonly touchScreenOrientation screenOrientation;

        //current touches, updated every frame
        public touch[] touches {get; private set;}
        public uint touchCount {get; private set;}
        public bool active {get; private set;} //inactive touchScreen simply means it is skipped over in all iteration cycles


        //external interface..
        public Vector2 averagePos {get; private set;} //The 0-1 normalized average position of all touches
        public Vector2 averageDiff {get; private set;} //The normalized distance the touch moved 0-1
        public Vector2 averageDist {get; private set;} //The distance the touch moved, in Centimeters


        public float twist {get; private set;}
        public float pinch {get; private set;}//0-1
        public float touchSize {get; private set;} //0-1
        public float touchSizeCm {get; private set;} //the ave distance between the farthest 2 touches in 1 axis, in centimeters


        //these variables map the raw touches into coherent positional data that is consistent across devices.
        float screenResX = 800f; //these are not public because the touchscreen res can vary from device to device.  We abstract this for the dev as 0-1.
        float screenResY = 450f;
        float touchScreenWidth = 20f; // physical size of the touchscreen, in centimeters
        float touchScreenHeight = 12f;
        float topLimit = 0f;
        float bottomLimit = 1f;
        float leftLimit = 0f;
        float rightLimit = 1f;

        public touchScreen(touchScreenOrientation _orientation)
        {
            touches = new touch[0];
            touchCount = 0;
            pinch = 1f;
            twist = 0f;
            screenOrientation = _orientation;

            active = false; //the first input causes it to activate.  

            for (int i = 0; i < touchPool.Length; i++)
            {
                touchPool[i] = new touch();
            }
            for (int i = 0; i < interfaces.Length; i++)
            {
                interfaces[i] = new touchInterface();
                interfaces[i].orientation = _orientation;
            }
            for (int i = 0; i < touchIdMap.Length; i++)
            {
                touchIdMap[i] = 0;
            }
        }


        touchInterface mainTouchA = null; //instead of dynamically searching for touches with each update of data, we try to reuse the same ones across frames so that pinch and twist are less jittery
        touchInterface mainTouchB = null;

        //we don't know what kind of software architecture will use this code. So I chose a pool instead of a factory pattern here to avoid affecting garbage collection in any way 
        //at the expense of a some kb of memory (touchPoolSize * sizeof(touch)).  The problem with using a pool, is that a pointer to a touch might be held, meanwhile it gets recycled by touchScreenInputManager 
        //to represent a new touch.  To avoid this, I implemented the 'destroyed' state on touches, and when any access occurs on it while 'destroyed', the touch throws an error.  
        //The other part of trying to keep touches difficult to misuse is to make the poolSize large enough so that some time will pass before recycling, 
        //giving any straggling pointers to touches a chance to throw those errors and let the dev know that their design needs change.
        //so in short: DON'T HOLD POINTERS TO THE TOUCHES FOR MORE THAN THE CURRENT UPDATE
        const int touchPoolSize = 128;
        touch[] touchPool = new touch[touchPoolSize];
        int touchPoolItr = 1; //index 0 is forever an inactive touch.

        touchInterface[] interfaces = new touchInterface[touchPoolSize]; //these are used to update the touches internally, allowing us to expose all data but not any controls to anything outside of this class or the touch class.

        const int maxTouches = 12;
        //Dictionary<int, int> touchIdMap = new Dictionary<int, int>(); //dictionary instead of array prevents duplicate entries
        int[] touchIdMap = new int[maxTouches];  //this maps the touchId coming from hardware to its arrayPosition in the touchPool;
        System.UInt16 currentTouchID = 0; //strictly for external convenience


        private float lastSize = 0f; //0-1
        float lastTouchAngle = 0f; //used to calculate twist, this is the angle between the two farthest touches in the last frame
        public Vector3 getAverageTouchWorldPos(hypercubeCamera c)
        {
            return c.transform.TransformPoint(getAverageTouchLocalPos());
        }
        public Vector3 getAverageTouchLocalPos()
        {
            if (screenOrientation == touchScreenOrientation.FRONT_TOUCHSCREEN)
                return new Vector3(averagePos.x + .5f, averagePos.y + .5f, -.5f); 
            else if (screenOrientation == touchScreenOrientation.BACK_TOUCHSCREEN)
                return new Vector3((1f - averagePos.x) + .5f, averagePos.y + .5f, .5f);  
            else
            {
                Debug.LogError("implement this!!!"); //TODO
                return Vector3.zero; //TODO
            }
        }


        public void prepareNextFrame()
        {
            if (!active)
                return;

            for (int i = 0; i < touchPoolSize; i++)
            {
                interfaces[i].active = false;
            }
        }

        public void _init(int index, dataFileDict d, float projectionWidth, float projectionHeight)
        {
            if (index != (int)screenOrientation)
                Debug.LogError("Something went wrong with the touch screen initialization. How can these not match?");

            screenResX = d.getValueAsFloat("touchScreenResX_" + index, screenResX);
            screenResY = d.getValueAsFloat("touchScreenResY_" + index, screenResY);
            topLimit = d.getValueAsFloat("touchScreenMapTop_" + index, topLimit); //use averages.
            bottomLimit = d.getValueAsFloat("touchScreenMapBottom_" + index, bottomLimit);
            leftLimit = d.getValueAsFloat("touchScreenMapLeft_" + index, leftLimit);
            rightLimit = d.getValueAsFloat("touchScreenMapRight_" + index, rightLimit);

            touchScreenWidth = projectionWidth * (1f / (rightLimit - leftLimit));
            touchScreenHeight = projectionHeight * (1f / (topLimit - bottomLimit));

        }

        //this is the method through which the touchScreenInputManager injects data into the touchScreen object
        public void _interface(rawTouchData d)
        {
            if (!active)
                active = true; //if we get touch events, automatically activate this touch screen. (it obviously exists on this device)

            //sometimes the hardware sends us funky data.
            //if the stats are funky, throw it out.
            if (d.id == 0 || d.id >= maxTouches)
                return;
            if (d.x < 0 || d.x > screenResX)
                return;
            if (d.y < 0 || d.y > screenResY)
                return;

            //is this a new touch?  If so, assign it to a new item in the pool, and update our iterators.
            if (touchPool[touchIdMap[d.id]].state < touch.activationState.ACTIVE) //a new touch!  Point it to a new element in our touchPool  (we know it is new because the place where the itr is pointing to is deactivated. Hence, it must have gone through at least 1 frame where no touch info was received for it.)
            {
                //we can not allow either key duplicates or value duplicates in the map.
                //key duplicates are already handled because an array index is by definition unique.
                //however here, we have to make sure another id is not already pointing to our desired touchPoolItr.
                for (int k = 0; k < maxTouches; k++)
                {
                    if (touchIdMap[k] == touchPoolItr && k != d.id)
                        touchIdMap[k] = 0; //point it to our always defunct element.  Without this, we can cause our touchCount to be incorrect.
                }

                touchIdMap[d.id] = touchPoolItr; //point the id to the current iterator 

                currentTouchID++;
                interfaces[touchIdMap[d.id]]._id = currentTouchID; //this id, is purely for external convenience and does not affect our functions here.

                touchPoolItr++;
                if (touchPoolItr >= touchPoolSize)
                    touchPoolItr = 1;  //we rely on element 0 always being inactive so we can use it to fix any duplicates.
            }

            interfaces[touchIdMap[d.id]].active = true;

            interfaces[touchIdMap[d.id]].rawTouchScreenX = (int)d.iX;
            interfaces[touchIdMap[d.id]].rawTouchScreenY = (int)d.iY;

            //interfaces[touchIdMap[d.id]].orientation = d.o; //already set.

            //set the normalized x/y to the touchscreen limits
            mapToRange(d.x / screenResX, d.y / screenResY, topLimit, rightLimit, bottomLimit, leftLimit,
            out interfaces[touchIdMap[d.id]].normalizedPos.x, out interfaces[touchIdMap[d.id]].normalizedPos.y);

            interfaces[touchIdMap[d.id]].physicalPos.x = (d.x / screenResX) * touchScreenWidth;
            interfaces[touchIdMap[d.id]].physicalPos.y = (d.y / screenResY) * touchScreenHeight;
        }



        public void postProcessData()
        {
            if (!active)
                return;

            touchCount = 0;
            for (int k = 0; k < maxTouches; k++)
            {
                if (interfaces[touchIdMap[k]].active)
                    touchCount++;
            }

            float averageNormalizedX = 0f;
            float averageNormalizedY = 0f;
            float averageDiffX = 0f;
            float averageDiffY = 0f;
            float averageDistX = 0f;
            float averageDistY = 0f;

            touchInterface highX = null;
            touchInterface lowX = null;
            touchInterface highY = null;
            touchInterface lowY = null;

            touches = new touch[touchCount];

            //main touches are a way for us to stabilize the twist and scale outputs by not hopping around different touches, instead trying to calculate the values from the same touches if possible
            bool updateMainTouches = false;
            if (touchCount > 1 && (mainTouchA == null || mainTouchB == null || mainTouchA.active == false || mainTouchB.active == false))
                updateMainTouches = true;

            int t = 0;
            for (int i = 0; i < touchPoolSize; i++)
            {
                touchPool[i]._interface(interfaces[i]); //update and apply our new info to each touch.     
                if (interfaces[i].active)
                {
                    touches[t] = touchPool[i];//these are the touches that can be queried from hypercube.input.front.touches              
                    t++;

                    averageNormalizedX += touchPool[i].posX;
                    averageNormalizedY += touchPool[i].posY;
                    averageDiffX += touchPool[i].diffX;
                    averageDiffY += touchPool[i].diffY;
                    averageDistX += touchPool[i].distX;
                    averageDistY += touchPool[i].distY;

                    if (updateMainTouches)
                    {
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
            }

            if (touchCount < 2)
            {
                touchSize = touchSizeCm = 0f;
                pinch = 1f;
                lastTouchAngle = twist = 0f;
                mainTouchA = mainTouchB = null;
                if (touchCount == 0)
                    averagePos = averageDiff = averageDist = Vector2.zero;
                else //1 touch only.
                {
                    averagePos = new Vector2(touches[0].posX, touches[0].posY);
                    averageDiff = new Vector2(touches[0].diffX, touches[0].diffY);
                    averageDist = new Vector2(touches[0].distX, touches[0].distY);
                }
            }
            else
            {
                averagePos = new Vector2(averageNormalizedX / (float)touchCount, averageNormalizedX / (float)touchCount);
                averageDiff = new Vector2(averageDiffX / (float)touchCount, averageDiffY / (float)touchCount);
                averageDist = new Vector2(averageDistX / (float)touchCount, averageDistY / (float)touchCount);

                if (averageDiff.x < -.3f || averageDiff.x > .3f || averageDiff.y < -.3f || averageDiff.y > .3) //this is too fast to be a real movement, its probably an artifact.
                {
                    averageDiff = averageDist = Vector2.zero;
                }

                //pinch and twist
                if (updateMainTouches)
                {
                    if ((highX.physicalPos.x - lowX.physicalPos.x) > (highY.physicalPos.y - lowY.physicalPos.y))//use the bigger of the two differences, and then use the true distance
                    {
                        mainTouchA = highX;
                        mainTouchB = lowX;
                    }
                    else
                    {
                        mainTouchA = highY;
                        mainTouchB = lowY;
                    }
                }

                touchSizeCm = mainTouchA.getPhysicalDistance(mainTouchB);
                touchSize = mainTouchA.getDistance(mainTouchB);

                float angle = angleBetweenPoints(mainTouchA.normalizedPos, mainTouchB.normalizedPos);


                //validate everything coming out of here... ignore crazy values that may come from hardware artifacts.
                if (lastTouchAngle == 0)
                    twist = 0;
                else
                    twist = angle - lastTouchAngle;

                if (twist < -20f || twist > 20f) //more than 20 degrees in 1 frame?!.. most likely junk. toss it.
                    twist = angle = 0f;
                lastTouchAngle = angle;

                if (lastSize == 0f)
                    pinch = 1f;
                else
                    pinch = touchSizeCm / lastSize;

                if (pinch < .7f || pinch > 1.3f) //the chances that this is junk data coming from the touchscreen are very high. dump it.
                    pinch = 1f;

                lastSize = touchSizeCm;
            }

            //finally, send off the events to touchScreenTargets.
            for (int i = 0; i < touchPoolSize; i++)
            {
                if (touchPool[i].state != touch.activationState.DESTROYED)
                    input._processTouchScreenEvent(touchPool[i]);
            }
        }


        static float angleBetweenPoints(Vector2 v1, Vector2 v2)
        {
            return Mathf.Atan2(v1.x - v2.x, v1.y - v2.y) * Mathf.Rad2Deg;
        }

        public static void mapToRange(float x, float y, float top, float right, float bottom, float left, out float outX, out float outY)
        {
            outX = map(x, left, right, 0f, 1.0f);
            outY = map(y, bottom, top, 0f, 1.0f);
        }
        static float map(float s, float a1, float a2, float b1, float b2)
        {
            //return Mathf.Lerp(b1, b2, Mathf.InverseLerp(a1, a2, s)); //also works fine.
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

    }
}

