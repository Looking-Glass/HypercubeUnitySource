using UnityEngine;
using System.Collections;
using UnityEditor;

namespace hypercube
{
    public class calibrationMenuOption : MonoBehaviour
    {


#if HYPERCUBE_DEV
        [MenuItem("Hypercube/Copy current slice calibration", false, 300)]  //# is prio
        public static void openCubeWindowPrefs()
        {
            calibrator c = GameObject.FindObjectOfType<calibrator>();

            if (c)
                c.copyCurrentSliceCalibration();
        }

        [MenuItem("Hypercube/Save Settings to Hardware", false, 301)]
        public static void saveToHardware()
        {
            //asynchronously save appropriate values to the chip
            castMesh c = GameObject.FindObjectOfType<castMesh>();
            c.saveConfigSettings();
        }
#if HYPERCUBE_INPUT
        [MenuItem("Hypercube/Clear all hardware values - DANGER!", false, 302)]
        public static void clearHardwareValues()
        {
            hypercube.input.clearAllHardwareValues();
        }

#endif
#endif


    }
}
