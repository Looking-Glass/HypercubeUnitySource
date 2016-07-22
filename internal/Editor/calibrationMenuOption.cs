using UnityEngine;
using System.Collections;
using UnityEditor;

public class calibrationMenuOption : MonoBehaviour {


    [MenuItem("Hypercube/Copy current slice calibration", false, 300)]  //1 is prio
    public static void openCubeWindowPrefs()
    {
        canvasCalibrator c = GameObject.FindObjectOfType<canvasCalibrator>();

        if (c)
            c.copyCurrentSliceCalibration();
    }
}
