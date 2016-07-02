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
public class canvasCalibrator : MonoBehaviour
{

    public string current;

    public hypercubeCamera cam;

    public float interval = .5f;

    public KeyCode nextSlice;
    public KeyCode highlightUL;
    public KeyCode highlightUR;
    public KeyCode highlightLL;
    public KeyCode highlightLR;
    public KeyCode highlightM;
    public KeyCode up;
    public KeyCode down;
    public KeyCode left;
    public KeyCode right;
    public KeyCode toggleCalibration;
    public Texture2D calibrationCorner;
    public Texture2D calibrationCenter;

    public bool calibration = false;

  //  public bool forceLoadFromFile = false;

    canvasEditMode m;
    int currentSlice;

    void Start()
    {
        updateSelection();
    }

    // Update is called once per frame
    void Update()
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
        else if (Input.GetKeyDown(toggleCalibration))
        {
            cam.renderCam.GetComponent<hypercube.screenOverlay>().enabled = !cam.renderCam.GetComponent<hypercube.screenOverlay>().enabled;
            calibration = cam.renderCam.GetComponent<hypercube.screenOverlay>().enabled;
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
            float xPixel = 4f / cam.localCanvas.sliceWidth; //the xpixel makes the movement distance between x/y equivalent (instead of just a local transform)
            cam.localCanvas.makeAdjustment(currentSlice, m, true, -interval * xPixel);
        }
        else if (Input.GetKeyDown(right))
        {
            float xPixel = 4f / cam.localCanvas.sliceWidth; //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
            cam.localCanvas.makeAdjustment(currentSlice, m, true, interval * xPixel);
        }
        else if (Input.GetKeyDown(down))
        {
            float yPixel = 1f / ((float)cam.localCanvas.sliceHeight * cam.slices);
            cam.localCanvas.makeAdjustment(currentSlice, m, false, -interval * yPixel);
        }
        else if (Input.GetKeyDown(up))
        {
            float yPixel = 1f / ((float)cam.localCanvas.sliceHeight * cam.slices);
            cam.localCanvas.makeAdjustment(currentSlice, m, false, interval * yPixel);
        }

    }

    void OnValidate()
    {
        //if (forceLoadFromFile)
        //{
        //    if (cam)
        //        cam.loadSettings();
        //    forceLoadFromFile = false;
        //}

        if (calibration)
            cam.renderCam.GetComponent<hypercube.screenOverlay>().enabled = true;
        else
            cam.renderCam.GetComponent<hypercube.screenOverlay>().enabled = false;

    }


    void updateSelection()
    {
        current = "s" + currentSlice + "  " + m.ToString();

        //set to slice:
        float sliceSize = 1f / (float)cam.slices;
        transform.localPosition = new Vector3(0f, 0f, (currentSlice * sliceSize) - .5f + (sliceSize / 2)); //the -.5f is an offset because 0 is the center of the cube, sliceSize/2 puts it in the center of the slice

        Material mat = GetComponent<MeshRenderer>().sharedMaterial;
        if (m == canvasEditMode.M)
        {
            mat.SetTexture("_MainTex", calibrationCenter);
            mat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
        }
        else
        {
            mat.SetTexture("_MainTex", calibrationCorner);
            if (m == canvasEditMode.UL)
                mat.SetTextureScale("_MainTex", new Vector2(1f, -1f));
            else if (m == canvasEditMode.UR)
                mat.SetTextureScale("_MainTex", new Vector2(-1f, -1f));
            else if (m == canvasEditMode.LL)
                mat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            else if (m == canvasEditMode.LR)
                mat.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
        }
    }
}