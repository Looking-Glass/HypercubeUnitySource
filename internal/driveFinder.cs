using UnityEngine;
using System.Collections;
using System.IO;

//this class is used to figure out which drive is the usb flash drive attached to Volume, and then returns that path so that our settings can load normally from there.

public class driveFinder : MonoBehaviour {


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

    //void readFile(string path)
    //{
    //    string text = System.IO.File.ReadAllText(path);
    //    Debug.Log(text);
    //}
}
