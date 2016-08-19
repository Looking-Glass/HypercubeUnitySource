using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System.Collections.Generic;
using System;

public class serialStreamer : MonoBehaviour {

    public int baudRate = 115200;
    SerialPort port;
    List<int> buffer = new List<int>();
    byte[] arduinoTime = new byte[4];
    uint micros;
    uint deltaMicros;

    UnityEngine.UI.Text outputText = null;
    void Start()
    {
        outputText = GameObject.Find("OUTPUT").GetComponent<UnityEngine.UI.Text>();

        string[] names = SerialPort.GetPortNames();
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].StartsWith("COM"))
            {
                port = new SerialPort(names[i], baudRate);
                port.ReadBufferSize = 1024;//默认值为4096
                port.NewLine = "\r\n";
                port.ReadTimeout = 1;//Unity在Windows平台下不能通过新线程与串口通信，这样会导致数据丢失，必须在主线程中进行
                //port.DataReceived += PortDataReceived;//Unity不支持DataReceived事件，参考：http://www.cnblogs.com/zhaozhengling/p/3696251.html
                //port.ReceivedBytesThreshold = 1;//理由同上
                break;
            }
        }
        if (port == null)
        {
           
        }
        else
        {
            port.Open();
        }
    }

    void Update()
    {
        if (buffer.Count > 0)
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
        }

        if (port != null && port.IsOpen)
        {
            //if (Input.GetKeyDown(KeyCode.A))
            //{
            //    byte[] bs = new byte[] { 0x61, 0x62, 0x3B };
            //    port.Write(bs, 0, 3);
            //}

            try
            {
                for (int i = 0; i < port.ReadBufferSize; i++)
                {
                    buffer.Add(port.ReadByte());//port.ReadByte()：当串口缓冲区无数据可读时将触发"读取超时"异常
                }
            }
            catch (TimeoutException)
            {
                //UnityEngine.Debug.Log("读取超时");
            }
        }

        if (buffer.Count > 0)
        {
            outputText.text = "";
            foreach (int i in buffer)
                outputText.text += i.ToString() + " ";
            buffer.Clear();
        }
    }

    void OnDestroy()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }


/*    public int baudRate = 115200;

    static SerialPort serialPort = null;

	void Start () 
    {
        foreach (string s in SerialPort.GetPortNames())
            Debug.Log("PORT: " +s);

        serialPort = new SerialPort("COM9", baudRate);
        serialPort.ReadTimeout = 100;
        serialPort.WriteTimeout = 100;
        //arduinoSerial.DataReceived += arduinoSerial_DataReceived;

        serialPort.Open();

	}


    //static void arduinoSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
    void Update()
    {
        if (!serialPort.IsOpen)
            return;

        if (serialPort.BytesToRead != null && serialPort.BytesToRead >= 4)
        {
            byte[] data = new byte[4];
            serialPort.Read(data, 0, 4);

            UInt32 result = BitConverter.ToUInt32(data, 0);

             GameObject.Find("OUTPUT").GetComponent<UnityEngine.UI.Text>().text = result.ToString();
        }

    }*/
}
