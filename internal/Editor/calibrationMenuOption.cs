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
            hypercube.castMesh c = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (c)
                c.saveConfigSettings();
            else
                Debug.LogWarning("No castMesh was found, and therefore no saving could occur.");
        }
#endif

        [MenuItem("Hypercube/Load Settings", false, 52)]
        public static void loadCubeSettings()
        {
            hypercube.castMesh c = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (c)
                c.loadSettings();
            else
                Debug.LogWarning("No castMesh was found, and therefore no loading occurred.");
        }

#if HYPERCUBE_DEV
        [MenuItem("Hypercube/Copy current slice calibration", false, 300)]  //# is prio
        public static void openCubeWindowPrefs()
        {
            calibrator c = GameObject.FindObjectOfType<calibrator>();

            if (c)
                c.copyCurrentSliceCalibration();
            else
                Debug.LogWarning("No calibrator was found, and therefore no copying occurred.");
        }

#endif
    }
}
