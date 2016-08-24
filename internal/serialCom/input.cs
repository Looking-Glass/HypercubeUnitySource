using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace hypercube
{


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

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            //end singleton

            setupSerialComs();
        }
      
        public int baudRate = 115200;
        public int reconnectionDelay = 500;
        public int maxUnreadMessage = 5;
        public int maxAllowedFailure = 3;
        public bool debug = false;


        const int maxTouchesPerScreen = 9;

        public static touchScreenInputManager frontScreen = null;  //the front touchscreen
        public static touchScreenInputManager backScreen = null;  //the back touchscreen

        HashSet<touchscreenTarget> eventTargets = new HashSet<touchscreenTarget>();
        public static void _setTouchScreenTarget(touchscreenTarget t, bool addRemove)
        {
            if (addRemove)
                instance.eventTargets.Add(t);
            else
                instance.eventTargets.Remove(t);
        }

        //use this instead of Start(),  that way we know we have our hardware settings info ready before we begin receiving data
        public static void init(dataFileDict d)
        {
            if (!d)
            {
                Debug.LogError("Input was passed bad hardware dataFileDict!");
                return;
            }

            if (!instance)
                return;

#if HYPERCUBE_INPUT

            if (!d.hasKey("touchscreenResX") ||
                !d.hasKey("touchscreenResY") ||
                !d.hasKey("projectionCentimeterWidth") ||
                !d.hasKey("projectionCentimeterHeight") ||
                !d.hasKey("touchscreenCentimeterWidth") ||
                !d.hasKey("touchscreenCentimeterHeight") ||
                !d.hasKey("projectionCentimeterDepth")  //this one is necessary to keep the hypercube aspect ratio
                )
                Debug.LogWarning("Volume config file lacks touch screen hardware specs!"); //these must be manually entered, so we should warn if they are missing.

            if (frontScreen != null)
            {
                frontScreen.setTouchScreenDims(
                    d.getValueAsFloat("touchscreenResX", 800f),
                    d.getValueAsFloat("touchscreenResY", 450f),
                    d.getValueAsFloat("projectionCentimeterWidth", 20f),
                    d.getValueAsFloat("projectionCentimeterHeight", 12f),
                    d.getValueAsFloat("touchscreenCentimeterWidth", 20f),
                    d.getValueAsFloat("touchscreenCentimetersHeight", 12f),
                    d.getValueAsFloat("centimeterWidthOffset", 0f),
                    d.getValueAsFloat("centimeterHeightOffset", 0f)
                    );
            }
#endif
        }

#if HYPERCUBE_INPUT

        public static void _processTouchScreenEvent(touch t)
        {
            if (t.state == touch.activationState.TOUCHDOWN)
            {
                    foreach(touchscreenTarget target in instance.eventTargets)
                        target.onTouchDown(t);
            }
            else if (t.state == touch.activationState.ACTIVE)
            {
                foreach (touchscreenTarget target in instance.eventTargets)
                    target.onTouchMoved(t);
            }
            else if (t.state == touch.activationState.TOUCHUP)
            {
                foreach (touchscreenTarget target in instance.eventTargets)
                    target.onTouchUp(t);
            }               
        }

        void setupSerialComs()
        {
            string frontComName = "";
            string[] names = getPortNames();

            if (names.Length == 0)
            {
                Debug.LogWarning("No ports detected. Confirm that Volume is connected via USB.");
                return;
            }

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].StartsWith("COM"))
                {
                    frontComName = names[i];
                }
            }

            if (frontScreen == null)
                frontScreen = new touchScreenInputManager("Front Touch Screen", addSerialPortInput(frontComName), true);

         //   if (back == null)
         //       back = new touchScreenInputManager("Back Touch Screen", addSerialPortInput(backComName), false);
        }


        static string[] getPortNames()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return System.IO.Ports.SerialPort.GetPortNames();
#else
            int p = (int)Environment.OSVersion.Platform;
            List<string> serial_ports = new List<string>();

            // Are we on Unix?
            if (p == 4 || p == 128 || p == 6)
            {
                string[] ttys = System.IO.Directory.GetFiles("/dev/", "tty.*");
                foreach (string dev in ttys)
                {
                    if (dev.StartsWith("/dev/tty.*"))
                        serial_ports.Add(dev);
                }
            }

            return ttys;
#endif
        }
        

        void Update()
        {
            if (frontScreen != null && frontScreen.serial.enabled)
                frontScreen.update(debug);
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

            if (frontScreen != null)
            {
                if (!frontScreen.serial.enabled)
                    frontScreen.serial.readDataAsString = true; //we must wait for another init:done before we give the go-ahead to get raw data again.
                else if (frontScreen.serial.readDataAsString == false)
                    return true;
            }
           
            return false;
        }

  /*      public static bool sendCommandToHardware(string cmd)
        {
            if (isHardwareReady())
            {
                touchScreenFront.serial.SendSerialMessage(cmd + "\n\r");
                return true;
            }
            else
                Debug.LogWarning("Can't send message to hardware, it is either not yet initialized, disconnected, or malfunctioning.");

            return false;
        }
*/
   


#else //We use HYPERCUBE_INPUT because I have to choose between this odd warning below, or immediately throwing a compile error for new users who happen to have the wrong settings (IO.Ports is not included in .Net 2.0 Subset).  This solution is odd, but much better than immediately failing to compile.
    
        void setupSerialComs()
        {

        }

        public static bool isHardwareReady() //can the touchscreen hardware get/send commands?
        {
            return false;
        }
        public static void sendCommandToHardware(string cmd)
        {
            printWarning();
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
