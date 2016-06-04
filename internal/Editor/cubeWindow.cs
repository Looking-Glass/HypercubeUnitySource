using UnityEngine;
using UnityEditor;
using System.Collections;

//manages a full screen display of the render texture of the hypercube
//Ctrl + Q closes it


public class cubeWindow : EditorWindow
{
    Camera canvasCam;
    RenderTexture renderTexture;
    Texture2D blackBG; //this keeps the rtt from blending with the grey color of the editorWindow itself


    [MenuItem("Hypercube/Open Window")]
    public static void QB_openWindow()
    {
        //allow only 1 window
        EditorWindow window = EditorWindow.GetWindow(typeof(cubeWindow), true, "");
        if (window)
            window.Close();

        int posX = EditorPrefs.GetInt("QB_windowOffsetX", 0);
        int w = EditorPrefs.GetInt("QB_windowWidth", Display.main.renderingWidth);
        int h = EditorPrefs.GetInt("QB_windowHeight", Display.main.renderingHeight); 

        //create a new window
        cubeWindow win = ScriptableObject.CreateInstance<cubeWindow>();
        win.position = new Rect(posX, 0, w, h);
        win.autoRepaintOnSceneChange = true;  //this lets it update any changes.  see also: http://docs.unity3d.com/ScriptReference/EditorWindow-autoRepaintOnSceneChange.html
        win.ShowPopup();
    }

    [MenuItem("Hypercube/Close Window _%q")] //see  https://docs.unity3d.com/ScriptReference/MenuItem.html)
    public static void QB_closeWindow()
    {
        EditorWindow.GetWindow(typeof(cubeWindow), true, "").Close();
    }



    public void Awake()
    {
        //close the game window, if it's up.
        //EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow)) as EditorWindow[];
        //foreach (EditorWindow w in allWindows)
        //{
        //    if (w.GetType().ToString() == "UnityEditor.GameView")
        //        w.Close();
        //}

        ensureTextureIntegrity();
    }

    public void Update()
    {
        if (canvasCam == null)
        {
            hypercubeCanvas canvas = GameObject.FindObjectOfType<hypercubeCanvas>();
            if (canvas)
            {
                canvasCam = canvas.GetComponent<Camera>();
                canvas.setCustomWidthHeight(position.width, position.height);
            }
            else
            {
                Debug.LogWarning("No Hypercube Canvas found, closing window.");
                QB_closeWindow();
                return;
            }
        }

        if (canvasCam != null)
        {
            canvasCam.targetTexture = renderTexture;
            canvasCam.Render();
            canvasCam.targetTexture = null;
        }
        else
            return;

        ensureTextureIntegrity(); //this call is for during Editor
    }

    void OnGUI()
    {
        ensureTextureIntegrity();//this call is for during Play

        GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), blackBG);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, position.width, position.height), renderTexture);
    }


    void ensureTextureIntegrity()
    {
        if (!renderTexture || renderTexture.width != position.width || renderTexture.height != position.height)
            renderTexture = new RenderTexture((int)position.width, (int)position.height, (int)RenderTextureFormat.ARGB32);

        if (!blackBG)
        {
            blackBG = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            blackBG.SetPixel(0, 0, new Color(0f, 0f, 0f));
            blackBG.Apply();
        }
    }

    void OnDestroy()
    {
        //stop deforming the output view
        hypercubeCanvas canvas = GameObject.FindObjectOfType<hypercubeCanvas>();
        if (canvas)
            canvas.usingCustomDimensions = false;
    }


}
