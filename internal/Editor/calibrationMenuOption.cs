using UnityEngine;
using System.Collections;
using UnityEditor;

namespace hypercube
{
    public class calibrationMenuOption : MonoBehaviour
    {
#if HYPERCUBE_DEV
        [MenuItem("Hypercube/Save Settings", false, 51)]
        public static void saveCubeSettings()
        {
            hypercube.castMesh cam = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (cam)
                cam.saveConfigSettings();
        }
#endif

        [MenuItem("Hypercube/Load Settings", false, 52)]
        public static void loadCubeSettings()
        {
            hypercube.castMesh cam = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (cam)
                cam.loadSettings();
        }

#if HYPERCUBE_DEV
        [MenuItem("Hypercube/Copy current slice calibration", false, 300)]  //# is prio
        public static void openCubeWindowPrefs()
        {
            calibrator c = GameObject.FindObjectOfType<calibrator>();

            if (c)
                c.copyCurrentSliceCalibration();
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
