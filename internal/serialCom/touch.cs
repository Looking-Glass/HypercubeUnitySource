using UnityEngine;
using System.Collections;


//this class exposes the touch data from Volume to a developer in a way that will be consistent across different models of Volume

//TODO save original local touch coord.

namespace hypercube 
{

    public class touchInterface
    {        
        public bool active = false;
        public System.UInt16 _id = System.UInt16.MaxValue;
        //public float normalizedX = 0f;
        //public float normalizedY = 0f;
        //public float physicalX = 0f;
        //public float physicalY = 0f;
        public Vector2 normalizedPos;
        public Vector2 physicalPos;

        public float getPhysicalDistance(touchInterface i)
        {
            return Vector2.Distance(physicalPos, i.physicalPos);
        }
    }

    //Note that resolution dependent dims are not exposed.
    //this is important because different devices will host different resolutions and all users of this API should create device independent software.
    //all needed data has been abstracted here for maximum compatibility among all types of Volume hardware
    public class touch
    {
        public touch(bool _frontScreen)
        {
            frontScreen = _frontScreen;
            _posX = _posY = physicalPos.x = physicalPos.y = _diffX = _diffY = _distX = _distY= 0;
            state = activationState.DESTROYED;
        }

        public readonly bool frontScreen;
        public System.UInt16 id { get; private set; }

        public enum activationState
        {
            TOUCHDOWN = 1,
            ACTIVE = 0,
            TOUCHUP = -1,  //touch will be marked as 'destroyed' next frame
            DESTROYED = -2  //error out on any use.
        }
       public activationState state { get; private set; }

        private float _posX; public float posX { get { if (activeCheck()) return _posX; return 0f; } } //0-1
        private float _posY; public float posY { get { if (activeCheck()) return _posY; return 0f; } } //0-1

        private float _diffX; public float diffX { get { if (activeCheck()) return _diffX; return 0f; } } //normalized relative movement this frame inside 0-1
        private float _diffY; public float diffY { get { if (activeCheck()) return _diffY; return 0f; } } //normalized relative movement this frame inside 0-1

        private float _distX; public float distX { get { if (activeCheck()) return _distX; return 0f; } } //this accounts for physical distance that the touch traveled so that an application can react to the physical size of the movement irrelevant to the size of the touch screen (ie the value will be the same for a movement of 1 mm/1 frame regardless of the touch screen's internal resolution or physical size)
        private float _distY; public float distY { get { if (activeCheck()) return _distY; return 0f; } } //this accounts for physical distance that the touch traveled so that an application can react to the physical size of the movement irrelevant to the size of the touch screen (ie the value will be the same for a movement of 1 mm/1 frame regardless of the touch screen's internal resolution or physical size)

        public Vector3 getWorldPos(hypercubeCamera c)
        {
            return c.transform.TransformPoint(getLocalPos());
        }
        public Vector3 getLocalPos() //get the local coordinate relative to the hypercubeCamera
        {
            if (!activeCheck())
                return Vector3.zero;

            if (frontScreen)
                return new Vector3(_posX + .5f, _posY + .5f, -.5f);
            else
                return new Vector3((1f - _posX) + .5f, _posY + .5f, .5f);
        }

        //how much time since touchDown
        public float touchDownTime { get; private set; }
        public float age { get { if (state == activationState.DESTROYED) return 0f; return Time.timeSinceLevelLoad - touchDownTime; } }



        public float getPhysicalDistanceTo(touch t) 
        { 
            touchInterface i = null;
            t._getInterface(ref i);
            return Vector2.Distance(i.physicalPos, physicalPos);
        }

        private Vector2 physicalPos;

        public void _getInterface(ref touchInterface i)
        {
            i.normalizedPos.x = _posX;
            i.normalizedPos.y = _posY;
         //   i.physicalPos.x =
            i.physicalPos = physicalPos;
            i._id = id;
            if (state < activationState.ACTIVE)
                i.active = false;
            else
                i.active = true;

        }   
        public void _interface(touchInterface i)
        {
            if (!i.active) //deactivate this touch.
            {
                deactivate();
                return;
            }

            if (state < activationState.ACTIVE)
            {
                state = activationState.TOUCHDOWN;
                touchDownTime = Time.timeSinceLevelLoad;
            }
            else
                state = activationState.ACTIVE;

            _diffX = posX - i.normalizedPos.x;
            _diffY = posY - i.normalizedPos.y;
            _distX = physicalPos.x - i.physicalPos.x;
            _distY = physicalPos.y - i.physicalPos.y;

            _posX = i.normalizedPos.x;
            _posY = i.normalizedPos.y;
            physicalPos = i.physicalPos;

            id = i._id; //faster and easier to just set it all the time than check if this is a new touch or not.
        }
        void deactivate()
        {
            if (state == activationState.DESTROYED)
                return;

            if (state == activationState.TOUCHDOWN) //rare for a touch to last only 1 hardware frame, but possible.  Force it down.
                state = activationState.ACTIVE;

            state--;
            _diffX = _diffY = _distX = _distY = 0f;

            if (state == activationState.DESTROYED)
                touchDownTime = _posX = _posY = physicalPos.x = physicalPos.y = 0f;
        }

        bool activeCheck()
        {
            if (state == activationState.DESTROYED)
            {
                Debug.LogError("Code attempted to use a destroyed touch! Test for this first by checking touch.state.\nDon't hold pointers to touches once they are 'Destroyed'.");
                return false;
            }
            return true;
        }

        //if you need to map this touch to a gui area or other surface, use this to help you.
        public Vector2 mapToRange(float x, float y, Vector2 leftTop, Vector2 rightBottom)
        {
            Vector2 position = new Vector2(x, y);
            position.x = map(_posX, 0, 1.0f, leftTop.x, rightBottom.x);
            position.y = map(_posY, 0.0f, 1.0f, rightBottom.y, leftTop.y);
            return position;
        }

        public static float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

    }


}
