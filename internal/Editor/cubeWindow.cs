using UnityEngine;
using UnityEditor;
using System.Collections;

//manages a full screen display of the render texture of the hypercube
//Ctrl + Q closes it


public class cubeWindow : EditorWindow
{
    Camera canvasCam;
    hypercubeCanvas canvas;
    RenderTexture renderTexture;
    Texture2D blackBG; //this keeps the rtt from blending with the grey color of the editorWindow itself


    [MenuItem("Volume/Open Window _%e", false, 10)]
    public static void V_openWindow()
    {
        //allow only 1 window
        EditorWindow window = EditorWindow.GetWindow(typeof(cubeWindow), true, "");
		if (window) 
		{
			window.Close ();
			//window.position = new Rect(posX, posY, w, h);
			//window.ShowPopup ();
			//return;
		}
	

        int posX = EditorPrefs.GetInt("V_windowOffsetX", 0);
        int posY = EditorPrefs.GetInt("V_windowOffsetY", 0);
        int w = EditorPrefs.GetInt("V_windowWidth", Display.main.renderingWidth);
        int h = EditorPrefs.GetInt("V_windowHeight", Display.main.renderingHeight); 


        //create a new window
        cubeWindow win = ScriptableObject.CreateInstance<cubeWindow>();
        win.position = new Rect(posX, posY, w, h);
        win.autoRepaintOnSceneChange = true;  //this lets it update any changes.  see also: http://docs.unity3d.com/ScriptReference/EditorWindow-autoRepaintOnSceneChange.html
        win.ShowPopup();
    }

    [MenuItem("Volume/Close Window _%w", false, 11)] //see  https://docs.unity3d.com/ScriptReference/MenuItem.html)
    public static void V_closeWindow()
    {
        EditorWindow w = EditorWindow.GetWindow(typeof(cubeWindow), true, "");
		w.Focus ();
		w.Close ();

        //stop deforming the output view
        hypercubeCanvas canvas = GameObject.FindObjectOfType<hypercubeCanvas>();
        if (canvas)
            canvas.usingCustomDimensions = false;

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

        //force things to reset. set it all up in update so that it will be dynamic
        canvas = null;
        canvasCam = null; 

        ensureTextureIntegrity();
    }

    public void Update()
    {
        if (canvas == null || canvasCam == null)
        {
            canvas = GameObject.FindObjectOfType<hypercubeCanvas>();
            if (canvas)
            {
                canvasCam = canvas.GetComponent<Camera>();
                canvas.setCustomWidthHeight(position.width, position.height);
            }
            else
            {
                Debug.LogWarning("No Hypercube Canvas found, closing window.");
                V_closeWindow();
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

        //let the hypercube know that it is not using the gameWindow for rendering, and to rely only on the given settings.
        if (canvas)
            canvas.setCustomWidthHeight(position.width, position.height); //try to set it to proper dims
    }

}
