using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace hypercube
{
    public enum touchEvent
    {
        TOUCH_INVALID = -1,
        TOUCH_DOWN = 0,  //a brand new touch will contain this event;
        TOUCH_UP,  //the last touch with this id will contain this event;
        TOUCH_MOVE
    }

    public class touch
    {
        public int id;
        public touchEvent e = touchEvent.TOUCH_INVALID;
        public float posX; //0-1
        public float posY; //0-1
        public float diffX; //normalized relative movement this frame inside 0-1
        public float diffY; //normalized relative movement this frame inside 0-1

        public float distX; //this accounts for physical distance that the touch traveled so that an application can react to the physical size of the movement irrelevant to the size of the touch screen (ie the value will be the same for a movement of 1 mm/1 frame regardless of the touch screen's internal resolution or physical size)
        public float distY;//this accounts for physical distance that the touch traveled so that an application can react to the physical size of the movement irrelevant to the size of the touch screen (ie the value will be the same for a movement of 1 mm/1 frame regardless of the touch screen's internal resolution or physical size)
    }


    public class hypercubeInput : MonoBehaviour
    {

        //singleton pattern

        private static hypercubeInput instance = null;
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                instance = this;
            }
            DontDestroyOnLoad(this.gameObject);
        }
        //end singleton

        public int baudRate = 115200;
        public int reconnectionDelay = 500;
        public int maxUnreadMessage = 5;
        public int maxAllowedFailure = 3;



#if HYPERCUBE_INPUT
        Dictionary<int, Touch> touches = new Dictionary<int, Touch>();
        //TODO add leap hand input dictionary
        public static hypercubeInput get() { return instance; }


        public SerialController touchScreenFront;

        void Start()
        {
            touchScreenFront = addSerialPortInput("COM5"); //TEMP - SHOULD NOT BE HARDCODED!
        }

        void Update()
        {
            processRawTouchscreenInput(touchScreenFront);
        }

        void processRawTouchscreenInput(SerialController c)
        {

            string data = c.ReadSerialMessage();
            if (data == null)
                return;

            Debug.Log(data);
        }

        SerialController addSerialPortInput(string comName)
        {
            SerialController sc = gameObject.AddComponent<SerialController>();
            sc.portName = comName;
            sc.baudRate = baudRate;
            sc.reconnectionDelay = reconnectionDelay;
            sc.maxUnreadMessages = maxUnreadMessage;
            sc.maxFailuresAllowed = maxAllowedFailure;
            sc.enabled = true;
            return sc;
        }

#else //We use HYPERCUBE_INPUT because I have to choose between this odd warning below, or immediately throwing a compile error for new users who happen to have the wrong settings (IO.Ports is not included in .Net 2.0 Subset).  This solution is odd, but much better than immediately failing to compile.
    
    public static hypercubeInput get() 
    { 
        printWarning();
        return instance; 
    }
    
    void Start () 
    {
        printWarning();
        this.enabled = false;
    }

    static void printWarning()
    {
        Debug.LogWarning("TO USE HYPERCUBE INPUT: \n1) Go To - Edit > Project Settings > Player    2) Set Api Compatability Level to '.Net 2.0'    3) Add HYPERCUBE_INPUT to Scripting Define Symbols (separate by semicolon, if there are others)");
    }
#endif
    }

}
