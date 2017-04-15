
//this class updates the date in hypercubeBuildRecord.txt just before building
//the file is included in the build and can be used to diff versions of applications for debugging.

//see: https://docs.unity3d.com/560/Documentation/ScriptReference/Build.IPreprocessBuild.OnPreprocessBuild.html

#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

class buildDateMarker : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        System.IO.File.WriteAllText(Application.dataPath + "/Hypercube/internal/hypercubeBuildRecord.txt", System.DateTime.Now.ToString("F"));
        AssetDatabase.Refresh();
    }
}
#endif

