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

    public class input : MonoBehaviour
    {

        //singleton pattern

        private static input instance = null;
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

        public static input get() { return instance; }


        public SerialController touchScreenFront;

        void Start()
        {
            touchScreenFront = addSerialPortInput("COM7"); //TEMP - SHOULD NOT BE HARDCODED!
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

        public static bool isHardwareReady() //can the touchscreen hardware get/send commands?
        {
            if (!instance)
                return false;

            if (input.get().touchScreenFront && isFunctional && input.get().touchScreenFront.enabled )
                return true;
            return false;
        }

        public static bool sendCommandToHardware(string cmd)
        {
            if (isHardwareReady())
            {
                instance.touchScreenFront.SendSerialMessage(cmd + "\n\r");
                return true;
            }
            else
                Debug.LogWarning("Can't send message to hardware, it is either not yet initialized, disconnected, or malfunctioning.");

            return false;
        }

        public static bool clearAllHardwareValues()
        {
            return sendCommandToHardware("#erase");
        }

        public static bool saveValueToHardware(string varName, string _val)
        {
            if (!isHardwareReady())
                return false;

            if (varName.Length == 0 || _val.Length == 0)
                return false;

            if (_val.Length > 8)
                _val = _val.Substring(0, 8); //the hardware expects less than 8 characters in the string

            sendCommandToHardware("string," + validateVarName(varName) + "," + _val);
            return true;
        }
        
        public static bool saveValueToHardware(string varName, int _val)
        {
            if (!isHardwareReady())
                return false;

            if (varName.Length == 0)
                return false;

            sendCommandToHardware("int," + validateVarName(varName) + "," + _val.ToString());
            return true;
        }
        public static bool saveValueToHardware(string varName, short _val)
        {
            if (!isHardwareReady())
                return false;

            if (varName.Length == 0)
                return false;

            sendCommandToHardware("char," + validateVarName(varName) + "," + _val);
            return true;
        }
        public static bool saveValueToHardware(string varName, float _val)
        {
            if (!isHardwareReady())
                return false;

            if (varName.Length == 0)
                return false;

            sendCommandToHardware("float," + validateVarName(varName) + "," + _val.ToString());
            return true;
        }
        public static bool saveValueToHardware(string varName, bool _val)
        {
            if (!isHardwareReady())
                return false;

            if (varName.Length == 0)
                return false;

            //bool is saved as an int.
            int v = 0;
            if (_val)
                v = 1;
            sendCommandToHardware("int," + validateVarName(varName) + "," + v.ToString());
            return true;
        }
        
        static string validateVarName(string varName)
        {
            if (varName.Length > 4)
                return varName.Substring(0, 4);
            return varName;
        }


        public const bool isFunctional = true; //proof we are compiled with HYPERCUBE_INPUT


#else //We use HYPERCUBE_INPUT because I have to choose between this odd warning below, or immediately throwing a compile error for new users who happen to have the wrong settings (IO.Ports is not included in .Net 2.0 Subset).  This solution is odd, but much better than immediately failing to compile.
    
    public const bool isFunctional = false;

    public static bool isHardwareReady() //can the touchscreen hardware get/send commands?
    {
        printWarning();
        return false;
    }
    public static void sendCommandToHardware(string cmd)
    {
        printWarning();
    }

    public static input get() 
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
