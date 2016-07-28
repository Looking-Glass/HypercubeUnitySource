using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum softSliceMode
{
    OFF = 0,
    INTEGRAL,
    FREE
}

[ExecuteInEditMode]
[RequireComponent (typeof(dataFileDict))]
public class hypercubeCamera : MonoBehaviour {

    public softSliceMode softSlicing;
    public float overlap = 2f;
    [Range(0.001f, .5f)]
    public float shaderOverlap = .5f;
    public float brightness = 1f; //  a convenience way to set the brightness of the rendered textures. The proper way is to call 'setTone()' on the canvas
    public int slices = 12;
	public float forcedPerspective = 0f; //0 is no forced perspective, other values force a perspective either towards or away from the front of the Volume.

    [Tooltip ("This can be used to differentiate between negative space, and 'black'.  This Color will be added to everything except actual void.")]
    public Color blackPoint; 
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
        if (sliceTextures.Length == 0)
            Debug.LogError("The Hypercube has no slice textures to render to.  Please assign them or reset the prefab.");


        if (slices > sliceTextures.Length)
            slices = sliceTextures.Length;

        if (slices < 1)
            slices = 1;

        if (softSlicing == softSliceMode.OFF)
            shaderOverlap = 0f;

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
        if (overlap < 0)
            overlap = 0;

        softOverlap o = renderCam.GetComponent<softOverlap>();
        if (softSlicing != softSliceMode.OFF)
        {
            if (softSlicing == softSliceMode.INTEGRAL)
                shaderOverlap = overlap / ((overlap * 2f) + 1f);

            o.enabled = true;
            o.setShaderProperties(shaderOverlap, blackPoint);

        }
        else
            o.enabled = false;
    }

    public void render()
    {
        if (overlap > 0f && softSlicing != softSliceMode.OFF)
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

        if (overlap > 0f && softSlicing != softSliceMode.OFF)
            renderCam.gameObject.SetActive(false);

		//TEMP
		//Camera.main.Render();
    }

    //prefs input
    public void softSliceToggle()
    {
        if (softSlicing == softSliceMode.OFF)
            softSlicing = softSliceMode.INTEGRAL;
        else
            softSlicing = softSliceMode.OFF;
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
            nearValues[i] = (i * sliceDepth) - (sliceDepth * overlap);
            farValues[i] = ((i + 1) * sliceDepth) + (sliceDepth * overlap);
        }


			
        updateOverlap();
    }

    public void loadSettings(bool forceLoad = false)
    {
        dataFileDict d = GetComponent<dataFileDict>();

        d.clear();
        d.load();

        //use our save values only in the player only to avoid confusing behaviors in the editor
        //LOAD OUR PREFS
        if (!Application.isEditor || forceLoad)
        {

            UnityEditor.Undo.RecordObject(localCanvas, "Loaded saved settings from file."); //these force the editor to mark the canvas as dirty and save what is loaded.
            UnityEditor.Undo.RecordObject(this, "Loaded saved settings from file.");

            slices = d.getValueAsInt("sliceCount", 10);
            localCanvas.sliceOffsetX = d.getValueAsFloat("offsetX", 0);
            localCanvas.sliceOffsetY = d.getValueAsFloat("offsetY", 0);
            localCanvas.sliceWidth = d.getValueAsFloat("sliceWidth", 1080f);
            localCanvas.sliceHeight = d.getValueAsFloat("pixelsPerSlice", 108f);
            localCanvas.sliceGap = d.getValueAsFloat("sliceGap", 0f);
            localCanvas.flipX = d.getValueAsBool("flipX", false);
            localCanvas.flipY = d.getValueAsBool("flipY", false);
            localCanvas.flipZ = d.getValueAsBool("flipZ", false);
            overlap = d.getValueAsFloat("overlap", 1f);
            shaderOverlap = d.getValueAsFloat("shaderOverlap", 1f);
            softSlicing = (softSliceMode)d.getValueAsInt("softSlicing", (int)softSliceMode.INTEGRAL);
        }

        localCanvas.setCalibrationOffsets(d, sliceTextures.Length);
        localCanvas.updateMesh(slices);       
    }

    public void saveSettings()
    {
        //save our settings whether in editor mode or play mode.
        dataFileDict d = GetComponent<dataFileDict>();
        if (!d)
            return;
        d.setValue("sliceCount", slices.ToString(), true);
        d.setValue("offsetX", localCanvas.sliceOffsetX.ToString(), true);
        d.setValue("offsetY", localCanvas.sliceOffsetY.ToString(), true);
        d.setValue("sliceWidth", localCanvas.sliceWidth.ToString(), true);
        d.setValue("pixelsPerSlice", localCanvas.sliceHeight.ToString(), true);
        d.setValue("sliceGap", localCanvas.sliceGap.ToString(), true);
        d.setValue("flipX", localCanvas.flipX.ToString(), true);
        d.setValue("flipY", localCanvas.flipY.ToString(), true);
        d.setValue("flipZ", localCanvas.flipZ.ToString(), true);
        d.setValue("overlap", overlap.ToString(), true);
        d.setValue("shaderOverlap", shaderOverlap.ToString(), true);
        d.setValue("softSlicing", ((int)softSlicing).ToString(), true);

        if (localCanvas)
            localCanvas.saveCalibrationOffsets(d);

        d.save();
    }

    void OnApplicationQuit()
    {
        saveSettings();
    }
}
