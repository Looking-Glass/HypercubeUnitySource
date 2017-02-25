using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class casterWindowPrefs : EditorWindow
{


    int posX = 0;
    int posY = 0;
    int width = 1920;  //Display.main.renderingWidth
    int height = 1080;

    void Awake()
    {
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
        width = EditorPrefs.GetInt("V_windowWidth", width);  //Display.main.renderingWidth
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
        	
		#if UNITY_STANDALONE_OSX 
            "TO OPEN THE WINDOW:\nmouse over Volume's display, then ⌘E", MessageType.Info);
#else
            "Toggle the Caster window with Ctrl+E", MessageType.Info);
		#endif


        posX = EditorGUILayout.IntField("X Position:", posX);
        posY = EditorGUILayout.IntField("Y Position:", posY);
        width = EditorGUILayout.IntField("Width:", width);
        height = EditorGUILayout.IntField("Height:", height);


        if (GUILayout.Button("Move Right +" + Screen.currentResolution.width))
            posX += Screen.currentResolution.width;


        if (GUILayout.Button("Set to current: " + Screen.currentResolution.width + " x " + Screen.currentResolution.height))
        {
            posX = 0;
            posY = 0;
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
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


}
