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
    [RequireComponent(typeof(dataFileDict))]
    public class hypercubeCamera : MonoBehaviour
    {

        public softSliceMode slicing;
        public float overlap = 2f;
        [Tooltip("Softness is calculated for you to blend only overlapping areas. It can be set manually if Slicing is set to SOFT_CUSTOM.")]
        [Range(0.001f, .5f)]
        public float softness = .5f;
        public float brightness = 1f; //  a convenience way to set the brightness of the rendered textures. The proper way is to call 'setTone()' on the canvas
        public float forcedPerspective = 0f; //0 is no forced perspective, other values force a perspective either towards or away from the front of the Volume.

        [Tooltip("This can be used to differentiate between what is empty space, and what is 'black' in Volume.  This Color will be added to everything that has geometry.\n\nNOTE: Black Point can only be used if when soft slicing is being used.")]
        public Color blackPoint;
        public Shader softSliceShader;
        public Camera renderCam;
        public RenderTexture[] sliceTextures;
        public hypercube.castMesh castMeshPrefab;
        public hypercube.castMesh localCastMesh = null;
       

        //store our camera values here.
        float[] nearValues;
        float[] farValues;

        void Start()
        {

            if (!localCastMesh)
            {
                localCastMesh = GameObject.FindObjectOfType<hypercube.castMesh>();
                if (!localCastMesh)
                {
                    //if no canvas exists. we need to have one or the hypercube is useless.
#if UNITY_EDITOR
                    localCastMesh = UnityEditor.PrefabUtility.InstantiatePrefab(castMeshPrefab) as hypercube.castMesh;  //try to keep the prefab connection, if possible
#else
                localCastMesh = Instantiate(canvasPrefab); //normal instantiation, lost the prefab connection
#endif
                }
            }

            loadSettings();
            resetSettings();
        }


        void Update()
        {

            if (!localCastMesh)
                localCastMesh = GameObject.FindObjectOfType<hypercube.castMesh>();

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


            if (slicing == softSliceMode.HARD)
                softness = 0f;

            if (!localCastMesh)
                localCastMesh = GameObject.FindObjectOfType<hypercube.castMesh>();

            if (localCastMesh)
            {
                localCastMesh.setTone(brightness);
                localCastMesh.updateMesh();
            }

            saveSettings();

            //handle softOverlap
            updateOverlap();
        }


        //let the slice image filter shader know how much 'softness' they should apply to the soft overlap
        void updateOverlap()
        {
            if (overlap < 0)
                overlap = 0;

            hypercube.softOverlap o = renderCam.GetComponent<hypercube.softOverlap>();
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

            if (localCastMesh.slices > sliceTextures.Length)
                localCastMesh.slices = sliceTextures.Length;

            for (int i = 0; i < localCastMesh.slices; i++)
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
            if (!localCastMesh)
                return;

            nearValues = new float[localCastMesh.slices];
            farValues = new float[localCastMesh.slices];

            float sliceDepth = transform.lossyScale.z / (float)localCastMesh.slices;

            renderCam.aspect = transform.lossyScale.x / transform.lossyScale.y;
            renderCam.orthographicSize = .5f * transform.lossyScale.y;

            for (int i = 0; i < localCastMesh.slices && i < sliceTextures.Length; i++)
            {
                nearValues[i] = (i * sliceDepth) - (sliceDepth * overlap);
                farValues[i] = ((i + 1) * sliceDepth) + (sliceDepth * overlap);
            }



            updateOverlap();
        }

        public void loadSettings()
        {
            dataFileDict d = GetComponent<dataFileDict>();

            d.clear();
            d.load();

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Loaded saved settings from file.");
#endif

            //these always come from the prefs
            slicing = (softSliceMode)d.getValueAsInt("softSlicing", (int)softSliceMode.SOFT);
            softness = d.getValueAsFloat("shaderOverlap", softness);
            overlap = d.getValueAsFloat("overlap", overlap);
            forcedPerspective = d.getValueAsFloat("forcedPersp", forcedPerspective);

        }

        public void saveSettings()
        {
            dataFileDict d = GetComponent<dataFileDict>();
            if (!d)
                return;
            d.setValue("overlap", overlap.ToString());
            d.setValue("shaderOverlap", softness.ToString());
            d.setValue("softSlicing", ((int)slicing).ToString());
            d.setValue("forcedPersp", forcedPerspective.ToString());
            d.setValue("blackPoint", blackPoint.ToString());

            d.save();
        }


        void OnApplicationQuit()
        {
            saveSettings();
        }
    }