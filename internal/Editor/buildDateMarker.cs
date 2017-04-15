
//this class updates the date in hypercubeBuildRecord.txt just before building
//the file is included in the build and can be used to diff versions of applications for debugging.

#if UNITY_5_6_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.Build;

class buildDateMarker : IPreprocessBuild
{
    public int CallbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        File.WriteAllText(Application.dataPath + "/Hypercube/internal/hypercubeBuildRecord.txt", System.DateTime.Today.ToString("F")));
        AssetDatabase.Refresh();
    }
}
#endif

