using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class casterWindowPrefs : EditorWindow
{

    int posX = EditorPrefs.GetInt("V_windowOffsetX", 0);
    int posY = EditorPrefs.GetInt("V_windowOffsetY", 0);
    int width = EditorPrefs.GetInt("V_windowWidth", Display.main.renderingWidth); 
    int height = EditorPrefs.GetInt("V_windowHeight", Display.main.renderingHeight);


    [MenuItem("VOLUME/Caster Window Prefs", false, 1)]  //1 is prio
    public static void openCubeWindowPrefs()
    {
        EditorWindow.GetWindow(typeof(casterWindowPrefs), false, "Hypercube Prefs");
    }

	[MenuItem("VOLUME/Save Settings", false, 51)]
    public static void saveCubeSettings()
    {
        hypercubeCamera cam = GameObject.FindObjectOfType<hypercubeCamera>();
        if (cam)
            cam.saveSettings();
    }

	[MenuItem("VOLUME/Load Settings", false, 52)]
    public static void loadCubeSettings()
    {
        hypercubeCamera cam = GameObject.FindObjectOfType<hypercubeCamera>();
        if (cam)
            cam.loadSettings(true); //use force load
    }



    void OnGUI()
    {

        GUILayout.Label("Set Cube Window Preferences", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this tool to align a cube view window to the cube display monitor.\n\nTIP:\nIf it ever blocks important screen elements, the window can be closed with Ctrl + W", MessageType.Info);

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
		EditorGUILayout.HelpBox("TIPS:\nUnity prefers if the cube monitor is left of the main monitor (don't ask me why). \n\nIf any changes are made to the monitor setup, Unity must be off or restarted for this tool to work properly.", MessageType.Info);

		#elif UNITY_STANDALONE_OSX

		EditorGUILayout.HelpBox("TO OPEN THE WINDOW:\nmouse over the desired display, then ⌘ E", MessageType.Info);
#endif

        if (GUILayout.Button("- SAVE -"))
        {
            EditorPrefs.SetInt("V_windowOffsetX", posX);
            EditorPrefs.SetInt("V_windowOffsetY", posY);
            EditorPrefs.SetInt("V_windowWidth", width);
            EditorPrefs.SetInt("V_windowHeight", height);

            casterWindow.V_closeWindow();
        }
    }


}
