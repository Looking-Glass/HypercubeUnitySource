using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class cubeWindowPrefs : EditorWindow
{

    int posX = EditorPrefs.GetInt("QB_windowOffsetX", 0);
    int width = EditorPrefs.GetInt("QB_windowWidth", Display.main.renderingWidth); 
    int height = EditorPrefs.GetInt("QB_windowHeight", Display.main.renderingHeight);


    
    [MenuItem("QB/Window Prefs")]
    public static void openCubeWindowPrefs()
    {
        EditorWindow.GetWindow(typeof(cubeWindowPrefs), false, "Hypercube Prefs");
    }

    [MenuItem("QB/Save Cube Settings")]
    public static void saveCubeSettings()
    {
        hypercubeCamera cam = GameObject.FindObjectOfType<hypercubeCamera>();
        if (cam)
            cam.saveSettings();
    }

    [MenuItem("QB/Load Cube Settings")]
    public static void loadCubeSettings()
    {
        hypercubeCamera cam = GameObject.FindObjectOfType<hypercubeCamera>();
        if (cam)
            cam.loadSettings(true); //use force load
    }


    void OnGUI()
    {

        GUILayout.Label("Set Cube Window Preferences", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this to align the window position to the cube display monitor.\n\nREMEMBER: If it ever blocks important screen elements, the window can be closed with Ctrl + Q", MessageType.Info);

        posX = EditorGUILayout.IntField("X Position:", posX);
        width = EditorGUILayout.IntField("Width:", width);
        height = EditorGUILayout.IntField("Height:", height);


        if (GUILayout.Button("Move Right +" + Screen.currentResolution.width))
            posX += Screen.currentResolution.width;

        if (GUILayout.Button("Set to current: " + Screen.currentResolution.width + " x " + Screen.currentResolution.height))
        {
            posX = 0;
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Preview it!"))
        {
            EditorPrefs.SetInt("QB_windowOffsetX", posX);
            EditorPrefs.SetInt("QB_windowWidth", width);
            EditorPrefs.SetInt("QB_windowHeight", height); 
            cubeWindow.QB_openWindow();
        }

    }
}
