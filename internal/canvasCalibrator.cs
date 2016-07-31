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

public enum distortionCompensationType
{
    PIXEL,
    SPATIAL
}

public class canvasCalibrator : MonoBehaviour
{

    public string current;

    public hypercubeCanvas canvas;
    public float brightness = 3f;
    
    [Tooltip("How sensitive do you want your calibrations to be.")]
    public float interval = .5f;
    [Tooltip("Pixel movement will cause the interval to cause interval * pixel movements. Spatial will feel more intuitive if you are working directly on the volume.")]
    public distortionCompensationType relativeTo = distortionCompensationType.PIXEL;

    public KeyCode nextSlice;
    public KeyCode prevSlice;
    public KeyCode highlightUL;
    public KeyCode highlightUR;
    public KeyCode highlightLL;
    public KeyCode highlightLR;
    public KeyCode highlightM;
    public KeyCode up;
    public KeyCode down;
    public KeyCode left;
    public KeyCode right;
    public KeyCode skewXUp;
    public KeyCode skewXDn;
    public KeyCode skewYUp;
    public KeyCode skewYDn;
    public Texture2D calibrationCorner;
    public Texture2D calibrationCenter;

    public Material selectedMat;
    public Material offMat;

  //  public bool forceLoadFromFile = false;

    canvasEditMode m;
    int currentSlice;

    void OnEnable()
    {
        canvas.updateMesh();
    }
    void OnDisable()
    {
        canvas.updateMesh();
    }


    public void copyCurrentSliceCalibration()
    {
        canvas.copyCurrentSliceCalibration(currentSlice);
    }

    // Update is called once per frame
    void Update()
    {
        if (!canvas)
            return;

        canvasEditMode oldMode = m;
        int oldSelection = currentSlice;

        if (Input.GetKeyDown(nextSlice))
        {
            currentSlice++;
            if (currentSlice >= canvas.getSliceCount())
                currentSlice = 0;
        }
        if (Input.GetKeyDown(prevSlice))
        {
            currentSlice--;
            if (currentSlice < 0)
                currentSlice = canvas.getSliceCount() -1;
        }
        else if (Input.GetKeyDown(skewXDn))
        {
            float xPixel = 2f / canvas.sliceWidth;  //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
            if (relativeTo == distortionCompensationType.SPATIAL)
                xPixel *= canvas.getSliceCount();
            canvas.makeSkewAdjustment(currentSlice, true, interval * xPixel );
        }
        else if (Input.GetKeyDown(skewXUp))
        {
            float xPixel = 2f / canvas.sliceWidth;  //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
            if (relativeTo == distortionCompensationType.SPATIAL)
                xPixel *= canvas.getSliceCount();
            canvas.makeSkewAdjustment(currentSlice, true, -interval * xPixel);
        }
        else if (Input.GetKeyDown(skewYUp))
        {
            float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
            canvas.makeSkewAdjustment(currentSlice, false, interval * yPixel);
        }
        else if (Input.GetKeyDown(skewYDn))
        {
            float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
            canvas.makeSkewAdjustment(currentSlice, false, -interval * yPixel);
        }
        else if (Input.GetKeyDown(highlightUL))
        {
            m = canvasEditMode.UL;
        }
        else if (Input.GetKeyDown(highlightUR))
        {
            m = canvasEditMode.UR;
        }
        else if (Input.GetKeyDown(highlightLL))
        {
            m = canvasEditMode.LL;
        }
        else if (Input.GetKeyDown(highlightLR))
        {
            m = canvasEditMode.LR;
        }
        else if (Input.GetKeyDown(highlightM))
        {
            m = canvasEditMode.M;
        }
        else if (Input.GetKeyDown(left))
        {
            float xPixel = 2f / canvas.sliceWidth; //the xpixel makes the movement distance between x/y equivalent (instead of just a local transform)
            if (relativeTo == distortionCompensationType.SPATIAL)
                xPixel *= canvas.getSliceCount();
            canvas.makeAdjustment(currentSlice, m, true, -interval * xPixel);
        }
        else if (Input.GetKeyDown(right))
        {
            float xPixel = 2f / canvas.sliceWidth;  //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
            if (relativeTo == distortionCompensationType.SPATIAL)
                xPixel *= canvas.getSliceCount();
            canvas.makeAdjustment(currentSlice, m, true, interval * xPixel);
        }
        else if (Input.GetKeyDown(down))
        {
            float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
            canvas.makeAdjustment(currentSlice, m, false, -interval * yPixel);
        }
        else if (Input.GetKeyDown(up))
        {
            float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
            canvas.makeAdjustment(currentSlice, m, false, interval * yPixel);
        }

        if (currentSlice != oldSelection || m != oldMode)
        {
            current = "s" + currentSlice + "  " + m.ToString();
            canvas.updateMesh();
        }    
    }

    void OnValidate()
    {

        if (!canvas)
        {
            //thats weird... this should already be set in the prefab, try to automagically fix...
            canvas = GetComponent<hypercubeCanvas>(); 
            if (!canvas)
                Debug.LogError("The calibration tool has no hypercubeCanvas to calibrate!");
        }

        selectedMat.SetFloat("_Mod", brightness);
        offMat.SetFloat("_Mod", brightness);

        canvas.updateMesh();

    }


    public Material[] getMaterials()
    {

        if (m == canvasEditMode.M)
        {
            selectedMat.SetTexture("_MainTex", calibrationCenter);
            selectedMat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
        }
        else
        {
            selectedMat.SetTexture("_MainTex", calibrationCorner);
            if (m == canvasEditMode.UL)
                selectedMat.SetTextureScale("_MainTex", new Vector2(1f, -1f));
            else if (m == canvasEditMode.UR)
                selectedMat.SetTextureScale("_MainTex", new Vector2(-1f, -1f));
            else if (m == canvasEditMode.LL)
                selectedMat.SetTextureScale("_MainTex", new Vector2(1f, 1f));
            else if (m == canvasEditMode.LR)
                selectedMat.SetTextureScale("_MainTex", new Vector2(-1f, 1f));
        }

        Material[] outMats = new Material[canvas.getSliceCount()];
        for (int i = 0; i < canvas.getSliceCount(); i++)
        {
            if (i == currentSlice)
                outMats[i] = selectedMat;
            else
                outMats[i] = offMat;
        }
        return outMats;
    }
}