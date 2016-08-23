using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace hypercube
{



public class touchScreenInputManager  : streamedInputManager
{

    UnityEngine.UI.Text outputText = null;

    public readonly string deviceName;

    HashSet<touch> touches = new HashSet<touch>();

    public uint totalTouches { get; private set; }

    public Vector2 averageDiff { get; private set; } //0-1
    public Vector2 averageDist {get;private set;} //in centimeters

    public float twist {get;private set;}
    public float scale { get; private set; }//0-1
    public float scaleDist {get;private set;} //in centimeters

    public Vector3 averagePosWorld {get;private set;}
    public Vector3 averagePosLocal { get; private set; }




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
