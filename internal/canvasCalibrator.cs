using UnityEngine;
using System.Collections;

//this is a tool to set calibrations on individual corners of the hypercubeCanvas
//TO USE:
//add this component to an empty gameObject
//connect the canvas to this component
//connect the hypercube camera to this component
//use TAB to cycle through the slices
//use Q E Z C S  to highlight a particular vertex on the slice
//use WADX to make adjustments
//use ENTER to load settings from the file

[ExecuteInEditMode]
public class canvasCalibrator : MonoBehaviour {

    public string current;

    public hypercubeCamera cam;

    public float interval = .5f;

    public KeyCode nextSlice;
    public KeyCode loadSettings;
    public KeyCode highlightUL;
    public KeyCode highlightUR;
    public KeyCode highlightLL;
    public KeyCode highlightLR;
    public KeyCode highlightM;
    public KeyCode up;
    public KeyCode down;
    public KeyCode left;
    public KeyCode right;

    public bool forceLoadFromFile = false;

    canvasEditMode m;
    int currentSlice;

    void Start()
    {
        updateSelection();
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (!cam || !cam.localCanvas)
            return;

        if (Input.GetKeyDown(nextSlice))
        {
            currentSlice++;
            if (currentSlice >= cam.slices)
                currentSlice = 0;

            updateSelection();
        }
        else if (Input.GetKeyDown(loadSettings))
        {
        }
        else if (Input.GetKeyDown(highlightUL))
        {
            m = canvasEditMode.UL;
            updateSelection();
        }
        else if (Input.GetKeyDown(highlightUR))
        {
            m = canvasEditMode.UR;
            updateSelection();
        }
        else if (Input.GetKeyDown(highlightLL))
        {
            m = canvasEditMode.LL;
            updateSelection();
        }
        else if (Input.GetKeyDown(highlightLR))
        {
            m = canvasEditMode.LR;
            updateSelection();
        }
        else if (Input.GetKeyDown(highlightM))
        {
            m = canvasEditMode.M;
            updateSelection();
        }
        else if (Input.GetKeyDown(left))
        {
            float xPixel = 1f / cam.localCanvas.sliceWidth; //the xpixel makes the movement distance between x/y (instead of just a local transform)
            cam.localCanvas.makeAdjustment(currentSlice, m, true, -interval * xPixel);
        }
        else if (Input.GetKeyDown(right))
        {
            float xPixel = 1f / cam.localCanvas.sliceWidth;
            cam.localCanvas.makeAdjustment(currentSlice, m, true, interval * xPixel);
        }
        else if (Input.GetKeyDown(down))
        {
            float yPixel = 1f / (float)Screen.height;
            cam.localCanvas.makeAdjustment(currentSlice, m, false, -interval * yPixel);
        }
        else if (Input.GetKeyDown(up))
        {
            float yPixel = 1f / (float)Screen.height;
            cam.localCanvas.makeAdjustment(currentSlice, m, false, interval * yPixel);
        }

	}

    void OnValidate()
    {
        if (forceLoadFromFile)
        {
            if (cam)
                cam.loadSettings();
            forceLoadFromFile = false;
        }
    }


    void updateSelection()
    {
        current = "s" + currentSlice + "  " + m.ToString();

        //set to slice:
        float sliceSize = 1f / (float)cam.slices;
        transform.localPosition = new Vector3(0f, 0f, (currentSlice * sliceSize) - .5f); //the -.5f is an offset because 0 is the center of the cube
    }
}
