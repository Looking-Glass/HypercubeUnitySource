using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent (typeof(dataFileAssoc))]
public class hypercubeCamera : MonoBehaviour {

    public float overlap = .2f; 
    public float brightness = 1f; //  a convenience way to set the brightness of the rendered textures. The proper way is to call 'setTone()' on the canvas
    public int slices = 12;
	public float forcedPerspective = 0f; //0 is no forced perspective, other values force a perspective either towards or away from the front of the Volume.
    public bool useSoftSlices = true;
    public Shader softSliceShader;
    public Camera renderCam;
    public RenderTexture[] sliceTextures;
    public hypercubeCanvas canvasPrefab;
    public hypercubeCanvas localCanvas = null;
    public hypercubePreview preview = null;
    //public hypercubeCanvas getLocalCanvas() { return localCanvas; }

    //store our camera values here.
    float[] nearValues;
    float[] farValues;

    void Start()
    {

        if (!localCanvas)
        {
            localCanvas = GameObject.FindObjectOfType<hypercubeCanvas>();
            if (!localCanvas)
            {
                //if no canvas exists. we need to have one or the hypercube is useless.
#if UNITY_EDITOR
                localCanvas = UnityEditor.PrefabUtility.InstantiatePrefab(canvasPrefab) as hypercubeCanvas;  //try to keep the prefab connection, if possible
#else
                localCanvas = Instantiate(canvasPrefab); //normal instantiation, lost the prefab connection
#endif
            }
        }

        if (!preview)
            preview = GameObject.FindObjectOfType<hypercubePreview>();


        loadSettings();
        resetSettings();
    }


    void Update()
    {
        if (transform.hasChanged)
        {
            resetSettings(); //comment this line out if you will not be scaling your cube during runtime
        }
        render();
    }

    void OnValidate()
    {
        if (slices < 1)
            slices = 1;
        if (slices > sliceTextures.Length)
            slices = sliceTextures.Length;

        if (localCanvas)
        {
            localCanvas.setTone(brightness);
            localCanvas.updateMesh(slices);
        }
        if (preview)
        {
            preview.sliceCount = slices;
            preview.sliceDistance = 1f / (float)slices;
            preview.updateMesh();
        }

        //handle softOverlap
        updateOverlap();
    }

    //let the slice image filter shader know how much 'softness' they should apply to the soft overlap
    void updateOverlap()
    {
        softOverlap o = renderCam.GetComponent<softOverlap>();
        if (useSoftSlices)
        {
            o.enabled = true;
            o.setOverlapPercentage(overlap);
        }
        else
            o.enabled = false;
    }

    public void render()
    {
        if (overlap > 0f && useSoftSlices)
            renderCam.gameObject.SetActive(true); //setting it active/inactive is only needed so that OnRenderImage() will be called on softOverlap.cs for the post process effect

		float baseViewAngle = renderCam.fieldOfView;

        for (int i = 0; i < slices; i++)
        {
			renderCam.fieldOfView = baseViewAngle + (i * forcedPerspective); //allow forced perspective or perspective correction

            renderCam.nearClipPlane = nearValues[i];
            renderCam.farClipPlane = farValues[i];
            renderCam.targetTexture = sliceTextures[i];
            renderCam.Render();
        }

		renderCam.fieldOfView = baseViewAngle;

        if (overlap > 0f && useSoftSlices)
            renderCam.gameObject.SetActive(false);

		//TEMP
		//Camera.main.Render();
    }

    //prefs input
    public void softSliceToggle()
    {
        useSoftSlices = !useSoftSlices;
    }
    public void overlapUp()
    {
        overlap += .05f;
    }
    public void overlapDown()
    {
        overlap -= .05f;
    }


    //NOTE that if a parent of the cube is scaled, and the cube is arbitrarily rotated inside of it, it will return wrong lossy scale.
    // see: http://docs.unity3d.com/ScriptReference/Transform-lossyScale.html
    //TODO change this to use a proper matrix to handle local scale in a heirarchy
    public void resetSettings()
    {
        nearValues = new float[slices];
        farValues = new float[slices];

        float sliceDepth = transform.lossyScale.z/(float)slices;

        renderCam.aspect = transform.lossyScale.x / transform.lossyScale.y;
		renderCam.orthographicSize = .5f * transform.lossyScale.y;

        for (int i = 0; i < slices && i < sliceTextures.Length; i ++ )
        {
            nearValues[i] = i * sliceDepth - (sliceDepth * overlap);
            farValues[i] = (i + 1) * sliceDepth + (sliceDepth * overlap);
        }
			
        updateOverlap();
    }

    public void loadSettings(bool forceLoad = false)
    {
        dataFileAssoc d = GetComponent<dataFileAssoc>();

        d.clear();
        d.load();

        //use our save values only in the player only to avoid confusing behaviors in the editor
        //LOAD OUR PREFS
        if (!Application.isEditor || forceLoad)
        {
            slices = d.getValueAsInt("sliceCount", 10);
            localCanvas.sliceOffsetX = d.getValueAsFloat("offsetX", 0);
            localCanvas.sliceOffsetY = d.getValueAsFloat("offsetY", 0);
            localCanvas.sliceWidth = d.getValueAsFloat("sliceWidth", 800f);
            localCanvas.sliceHeight = d.getValueAsFloat("pixelsPerSlice", 68f);
            localCanvas.flipX = d.getValueAsBool("flipX", false);
            overlap = d.getValueAsFloat("overlap", .15f);
            useSoftSlices = d.getValueAsBool("useSoftSlices", true);
        }

        localCanvas.setCalibrationOffsets(d, sliceTextures.Length);
        localCanvas.updateMesh(slices);
    }

    public void saveSettings()
    {
        //save our settings whether in editor mode or play mode.
        dataFileAssoc d = GetComponent<dataFileAssoc>();
        if (!d)
            return;
        d.setValue("sliceCount", slices.ToString(), true);
        d.setValue("offsetX", localCanvas.sliceOffsetX.ToString(), true);
        d.setValue("offsetY", localCanvas.sliceOffsetY.ToString(), true);
        d.setValue("sliceWidth", localCanvas.sliceWidth.ToString(), true);
        d.setValue("pixelsPerSlice", localCanvas.sliceHeight.ToString(), true);
        d.setValue("flipX", localCanvas.flipX.ToString(), true);
        d.setValue("overlap", overlap.ToString(), true);
        d.setValue("useSoftSlices", useSoftSlices.ToString(), true);

        if (localCanvas)
            localCanvas.saveCalibrationOffsets(d);

        d.save();
    }

    void OnApplicationQuit()
    {
        saveSettings();
    }
}
