/*  
Hypercube: Volume Plugin is released under the MIT License:

Copyright 2016 Looking Glass Factory, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do 
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Text;

namespace hypercube
{
    public class utils
    {
        public static string formatPathToOS(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }


        public static bool getDoesConfigFileExist(string relativePathToConfig)
        {
            string temp;
            return getConfigPathToFile(relativePathToConfig, out temp);
        }

        //this method is used to figure out which drive is the usb flash drive attached to Volume, and then returns that path so that our settings can load normally from there.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        public static bool getConfigPathToFile(string relativePathToConfig, out string fullPath)
        {
            relativePathToConfig = formatPathToOS(relativePathToConfig);

            string[] drives = System.Environment.GetLogicalDrives();
            foreach (string drive in drives)
            {
                if (File.Exists(drive + relativePathToConfig))
                {
                    fullPath = drive + relativePathToConfig;
                    return true;
                }
            }
            fullPath = Path.GetFileName(relativePathToConfig); //return the base name of the file only.
            return false;
        }
#else  //osx,  TODO: linux untested in standalone
        public static bool getConfigPathToFile(string relativePathToConfig, out string fullPath)        
        {                        
            string[] directories = Directory.GetDirectories("/Volumes/");
            foreach (string d in directories)
            {
                string fixedPath = d + "/" + relativePathToConfig;
                fixedPath = formatPathToOS(fixedPath);

                FileInfo f = new FileInfo (fixedPath);
                if (f.Exists)                
                {                    
                    fullPath = f.FullName;      
                    return true;  
                }            
            }            
            fullPath = Path.GetFileName(relativePathToConfig); //return the base name of the file only.        
            return false;
        }
#endif

        // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
        public static string colorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }
        public static Color hexToColor(string hex)
        {
            hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
            hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
            byte a = 255;//assume fully visible unless specified in hex
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            //Only use alpha if the string has enough characters
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return new Color32(r, g, b, a);
        }

        public static void vert2Bin(Vector2[,,] d, out string base64binary)
        {
            byte[] data = vert2Bin(d);

            if (data == null)
                base64binary = "";
            else
                base64binary = System.Convert.ToBase64String(data);
        }

        //this converts castMesh vertex offsets, into a binary form.
        public static byte[] vert2Bin(Vector2[,,] d)
        {
            List<byte> outData = new List<byte>();

            if (d == null || d.GetLength(0) == 0 || d.GetLength(1) == 0 || d.GetLength(2) == 0) 
                return null;

            int slices = d.GetLength(0);
            int ax = d.GetLength(1);
            int ay = d.GetLength(2);
            outData.AddRange(System.BitConverter.GetBytes(slices)); //header
            outData.AddRange(System.BitConverter.GetBytes(ax));
            outData.AddRange(System.BitConverter.GetBytes(ay));

            for (int s = 0; s < slices; s++)  //data
            {
                for (int y = 0; y < ay; y++)
                {
                    for (int x = 0; x < ax; x++)
                    {
                        float vx = d[s, x, y].x; //pass it to a variable, because it doesn't encode properly when its directly sent into get bytes.
                        float vy = d[s, x, y].y;
                        outData.AddRange(System.BitConverter.GetBytes(vx));
                        outData.AddRange(System.BitConverter.GetBytes(vy));
                    }
                }
            }
            return outData.ToArray();
        }

        //converts stored binary calibration data into a proper array for the castMesh
        public static bool bin2Vert(string base64binary, out Vector2[,,] outData)
        {
            byte[] rawBytes = null;
            outData = null;
            try
            {
                rawBytes = System.Convert.FromBase64String(base64binary);
            }
            catch 
            {           
                return false;
            }

            //byte[] rawBytes = System.Text.Encoding.Unicode.GetBytes(inBinaryData); //use this line if hte string contains the raw data itself
            return bin2Vert(rawBytes, out outData);
        }
        public static bool bin2Vert(byte[] rawBytes, out Vector2[,,] outData)
        {
            outData = null;
            if (rawBytes == null || rawBytes.Length == 0)
                return false;

            try
            {
                int sliceCount = System.BitConverter.ToInt32(rawBytes, 0);
                int xArticulation = System.BitConverter.ToInt32(rawBytes, 4);
                int yArticulation = System.BitConverter.ToInt32(rawBytes, 8);

                if (!System.BitConverter.IsLittleEndian)
                {
                    sliceCount = (int)reverseBytes((uint)sliceCount);
                    xArticulation = (int)reverseBytes((uint)xArticulation);
                    yArticulation = (int)reverseBytes((uint)yArticulation);
                }

                if (rawBytes.Length % 4 != 0)
                {
                    Debug.LogWarning("The data received for the slices is malformed.");
                    return false;
                }

                int count = rawBytes.Length / 4;
                if (count - 3 != sliceCount * xArticulation * yArticulation * 2) //the count -3 = don't count the header info   ... the *2 = x and also y
                {
                    Debug.LogWarning("The data received for the slices does not match the expected array length.");
                    return false;
                }

                outData = new Vector2[sliceCount, xArticulation, yArticulation];

                for (int s = 0; s < sliceCount; s++)
                {
                    for (int y = 0; y < yArticulation; y++)
                    {
                        for (int x = 0; x < xArticulation; x++)
                        {
                            int i = (s * xArticulation * yArticulation * 2) + (y * xArticulation * 2) + (x * 2);
                            i *= 4;
                            i += 12;//start at the end of the header.

                            outData[s, x, y] = new Vector2();
                            outData[s, x, y].x = System.BitConverter.ToSingle(rawBytes, i);
                            outData[s, x, y].y = System.BitConverter.ToSingle(rawBytes, i + 4);
                        }
                    }
                }
            }
            catch 
            {

                hypercube.input._debugLog("<color=orange>Exception caught in bin2Vert conversion!</color>");

                return false;
            }

            return true;
        }

        //convert endianess
        public static uint reverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }


        public static void writeDebugStats()
        {
            writeDebugStats("DEBUG_INFO.txt");
        }
        public static void writeDebugStats(string path)
        {
            System.IO.File.WriteAllText(path, getDebugStats());
            Debug.Log("Wrote out file to: " + path);
        }
        public static string getDebugStats()
        {
            StringBuilder s = new StringBuilder();

            s.Append("HYPERCUBE DEBUG REPORT: " +Application.productName + "  -  " + System.DateTime.Today.ToString("F") + "\n");

            s.Append("\n///////////////////////////////////////////////////////////////////// - SOFTWARE:\n");
            s.Append("Unity version: " + Application.unityVersion.ToString() + "\n");
            s.Append("Hypercube  version: " + hypercubeCamera.version  + "\n");
            s.Append("App  version: " + Application.version.ToString() + "\n");
            s.Append("Platform: " + Application.platform.ToString() + "\n");
            s.Append("Running in Editor?: " + Application.isEditor.ToString() + "\n");
            s.Append("Run Directory: " + System.Environment.CurrentDirectory + "\n");
            s.Append("App asset path: " + Application.dataPath + "\n");
#if !UNITY_EDITOR
            //TODO build date
#else
            s.Append("Build Date: " + System.DateTime.Today.ToString("F") + "\n"); //now.
#endif
            s.Append("\n///////////////////////////////////////////////////////////////////// - CPU HARDWARE:\n");
            s.Append(SystemInfo.operatingSystem + "\n");
            s.Append("Screen Resolution: " + Screen.currentResolution.width + " x " + Screen.currentResolution.height + "\n");
            s.Append(SystemInfo.graphicsDeviceVendor + "  -  " + SystemInfo.graphicsDeviceName + " - version: "+ SystemInfo.graphicsDeviceVersion +"\n");
            s.Append("Graphics memory: " + SystemInfo.graphicsMemorySize + " Mbytes\n");
            s.Append("Supports compute shaders: " + SystemInfo.supportsComputeShaders.ToString() + "\n");
            s.Append("Multi Threaded GPU: " + SystemInfo.graphicsMultiThreaded.ToString() + "\n");
            s.Append("Reversed Z Buffer: " + SystemInfo.usesReversedZBuffer.ToString() + "\n");

            s.Append("\n");
            s.Append("CPU Processor: " + SystemInfo.processorType.ToString() + "\n");
            s.Append("CPU Frequency: " + SystemInfo.processorFrequency.ToString() + "\n");
            s.Append("CPU Cores: " + SystemInfo.processorCount.ToString() + "\n");
            s.Append("RAM: " + SystemInfo.systemMemorySize.ToString() + " MBytes\n");

#if UNITY_5_6_OR_NEWER
            s.Append("\n");
            s.Append("Battery Status: " + SystemInfo.BatteryStatus.ToString() + "\n");
            s.Append("Battery Status: " + SystemInfo.batteryLevel * 100 + "%\n");
#endif

            s.Append("\n///////////////////////////////////////////////////////////////////// - LKG HARDWARE:\n");
            s.Append("Touch Panel Firmware version: " + (hypercube.input._get() != null ? hypercube.input._get().touchPanelFirmwareVersion.ToString() : "---") + "\n");
           // s.Append("Calibrated with calibrator version: " + (hypercube.castMesh.canvas == null ? "---" : hypercube.castMesh.canvas.GetComponent<dataFileDict>().getValue("calibratorVersion", "2.21 or older")) + "\n");

            s.Append("////////  CALIBRATION SETTINGS:\n");
            s.Append(hypercube.castMesh.canvas == null ? "---" : hypercube.castMesh.canvas.GetComponent<dataFileDict>().getDataAsString());

            return s.ToString();
        }

    }

}
