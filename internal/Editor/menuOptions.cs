using UnityEngine;
using System.Collections;
using UnityEditor;

namespace hypercube
{
    public class menuOptions : MonoBehaviour
    {

        //[MenuItem("Hypercube/Load Settings", false, 52)]
        //public static void loadHardwareCalibrationSettings()
        //{
        //    hypercube.castMesh c = GameObject.FindObjectOfType<hypercube.castMesh>();
        //    if (c)
        //    {
        //        if (c.loadSettings()) //to prevent spamming, this does not provide feedback when settings are loaded
        //            Debug.Log("Hypercube settings loaded.");
        //    }
        //    else
        //        Debug.LogWarning("No castMesh was found, and therefore no loading occurred.");
        //}


        [MenuItem("Hypercube/Load Volume friendly Unity Prefs", false, 600)]
        public static void setVolumeFriendlyPrefs()
        {

            Debug.Log("Removing skybox...");
            Debug.Log("Removing ambient light...");
            //turn off ambient stuff, they can cause lighting anomalies of not specifically set or handled
            RenderSettings.skybox = null;
            RenderSettings.ambientLight = Color.black;

            Debug.Log("Ensuring editor set to 3D mode...");
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;

            Debug.Log("Ensuring Scene views set to 3D mode...");
            foreach (SceneView s in SceneView.sceneViews)
            {
                s.in2DMode = false;
            }

            Debug.Log("Setting compatibility level to .Net 2.0 (necessary for receiving input from Volume)...");
            Debug.Log("Setting HYPERCUBE_INPUT preprocessor macro (necessary for receiving input from Volume)...");


            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "HYPERCUBE_INPUT"); //add HYPERCUBE_INPUT to prerprocessor defines   

            Debug.Log("Setting our standalone build target...");

#if UNITY_5_6_OR_NEWER
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_2_0);
#if UNITY_EDITOR_WIN
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows);
#elif UNITY_EDITOR_OSX
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSXUniversal)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneOSXUniversal);
#endif
#else
                PlayerSettings.apiCompatibilityLevel = ApiCompatibilityLevel.NET_2_0;
#if UNITY_EDITOR_WIN
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows);
#elif UNITY_EDITOR_OSX
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSXUniversal)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneOSXUniversal);
#endif
#endif
        }


        [MenuItem("Hypercube/Print Debug Info", false, 601)]
        public static void printDebugStats()
        {
            Debug.Log(utils.getDebugStats());
        }

        [MenuItem("Hypercube/About Hypercube", false, 602)]
        public static void aboutHypercube()
        {
            Debug.Log("Hypercube: Volume Plugin  -  Version: " + hypercubeCamera.version + "  -  by Looking Glass Factory, Inc.  Visit lookingglassfactory.com to learn more!");
        }

    }
}
