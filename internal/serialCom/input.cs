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
        static bool hardwareInitReceived = false;  //this is flipped as soon as we get 'init:done' from the front touchscreen.  It is flipped off if we detect that it is disabled.


        public SerialController touchScreenFront;   

        void Start()
        {
            touchScreenFront = addSerialPortInput("COM8"); //TODO - SHOULD NOT BE HARDCODED!
        }

        void Update()
        {
            processRawTouchscreenInput(touchScreenFront, true); //we only really care about config messages for the front touch screen, which is where we store data.
        }

        void processRawTouchscreenInput(SerialController c, bool considerConfigMessages = false)
        {

            string data = c.ReadSerialMessage();
            if (data == null)
                return;

            if (considerConfigMessages)
            {
                if (processConfigData(data))
                    return; //we found some data that corresponds to config or settings, no need to do more with it
            }

            //TODO process touch events

            Debug.Log(data);
        }

        bool processConfigData(string data)
        {
            if (data == "init:done")
            {
                hardwareInitReceived = true;

                castMesh[] cameras = getCastMeshes();
                foreach (castMesh ca in cameras)
                {
                        ca.loadSettings();
                }
                return true;
            }
            else if (data == "get:complete") //TODO make this say 'done'  so it is consistent with the rest of the acks
            {
                hardwareInitReceived = true;

                castMesh[] cameras = getCastMeshes();
                foreach (castMesh ca in cameras)
                {
                        ca.updateMesh();
                }
                return true;
            }
            else if (data.StartsWith("int,"))
            {
                string[] toks = data.Split(',');
                castMesh[] casts = getCastMeshes();

                if (toks[1] == "sNum" )
                {
                    foreach (castMesh ca in casts)
                    {
                        ca.slices = dataFileDict.stringToInt(toks[2], ca.slices);
                        ca.updateMesh();  //the above line should call OnValidate and update this, but sometimes it doesn't... so we force it to call here.
                    }
                    return true;
                }
                else if (toks[1] == "invX")
                {
                    foreach (castMesh ca in casts)
                    {
                            ca.flipX = dataFileDict.stringToBool(toks[2], ca.flipX);
                    }
                    return true;
                }
                else if (toks[1] == "invY")
                {
                    foreach (castMesh ca in casts)
                    {
                            ca.flipY = dataFileDict.stringToBool(toks[2], ca.flipY);
                    }
                    return true;
                }
                else if (toks[1] == "invZ")
                {
                    foreach (castMesh ca in casts)
                    {
                            ca.flipZ = dataFileDict.stringToBool(toks[2], ca.flipZ);
                    }
                    return true;
                }           
            }
            else if (data.StartsWith("float,"))
            {
                string[] toks = data.Split(',');
                castMesh[] cameras = getCastMeshes();
                if (toks[1] == "offX")
                {
                    foreach (castMesh ca in cameras)
                    {
                            ca.sliceOffsetX = dataFileDict.stringToFloat(toks[2], ca.sliceOffsetX);
                    }
                    return true;
                }
                if (toks[1] == "offY")
                {
                    foreach (castMesh ca in cameras)
                    {
                            ca.sliceOffsetY = dataFileDict.stringToFloat(toks[2], ca.sliceOffsetY);
                    }
                    return true;
                }
                if (toks[1] == "wide")
                {
                    foreach (castMesh ca in cameras)
                    {
                            ca.sliceWidth = dataFileDict.stringToFloat(toks[2], ca.sliceWidth);
                    }
                    return true;
                }
                if (toks[1] == "heig")
                {
                    foreach (castMesh ca in cameras)
                    {
                            ca.sliceHeight = dataFileDict.stringToFloat(toks[2], ca.sliceHeight);
                    }
                    return true;
                }
                if (toks[1] == "gap")
                {
                    foreach (castMesh ca in cameras)
                    {
                            ca.sliceGap = dataFileDict.stringToFloat(toks[2], ca.sliceGap);
                    }
                    return true;
                }
            }
            else if (data.StartsWith("slice,"))
            {
                string[] toks = data.Split(',');

                if (toks.Length != 16) //type + name + 14 values 
                    return false;

                int s = dataFileDict.stringToInt(toks[1].Substring(1), -1);

                castMesh[] casts = getCastMeshes();
                foreach (castMesh ca in casts)
                {
                        ca.setCalibrationOffset(s,
                            dataFileDict.stringToFloat(toks[2], 0f),
                            dataFileDict.stringToFloat(toks[3], 0f),
                            dataFileDict.stringToFloat(toks[4], 0f),
                            dataFileDict.stringToFloat(toks[5], 0f),
                            dataFileDict.stringToFloat(toks[6], 0f),
                            dataFileDict.stringToFloat(toks[7], 0f),
                            dataFileDict.stringToFloat(toks[8], 0f),
                            dataFileDict.stringToFloat(toks[9], 0f),
                            dataFileDict.stringToFloat(toks[10], 0f),
                            dataFileDict.stringToFloat(toks[11], 0f),
                            dataFileDict.stringToFloat(toks[12], 0f),
                            dataFileDict.stringToFloat(toks[13], 0f),
                            dataFileDict.stringToFloat(toks[14], 0f),
                            dataFileDict.stringToFloat(toks[15], 0f)
                            );
                    }
                return true;
            }

            return false;
        }

        static castMesh[] getCastMeshes()
        {
            List<castMesh> outcams = new List<castMesh>();

            castMesh[] cameras = GameObject.FindObjectsOfType<castMesh>();
            foreach (castMesh ca in cameras)
            {
                    outcams.Add(ca);
            }
            return outcams.ToArray();
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

            if (!isFunctional || !instance)
                return false;

            if (input.get().touchScreenFront)
            {
                if (!input.get().touchScreenFront.enabled)
                    hardwareInitReceived = false; //we must wait for another init:done before we give the go-ahead to talk to the hardware.
                else if (hardwareInitReceived)
                    return true;
            }
           
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
