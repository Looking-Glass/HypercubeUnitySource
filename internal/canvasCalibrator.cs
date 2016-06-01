using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public Texture2D calibrationCorner;
    public Texture2D calibrationCenter;

    public bool calibration = false;

    public GameObject selectedSlice;
    public GameObject offSliceParent;
    public GameObject calibrationSlicePrefab;
    List<GameObject> calibrationSlices = new List<GameObject>(); //these must be separate objects, not a single mesh or the cameras have trouble differentiating the slices

   // public bool forceLoadFromFile = false;

    canvasEditMode m;
    int currentSlice;

    void Start()
    {
        //clean up, just in case
        foreach (Transform s in offSliceParent.transform)
        {
            DestroyImmediate(s.gameObject);
        }
        calibrationSlices.Clear();

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
            float xPixel = 2f / cam.localCanvas.sliceWidth; //the xpixel makes the movement distance between x/y equivalent (instead of just a local transform)
            cam.localCanvas.makeAdjustment(currentSlice, m, true, -interval * xPixel);
        }
        else if (Input.GetKeyDown(right))
        {
            float xPixel = 2f / cam.localCanvas.sliceWidth; //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
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
            offSliceParent.SetActive(true);
        else
            offSliceParent.SetActive(false);

        updateSelection();
    }


    void updateSelection()
    {
        current = "s" + currentSlice + "  " + m.ToString();

        Material mat = selectedSlice.GetComponent<MeshRenderer>().sharedMaterial;
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

        constructSlices();

        float sliceSize = 1f / (float)cam.slices;
        for (int s = 0; s < calibrationSlices.Count; s++)
        {
            if (s == currentSlice || s == currentSlice +1 || s == currentSlice -1) //hide the current slice
            {
                calibrationSlices[s].SetActive(false);
            }
            else
            {
                calibrationSlices[s].SetActive(true);
                calibrationSlices[s].transform.localPosition = new Vector3(0f, 0f, (s * sliceSize) - .5f + (sliceSize / 2)); //the -.5f is an offset because 0 is the center of the cube, sliceSize/2 puts it in the center of the slic            
            }
        }


        //set the selection slice, which is the gameObject of this script
        selectedSlice.transform.localPosition = new Vector3(0f, 0f, (currentSlice * sliceSize) - .501f + (sliceSize / 2)); //the -.5f is an offset because 0 is the center of the cube, sliceSize/2 puts it in the center of the slice
        
    }


    void constructSlices()
    {
        //add slices if necessary
        while (calibrationSlices.Count < cam.slices)
        {
            GameObject s = Instantiate(calibrationSlicePrefab);
            s.transform.parent = offSliceParent.transform;
            calibrationSlices.Add(s);
        }

        //remove them if necessary.  The slices must be separate gameObjects or the cameras have trouble distinguishing their distances.
        while (calibrationSlices.Count > cam.slices)
        {
            DestroyImmediate(calibrationSlices[calibrationSlices.Count - 1]);
            calibrationSlices.RemoveAt(calibrationSlices.Count - 1);
        }
    }

   

}
