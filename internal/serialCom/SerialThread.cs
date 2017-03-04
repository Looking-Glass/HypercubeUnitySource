﻿/**
 * Author: Daniel Wilches
 */
#if HYPERCUBE_INPUT
using UnityEngine;

using System;
using System.IO;
using System.IO.Ports;
using System.Collections;
using System.Threading;

/**
 * This class contains methods that must be run from inside a thread and others
 * that must be invoked from Unity. Both types of methods are clearly marked in
 * the code, although you, the final user of this library, don't need to even
 * open this file unless you are introducing incompatibilities for upcoming
 * versions.
 */
public class SerialThread
{
    // Parameters passed from SerialController, used for connecting to the
    // serial device as explained in the SerialController documentation.
    private string portName;
    private int baudRate;
    private int delayBeforeReconnecting;
    private int maxUnreadMessages;

    //public bool readDataAsString = true;

    // Object from the .Net framework used to communicate with serial devices.
    private SerialPort serialPort;

    // Amount of milliseconds alloted to a single read or connect. An
    // exception is thrown when such operations take more than this time
    // to complete.
    private const int readTimeout = 100;

    // Amount of milliseconds alloted to a single write. An exception is thrown
    // when such operations take more than this time to complete.
    private const int writeTimeout = 100;

    // Internal synchronized queues used to send and receive messages from the
    // serial device. They serve as the point of communication between the
    // Unity thread and the SerialComm thread.
    private Queue inputQueue, outputQueue;

    // Indicates when this thread should stop executing. When SerialController
    // invokes 'RequestStop()' this variable is set.
    private bool stopRequested = false;


    byte[] bytes = new byte[32];  //the size here is the chunk read value from the serialPort

    /**************************************************************************
     * Methods intended to be invoked from the Unity thread.
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Constructs the thread object. This object is not a thread actually, but
    // its method 'RunForever' can later be used to create a real Thread.
    // ------------------------------------------------------------------------
    public SerialThread(string portName,
                        int baudRate, 
                        int delayBeforeReconnecting,
                        int maxUnreadMessages)
    {
        this.portName = portName;
        this.baudRate = baudRate;
        this.delayBeforeReconnecting = delayBeforeReconnecting;
        this.maxUnreadMessages = maxUnreadMessages;

        inputQueue = Queue.Synchronized(new Queue());
        outputQueue = Queue.Synchronized(new Queue());
    }


    // ------------------------------------------------------------------------
    // Poll the internal message queue returning the next available message.
    // It returns null if no message has arrived since the latest invocation.
    // ------------------------------------------------------------------------
    public string ReadSerialMessage()
    {
        if (inputQueue.Count == 0)
            return null;
        
        return (string)inputQueue.Dequeue();
    }

    // ------------------------------------------------------------------------
    // Sends a message to the serial device. It writes the message to the
    // output queue, later the method 'RunOnce' reads this queue and sends
    // the message to the serial device.
    // ------------------------------------------------------------------------
    public void SendSerialMessage(string message)
    {
        outputQueue.Enqueue(message);
    }

    // ------------------------------------------------------------------------
    // Invoked to indicate to this thread object that it should stop.
    // ------------------------------------------------------------------------
    public void RequestStop()
    {
        lock (this)
        {
            stopRequested = true;
        }
    }


    /**************************************************************************
     * Methods intended to be invoked from the SerialComm thread (the one
     * created by the SerialController).
     *************************************************************************/

    // ------------------------------------------------------------------------
    // Enters an almost infinite loop of attempting conenction to the serial
    // device, reading messages and sending messages. This loop can be stopped
    // by invoking 'RequestStop'.
    // ------------------------------------------------------------------------
    public void RunForever()
    {
        // This try is for having a log message in case of an unexpected
        // exception.
        try
        {
            while (!IsStopRequested())
            {
                try
                {
                    // Try to connect
                    AttemptConnection();

                    // Enter the semi-infinite loop of reading/writing to the
                    // device.
                    while (!IsStopRequested())
                        RunOnce();
                }
                catch //(Exception ioe)
                {
                    // A disconnection happened, or there was a problem
                    // reading/writing to the device. Log the detailed message
                    // to the console and notify the listener too.
#if HYPERCUBE_DEV
                    hypercube.input._debugLog("<color=orange>Exception: " + ioe.Message + "\nStackTrace: " + ioe.StackTrace + "</color>");
#endif
                    inputQueue.Enqueue(SerialController.SERIAL_DEVICE_DISCONNECTED);

                    // As I don't know in which stage the SerialPort threw the
                    // exception I call this method that is very safe in
                    // disregard of the port's status
                    CloseDevice();

                    // Don't attempt to reconnect just yet, wait some
                    // user-defined time. It is OK to sleep here as this is not
                    // Unity's thread, this doesn't affect frame-rate
                    // throughput.
                    Thread.Sleep(delayBeforeReconnecting);
                }
            }

            // Attempt to do a final cleanup. This method doesn't fail even if
            // the port is in an invalid status.
            CloseDevice();
        }
        catch (Exception e)
        {
            Debug.LogError("Unknown exception: " + e.Message + " " + e.StackTrace);
        }
    }

    // ------------------------------------------------------------------------
    // Try to connect to the serial device. May throw IO exceptions.
    // ------------------------------------------------------------------------
    private void AttemptConnection()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = readTimeout;
        serialPort.WriteTimeout = writeTimeout;
        serialPort.Open();

        inputQueue.Enqueue(SerialController.SERIAL_DEVICE_CONNECTED);
    }

    // ------------------------------------------------------------------------
    // Release any resource used, and don't fail in the attempt.
    // ------------------------------------------------------------------------
    private void CloseDevice()
    {
        if (serialPort == null)
            return;

        try
        {
            serialPort.Close();
        }
        catch (IOException)
        {
            // Nothing to do, not a big deal, don't try to cleanup any further.
        }

        serialPort = null;
    }

    // ------------------------------------------------------------------------
    // Just checks if 'RequestStop()' has already been called in this object.
    // ------------------------------------------------------------------------
    private bool IsStopRequested()
    {
        lock (this)
        {
            return stopRequested;
        }
    }

    // ------------------------------------------------------------------------
    // A single iteration of the semi-infinite loop. Attempt to read/write to
    // the serial device. If there are more lines in the queue than we may have
    // at a given time, then the newly read lines will be discarded. This is a
    // protection mechanism when the port is faster than the Unity progeram.
    // If not, we may run out of memory if the queue really fills.
    // ------------------------------------------------------------------------
    private void RunOnce()
    {
  /*      try
        {
            // Send a message.
            if (outputQueue.Count != 0)
            {
                string outputMessage = (string)outputQueue.Dequeue();
                serialPort.Write(outputMessage);
            }

            // Read a message.
            // If a line was read, and we have not filled our queue, enqueue
            // this line so it eventually reaches the Message Listener.
            // Otherwise, discard the line.

            //if (readDataAsString)
            //{
            //    string inputMessage = serialPort.ReadLine();
            //    if (inputMessage != null && inputQueue.Count < maxUnreadMessages)
            //    {
            //        inputQueue.Enqueue(inputMessage);
            //    }
            //    return;
            //}
        }
        catch (TimeoutException)
        {
            // This is normal, not everytime we have a report from the serial device
            return;
        }*/

   //     if (readDataAsString)
  //          return;

        //READ DATA AS BYTES
        int byteCount = 0;    
        try
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                    bytes[i] = (byte)serialPort.ReadByte();//port.ReadByte()：当串口缓冲区无数据可读时将触发"读取超时"异常 When no serial buffer readable, the data will trigger "Read timeout" exception                 
                    byteCount++;
            }        
        }
         catch (TimeoutException)
        {
            // This is normal, not everytime we have a report from the serial device
             //but on a side note... what kind of nutcase designs an API that depends on itself crashing and for the dev to catch that exception to know that the api is 'done'.... wtf Ports.IO?!?!?
        }
        if (byteCount > 0 && inputQueue.Count < maxUnreadMessages)
            //inputQueue.Enqueue(bytesToStr(bytes, bytes.Length));
            inputQueue.Enqueue(System.Text.Encoding.Unicode.GetString(bytes));

        try
        {
            // Send a message.
            if (outputQueue.Count != 0)
            {
                string outputMessage = (string)outputQueue.Dequeue();
                serialPort.Write(outputMessage);
            }
        }
        catch (TimeoutException)
        {
        }

    }

    //from http://stackoverflow.com/questions/472906/how-to-get-a-consistent-byte-representation-of-strings-in-c-sharp-without-manual
//    static string bytesToStr(byte[] bytes, int count)
//    {
//        char[] chars = new char[count * sizeof(char)];
 //       System.Buffer.BlockCopy(bytes, 0, chars, 0, count);
 //       return new string(chars);
 //   }


}
#endif

