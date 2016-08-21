using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Collections.Generic;
using System;

//the base of this code came from: http://pxp1230.github.io/Arduino/Unity%E4%B8%8EArduino%E9%80%9A%E4%BF%A1/Unity%E4%B8%8EArduino%E9%80%9A%E4%BF%A1.html

public class serialStreamer : MonoBehaviour {

    public int baudRate = 115200;
    public uint maxBuffer = 1024;
    SerialPort port;
    List<int> buffer = new List<int>();
   // byte[] arduinoTime = new byte[4];
  //  uint micros;
  //  uint deltaMicros;

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    UnityEngine.UI.Text outputText = null;
    void Start()
    {
        outputText = GameObject.Find("OUTPUT").GetComponent<UnityEngine.UI.Text>();


        string[] names = SerialPort.GetPortNames();
        for (int i = 0; i < names.Length; i++)
        {          
            if (names[i].StartsWith("COM"))
            {
                Debug.Log("Connecting to PORT: " + names[i]);

                port = new SerialPort(names[i], baudRate);
                port.ReadBufferSize = 7;//default 4096
                port.NewLine = "\r\n";
                port.ReadTimeout = 100; //Unity can not pass under the Windows platform, the new thread and serial communication, which can cause loss of data, must be in the main thread
                port.WriteTimeout = 100;
                //port.DataReceived += PortDataReceived; //Unity does not support DataReceived event, reference: http: //www.cnblogs.com/zhaozhengling/p/3696251.html
                //port.ReceivedBytesThreshold = 1;//...same
                break;
            }
        }

        if (port == null)
            Debug.LogWarning("Failed to connect to a port.");
        else
            port.Open();
    }

    void Update()
    {
     /*     if (buffer.Count > 0)
        {
             int i = 0;
            for (; i < buffer.Count; i++)
            {
                 if ((char)buffer[i] == ';')
                {
                    if (i + 4 < buffer.Count)
                    {
                        arduinoTime[0] = (byte)buffer[i + 1];
                        arduinoTime[1] = (byte)buffer[i + 2];
                        arduinoTime[2] = (byte)buffer[i + 3];
                        arduinoTime[3] = (byte)buffer[i + 4];
                        uint newMicros = BitConverter.ToUInt32(arduinoTime, 0);
                        //UnityEngine.Debug.Log((char)((int)buffer[0]));
                        //UnityEngine.Debug.Log(BitConverter.ToInt16(buffer, 1));
                        //UnityEngine.Debug.Log(BitConverter.ToSingle(buffer, 3));
                        if (micros < newMicros)
                            deltaMicros = newMicros - micros;
                        else
                            deltaMicros = newMicros + uint.MaxValue - micros;
                        micros = newMicros;
                        UnityEngine.Debug.Log(micros / 1000000.0 + "\t" + deltaMicros);
                        i += 4;
                    }
                    else
                    {
                        buffer.RemoveRange(0, i);
                        break;
                    }
                }
            }
        }*/

        if (port != null && port.IsOpen)
        {
            //if (Input.GetKeyDown(KeyCode.A))
            //{
            //    byte[] bs = new byte[] { 0x61, 0x62, 0x3B };
            //    port.Write(bs, 0, 3);
            //}
      //      if (buffer.Count >= maxBuffer) //if we reached the max buffer, throw out the old data and just read new data.
      //          buffer.Clear();

            try
            {
                for (int i = 0; i < port.ReadBufferSize; i++)
                {
                    buffer.Add(port.ReadByte());//port.ReadByte()：当串口缓冲区无数据可读时将触发"读取超时"异常 When no serial buffer readable, the data will trigger "Read timeout" exception
                }
            }
            catch (TimeoutException)
            {
                //UnityEngine.Debug.Log("读取超时"); //Read Timeout
            }
        }

        if (buffer.Count > 0)
        {
            
            //foreach (int i in buffer)
            //    sb.Append(i.ToString() + " ");
            //outputText.text = sb.ToString();
            buffer.Clear();
            sb.Length = 0; //clear the stringBuilder
        }
    }

    void OnDestroy()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
