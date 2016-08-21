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
        TOUCH_MOVE,
        TOUCH_HOLD
    }

    //Note that resolution dependent dims are not exposed.
    //this is important because different devices will host different resolutions and all users of this API should create device independent software.
    //all needed data has been abstracted here for maximum compatibility among all types of Volume hardware
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

        public Vector3 getWorldPos(hypercubeCamera c);
        public Vector3 getLocalPos(hypercubeCamera c); 
    }

 

public class touchScreenInputManager  : streamedInputManager
{

    UnityEngine.UI.Text outputText = null;

    public readonly string deviceName;

    HashSet<touch> touches = new HashSet<touch>();

    public uint totalTouches
    {
        get;
        private set;
    }
    public Vector2 averageDiff  //0-1
    {
        get;
        private set;
    }
    public Vector2 averageDist  //in centimeters
    {
        get;
        private set;
    }
    public float twist
    {
        get;
        private set;
    }
    public float scale //0-1
    {
        get;
        private set;
    }
    public float scaleDist  //in centimeters
    {
        get;
        private set;
    }
    public Vector3 averagePosWorld 
    {
        get;
        private set;
    }
    public Vector3 averagePosLocal  
    {
        get;
        private set;
    }



    float screenResX = 800f; //these are not public, as the touchscreen res can vary from device to device.  We abstract this for the dev as 0-1.
    float screenResY = 450f;
    float screenSizeX = 20f; //in centimeters
    float screenSizeY = 12f;

    static readonly byte[] emptyByte = new byte[] { 0 };

    public touchScreenInputManager(string _deviceName, SerialController _serial) : base(_serial, new byte[]{255,255}, 1024)
    {
        deviceName = _deviceName;
    }

    public void setTouchScreenDims(float _resX, float _resY, float _centimetersX, float _centimetersY)
    {
        screenResX = _resX;
        screenResY = _resY;
        screenSizeX = _centimetersX;
        screenSizeY = _centimetersY;
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

                byte[] bytes = new byte[data.Length * sizeof(char)];
                System.Buffer.BlockCopy(data.ToCharArray(), 0, bytes, 0, bytes.Length);
                addData(bytes); //inherited from base class. Will process our data given the delimiter.
     
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



        if (!outputText)
            outputText = GameObject.Find("OUTPUT").GetComponent<UnityEngine.UI.Text>();
        outputText.text = System.BitConverter.ToString(dataChunk);

        // PROCESS TOUCH EVENT
        //first, obtain the raw info.
        //string[] toks = data.Split(' ');
        //int[] dataParts = new int[toks.Length];
        //foreach()
        //int touchCount = int.Parse(toks[0]);
        //for(int )
    }

}

}
