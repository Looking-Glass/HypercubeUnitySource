using UnityEngine;
using System.Collections;

//this code is designed to be used to connect to a given serial port, and if it can connect, handshake and 
//return with information about that serial port.

#if HYPERCUBE_INPUT
namespace hypercube
{

    public enum serialPortType
    {
        SERIAL_UNKNOWN = -1,
        SERIAL_WORKING = 0,
        SERIAL_TOUCHPANEL = 1
    }

    public class serialPortFinder
    {
        public bool debug;
        public float timeOut = 4f;
        float timer = 0f;

        public float firmwareVersion
        {
            get; private set;
        }
//        bool sentForcedInit = false;

        public stringInputManager getSerialInput()
        {
            return testSubject;
        }
        stringInputManager testSubject = null;

        serialPortType type = serialPortType.SERIAL_UNKNOWN;

        public void identifyPort(SerialController s)
        {
            testSubject = new stringInputManager(s);
            //testSubject.readDataAsString = true;
            timer = 0f;
            type = serialPortType.SERIAL_WORKING;
//            sentForcedInit = false;
        }

        // we try to read the serial port until we get some config data.
        // once we identify the port or timeout we can discard this class
        public serialPortType update(float deltaTime)
        {
            if (testSubject == null)
                return serialPortType.SERIAL_UNKNOWN;

            if (type != serialPortType.SERIAL_WORKING)
                return type;

            if (timer > timeOut)
                return serialPortType.SERIAL_UNKNOWN;


            timer += deltaTime;

            //when we first connect...
            //this will tell the chip to give us an init, even if it isn't mechanically resetting (just in case, for example on osx which does not mechanically reset the chip on connection)
//            if (!sentForcedInit && testSubject.serial.isConnected)
//            {
//#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
  //              testSubject.serial.SendSerialMessage("reping");  //OSX does not reset the serial port hardware on connect, so we need to get it to tell us again what it is.
//#endif
//                sentForcedInit = true;
//            }

            testSubject.update(debug);

            string data = testSubject.readMessage();
            while (data != null)
            {
                if (debug)
                    Debug.Log("serial port finder: " + data);


                if (data.StartsWith("firmwareVersion::"))
                {
                    string[] toks = data.Split(new string[] { "::" }, System.StringSplitOptions.None);
                    firmwareVersion = dataFileDict.stringToFloat(toks[2], firmwareVersion);

                    if (toks[1] == "touchPanelsPCB")
                    {
                        type = serialPortType.SERIAL_TOUCHPANEL;
                        return type; //don't readMessage again. let the calling method do it, now that we are done here.
                    }

                    //TODO add any other kinds of serial ports that need ID here.
                }

                data = testSubject.readMessage();
            }

            return type;
        }
    }
}

#endif
