using UnityEngine;
using System.Collections;
using UnityEditor;

public class calibrationMenuOption : MonoBehaviour
{

#if HYPERCUBE_INPUT
#if HYPERCUBE_DEV
    [MenuItem("Hypercube/Copy current slice calibration", false, 300)]  //# is prio
    public static void openCubeWindowPrefs()
    {
        canvasCalibrator c = GameObject.FindObjectOfType<canvasCalibrator>();

        if (c)
            c.copyCurrentSliceCalibration();
    }

     [MenuItem("Hypercube/Save Settings to Hardware", false, 301)] 
    public static void saveToHardware()
    {
        if (hypercube.input.isHardwareReady())
        {
            //asynchronously save appropriate values to the chip
            hypercubeCamera c = GameObject.FindObjectOfType<hypercubeCamera>();
            c.saveSettingsToHardware();
        }
    }

    [MenuItem("Hypercube/Clear all hardware values - DANGER!", false, 302)] 
    public static void clearHardwareValues()
    {
        hypercube.input.clearAllHardwareValues();
    }

#endif
#endif


}
