using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace hypercube
{
    
    public enum touchEvent
    {
        TOUCH_INVALID = -1,
        TOUCH_DOWN = 0,  //a brand new touch will contain this event;
        TOUCH_UP,  //the last touch with this id will contain this event;
        TOUCH_MOVE,
        TOUCH_HOLD
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
        public bool debug = false;

        float touchscreenResX = 800f; //these are not public, as the touchscreen res can vary from device to device.  We abstract this for the dev as 0-1.
        float touchscreenResY = 450f;
        float touchscreenSizeX = 8f;
        float touchscreenSizeY = 4f;

        const int maxTouchesPerScreen = 9;

        public void applySettings(dataFileDict d)
        {
            if (!d)
            {
                Debug.LogError("Input was passed bad hardware dataFileDict!"); 
                return;
            }

            if (!d.hasKey("touchscreenResX") || 
                !d.hasKey("touchscreenResY") ||
                !d.hasKey("touchscreenSizeX") ||
                !d.hasKey("touchscreenSizeY")
                )
                Debug.LogWarning("Volume config file lacks touch screen hardware specs!"); //these must be manually entered, so we should warn if they are missing.

            touchscreenResX = d.getValueAsFloat("touchscreenResX", 800f);
            touchscreenResY = d.getValueAsFloat("touchscreenResY", 450f);
            touchscreenSizeX = d.getValueAsFloat("touchscreenSizeX", 8f);
            touchscreenSizeY = d.getValueAsFloat("touchscreenSizeY", 8f);
        }

#if HYPERCUBE_INPUT
        Dictionary<int, Touch> frontTouches = new Dictionary<int, Touch>();

        //TODO add leap hand input dictionary

        //get the instance of hypercube.input
        public static input get() { return instance; }
        static bool hardwareInitReceivedFront = false;  //this is flipped as soon as we get 'init:done' from the front touchscreen.  It is flipped off if we detect that it is disabled.
        static bool hardwareInitReceivedBack = false;


        public SerialController touchScreenFront;

        UnityEngine.UI.Text outputText;

        void Start()
        {

            outputText = GameObject.Find("OUTPUT").GetComponent<UnityEngine.UI.Text>();



            touchScreenFront = addSerialPortInput("COM9"); //TODO - SHOULD NOT BE HARDCODED!

            HashSet<touch> touches = new HashSet<touch>();
        }

        void Update()
        {
            processRawTouchscreenInput(touchScreenFront, ref hardwareInitReceivedFront); //we only really care about config messages for the front touch screen, which is where we store data.
        }

        void processRawTouchscreenInput(SerialController c, ref bool initialized)
        {

            //UInt32 v = 0;
            //while (c.ReadSerialMessage(ref v))
            //{
            //    outputText.text = v.ToString();
            //}
            
           string data = c.ReadSerialMessage();
            while (data != null)
            {
                if (debug)
                    Debug.Log(data);

                //if (!initialized)
                //{
                //    if (data == "init:done" || data.Contains("init:done"))
                //        initialized = true;
                //    return; //still initializing
                //}

                //byte[] d = System.Text.Encoding.UTF8.GetBytes (data);
                outputText.text = data;

                // PROCESS TOUCH EVENT
                //first, obtain the raw info.
                //string[] toks = data.Split(' ');
                //int[] dataParts = new int[toks.Length];
                //foreach()
                //int touchCount = int.Parse(toks[0]);
                //for(int )



                data = c.ReadSerialMessage();
            }

            //preserve this frame's events into a nice array with organized info
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
            if ( !instance)
                return false;

            if (input.get().touchScreenFront)
            {
                if (!input.get().touchScreenFront.enabled)
                    hardwareInitReceivedFront = false; //we must wait for another init:done before we give the go-ahead to talk to the hardware.
                else if (hardwareInitReceivedFront)
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

   


#else //We use HYPERCUBE_INPUT because I have to choose between this odd warning below, or immediately throwing a compile error for new users who happen to have the wrong settings (IO.Ports is not included in .Net 2.0 Subset).  This solution is odd, but much better than immediately failing to compile.
    
        public static bool isHardwareReady() //can the touchscreen hardware get/send commands?
        {
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
