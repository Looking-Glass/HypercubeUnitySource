using UnityEngine;
using System.Collections;
using System.IO;

namespace hypercube
{
    public class utils
    {
        //this method is used to figure out which drive is the usb flash drive attached to Volume, and then returns that path so that our settings can load normally from there.
        public static string getConfigPath(string relativePathToConfig)
        {
            string[] drives = Directory.GetLogicalDrives();
            foreach (string drive in drives)
            {
                if (File.Exists(drive + relativePathToConfig))
                {
                    return drive + relativePathToConfig;
                }
            }

            return Path.GetFileName(relativePathToConfig); //return the base name of the file only.
        }

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
    }

}
