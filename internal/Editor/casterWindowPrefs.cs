using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class casterWindowPrefs : EditorWindow
{


    int posX = 0;
    int posY = 0;
    int width = 1920;  
    int height = 1080;

    void Awake()
    {
        //try to use prefs from our loaded settings.
        getVolumeScreenDims(out width, out height);

        //set the prefs, in case they don't exist.  This may prevent issues for first time user where gui does not match settings.
        if (!EditorPrefs.HasKey("V_windowOffsetX"))
            EditorPrefs.SetInt("V_windowOffsetX", posX);
        if (!EditorPrefs.HasKey("V_windowOffsetY"))
            EditorPrefs.SetInt("V_windowOffsetY", posY);
        if (!EditorPrefs.HasKey("V_windowWidth"))
            EditorPrefs.SetInt("V_windowWidth", width);
        if (!EditorPrefs.HasKey("V_windowHeight"))
            EditorPrefs.SetInt("V_windowHeight", height);


        posX = EditorPrefs.GetInt("V_windowOffsetX", posX);
        posY = EditorPrefs.GetInt("V_windowOffsetY", posY);
        width = EditorPrefs.GetInt("V_windowWidth", width);  
        height = EditorPrefs.GetInt("V_windowHeight", height);
    
    }

    [MenuItem("Hypercube/Caster Window Prefs", false, 1)]  //1 is prio
    public static void openCubeWindowPrefs()
    {
        EditorWindow.GetWindow(typeof(casterWindowPrefs), false, "Caster Prefs");
    }



    void OnGUI()
    {

        GUILayout.Label("Caster Window Prefs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this tool to align a Volume Caster Window to the Volume display.\n\n" +

#if UNITY_EDITOR_OSX
            "TO OPEN THE WINDOW:\nmouse over Volume's display, then ⌘E", MessageType.Info);
#else
            "Toggle the Caster window with Ctrl+E", MessageType.Info);
#endif


        posX = EditorGUILayout.IntField("X Position:", posX);
        posY = EditorGUILayout.IntField("Y Position:", posY);
        width = EditorGUILayout.IntField("Width:", width);
        height = EditorGUILayout.IntField("Height:", height);


    //    if (GUILayout.Button("Move Right +" + Screen.currentResolution.width))
    //        posX += Screen.currentResolution.width;


        if (GUILayout.Button("Set to current: " + Screen.currentResolution.width + " x " + Screen.currentResolution.height))
        {
#if UNITY_EDITOR_WIN
            posX = -Screen.currentResolution.width; //WIN likes to have the screen on the left
            posY = 0;
#else
            posX = 0;
            posY = 0;
#endif
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }


        int vx = 0;
        int vy = 0;
        getVolumeScreenDims(out vx, out vy);

        if (vx != Screen.currentResolution.width || vy != Screen.currentResolution.height)
        {
            if (GUILayout.Button("Set to Volume: " + vx + " x " + vy))
            {
#if UNITY_EDITOR_WIN
                posX = -vx; //WIN likes to have the screen on the left
                posY = 0;
#else
                posX = 0;
                posY = 0;
#endif

                width = vx;
                height = vy;
            }
        }

        GUILayout.FlexibleSpace();



#if UNITY_EDITOR_WIN
		EditorGUILayout.HelpBox("TIPS:\nUnity prefers if the Volume monitor is left of the main monitor (don't ask me why). \n\nIf any changes are made to the monitor setup, Unity must be off or restarted for this tool to work properly.", MessageType.Info);
#endif

        //if (GUILayout.Button("- SAVE -"))
        if (GUI.changed)
        {
            EditorPrefs.SetInt("V_windowOffsetX", posX);
            EditorPrefs.SetInt("V_windowOffsetY", posY);
            EditorPrefs.SetInt("V_windowWidth", width);
            EditorPrefs.SetInt("V_windowHeight", height);

         //   hypercube.casterWindow.closeWindow();
        }
    }


    /// <summary>
    /// Tries to get the resolution of a connected Volume.
    /// If it fails it returns the current screen resolution.
    /// </summary>
    /// <param name="x">screen width</param>
    /// <param name="y">screen height</param>
    public static void getVolumeScreenDims(out int x, out int y)
    {
        x = Screen.currentResolution.width;
        y = Screen.currentResolution.height;
        if (hypercube.castMesh.canvas && hypercube.castMesh.canvas.GetComponent<dataFileDict>())
        {
            dataFileDict d = hypercube.castMesh.canvas.GetComponent<dataFileDict>();
            x = d.getValueAsInt("volumeResX", x);
            y = d.getValueAsInt("volumeResY", y);
        }
    }


}
