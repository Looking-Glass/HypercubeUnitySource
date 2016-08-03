using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum softSliceMode
{
    HARD = 0,
    SOFT,
    SOFT_CUSTOM
}

[ExecuteInEditMode]
[RequireComponent (typeof(dataFileDict))]
public class hypercubeCamera : MonoBehaviour {

    public softSliceMode slicing;
    public float overlap = 2f;
    [Tooltip("Softness is calculated for you to blend only overlapping areas. It can be set manually if Slicing is set to SOFT_CUSTOM.")]
    [Range(0.001f, .5f)]
    public float softness = .5f;
    public float brightness = 1f; //  a convenience way to set the brightness of the rendered textures. The proper way is to call 'setTone()' on the canvas
    public int slices = 12;
	public float forcedPerspective = 0f; //0 is no forced perspective, other values force a perspective either towards or away from the front of the Volume.

    [Tooltip ("This can be used to differentiate between what is empty space, and what is 'black' in Volume.  This Color will be added to everything that has geometry.\n\nNOTE: Black Point can only be used if when soft slicing is being used.")]
    public Color blackPoint; 
    public Shader softSliceShader;
    public Camera renderCam;
    public RenderTexture[] sliceTextures;
    public hypercubeCanvas canvasPrefab;
    public hypercubeCanvas localCanvas = null;
    public hypercubePreview preview = null;

#if HYPERCUBE_INPUT
    //this will tell our hypercube to try to use the settings stored in the hardware so that we can use any kind of Volume, seamlessly
    //turn it off if you are trying to modify your Volume hardware or are a Volume developer working on prototypes.
    private bool useHardwareCalibrations = true;  
#else
    private bool useHardwareCalibrations = false;  
#endif

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

        if (slicing == softSliceMode.HARD)
            softness = 0f;

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
        if (slicing != softSliceMode.HARD)
        {
            if (slicing == softSliceMode.SOFT)
                softness = overlap / ((overlap * 2f) + 1f);

            o.enabled = true;
            o.setShaderProperties(softness, blackPoint);

        }
        else
            o.enabled = false;
    }

    public void render()
    {
        if (overlap > 0f && slicing != softSliceMode.HARD)
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

        if (overlap > 0f && slicing != softSliceMode.HARD)
            renderCam.gameObject.SetActive(false);

		//TEMP
		//Camera.main.Render();
    }

    //prefs input
    public void softSliceToggle()
    {
        if (slicing == softSliceMode.HARD)
            slicing = softSliceMode.SOFT;
        else
            slicing = softSliceMode.HARD;
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
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(localCanvas, "Loaded saved settings from file."); //these force the editor to mark the canvas as dirty and save what is loaded.
            UnityEditor.Undo.RecordObject(this, "Loaded saved settings from file.");
#endif

            //these always come from the prefs
            slicing = (softSliceMode)d.getValueAsInt("softSlicing", (int)softSliceMode.SOFT);
            softness = d.getValueAsFloat("shaderOverlap", 1f);
            overlap = d.getValueAsFloat("overlap", 1f);    

            if (useHardwareCalibrations && hypercube.input.get() != null && hypercube.input.isHardwareReady() )
            {
                //this will tell the hardware to give us it's specs
                //once complete, it will also update the canvas mesh when the response "get:complete" is received from the serialComm
                hypercube.input.sendCommandToHardware("#get");                
            }
            else //try to read them from our prefs file instead... using defaults as backup.  This will never be as good as using the config stored in the hardware.
            {
                slices = d.getValueAsInt("sliceCount", 10);
                localCanvas.sliceOffsetX = d.getValueAsFloat("offsetX", 0);
                localCanvas.sliceOffsetY = d.getValueAsFloat("offsetY", 0);
                localCanvas.sliceWidth = d.getValueAsFloat("sliceWidth", 1080f);
                localCanvas.sliceHeight = d.getValueAsFloat("pixelsPerSlice", 108f);
                localCanvas.sliceGap = d.getValueAsFloat("sliceGap", 0f);
                localCanvas.flipX = d.getValueAsBool("flipX", false);
                localCanvas.flipY = d.getValueAsBool("flipY", false);
                localCanvas.flipZ = d.getValueAsBool("flipZ", false);

                localCanvas.setCalibrationOffsets(d, sliceTextures.Length);
                localCanvas.updateMesh(slices); 
            }           
        }       
              
    }

    public void saveSettings()
    {
        //save our settings whether in editor mode or play mode.
        dataFileDict d = GetComponent<dataFileDict>();
        if (!d)
            return;
        d.setValue("overlap", overlap.ToString());
        d.setValue("shaderOverlap", softness.ToString());
        d.setValue("softSlicing", ((int)slicing).ToString());

        if (useHardwareCalibrations && hypercube.input.get() != null && hypercube.input.isHardwareReady())
        {
           // hypercube.input.saveValueToHardware("sliceCount", slices);  //TODO!!!
            //TODO finish
        }
        else //save hardware settings to our prefs file.
        {
            d.setValue("sliceCount", slices);
            d.setValue("offsetX", localCanvas.sliceOffsetX);
            d.setValue("offsetY", localCanvas.sliceOffsetY);
            d.setValue("sliceWidth", localCanvas.sliceWidth);
            d.setValue("pixelsPerSlice", localCanvas.sliceHeight);
            d.setValue("sliceGap", localCanvas.sliceGap);
            d.setValue("flipX", localCanvas.flipX);
            d.setValue("flipY", localCanvas.flipY);
            d.setValue("flipZ", localCanvas.flipZ);

            if (localCanvas)
                localCanvas.saveCalibrationOffsets(d);
        }
        
        d.save();
    }



    void OnApplicationQuit()
    {
        saveSettings();
    }
}
