using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//This script manages the final mesh that is displayed on Volume (the castMesh)
//the surface of the castMesh translates the rendered slices into a form that the Volume can display properly.

namespace hypercube
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(dataFileDict))]
    public class castMesh : MonoBehaviour
    {
        public string volumeModelName { get; private set; }
        public float volumeHardwareVer { get; private set; }

        //stored aspect ratio multipliers, each with the corresponding axis set to 1
        public Vector3 aspectX { get; private set; }
        public Vector3 aspectY { get; private set; }
        public Vector3 aspectZ { get; private set; }

        public bool foundConfigFile { get; private set; }

        public int slices = 12;
        public int getSliceCount() { return slices; } //a safe accessor, since its accessed constantly.

        int xArticulation = 1; //these will be set by the calibration data
        int yArticulation = 1;
        float[] calibrationData = null;

        public bool flipX = false;  //modifier values, by the user.
        public bool flipY = false;
        public bool flipZ = false;

        public bool _flipX { get; private set; } //true  values, coming from the config file.
        public bool _flipY { get; private set; }
        public bool _flipZ { get; private set; }


        private static bool _drawOccludedMode = false; 
        public bool drawOccludedMode
        {
            get
            {
                return _drawOccludedMode;
            }
            set
            {
                _drawOccludedMode = value;
                updateMesh();
            }
        }



        public float zPos = .01f;
        [Range(1, 20)]

        public int tesselation = 8;
        public GameObject sliceMesh;

        [Tooltip("The materials set here will be applied to the dynamic mesh")]
        public List<Material> canvasMaterials = new List<Material>();
        public Material occlusionMaterial;

        [HideInInspector]
        public bool usingCustomDimensions = false; //this is an override so that the canvas can be told to obey the dimensions of some particular output w/h screen other than the game window

        float customWidth;
        float customHeight;

        

        public hypercubePreview preview = null;

#if HYPERCUBE_DEV
        public calibrator calibrator = null;
#endif

        public Material casterMaterial;

        [Tooltip("This path is how we find the calibration and system settings in the internal drive for the Volume in use. Don't change unless you know what you are changing.")]
        public string relativeSettingsPath;

        void Awake()
        {
            foundConfigFile = false;
#if !UNITY_EDITOR
            Debug.Log("Loading Hypercube Tools v" + hypercubeCamera.version + " on  Unity v" + Application.unityVersion);
#endif
        }

        void Start()
        {
            if (!preview)
                preview = GameObject.FindObjectOfType<hypercubePreview>();

            loadSettings();
        }

        public void setCustomWidthHeight(float w, float h)
        {
            if (w == 0 || h == 0) //bogus values. Possible, if the window is still setting up.
                return;

            usingCustomDimensions = true;
            customWidth = w;
            customHeight = h;
        }



        public bool loadSettings()
        {

            dataFileDict d = GetComponent<dataFileDict>();

            //use this path as a base path to search for the drive provided with Volume.
             foundConfigFile = hypercube.utils.getConfigPath(relativeSettingsPath, out d.fileName);    //return it to the dataFileDict as an absolute path within that drive if we find it  (ie   G:/volumeConfigurationData/prefs.txt).
            
            d.clear();

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Loaded calibration settings from file."); //these force the editor to mark the canvas as dirty and save what is loaded.
#endif

            if (!d.load()) //we failed to load the file!  ...use backup defaults.
            {
                Debug.LogWarning("Could not read calibration data from Volume!\nIs Volume connected via USB? Using defaults..."); //This will never be as good as using the config stored with the hardware and the view will have distortions in Volume's display.
                foundConfigFile = false;
            }
                
            volumeModelName = d.getValue("volumeModelName", "UNKNOWN!");
            volumeHardwareVer = d.getValueAsFloat("volumeHardwareVersion", -9999f);

            slices = d.getValueAsInt("sliceCount", slices);
            Shader.SetGlobalInt("_sliceCount", slices); //let any shaders that need slice count, know what it is currently.

            xArticulation = d.getValueAsInt("xArticulation", 1);
            yArticulation = d.getValueAsInt("yArticulation", 1);

            calibrationData = dotCalibrator.getCalibrationDataFromBinaryString(d.getValue("calibrationData", ""), xArticulation, yArticulation, slices);

            _flipX = d.getValueAsBool("flipX", _flipX);
            _flipY = d.getValueAsBool("flipY", _flipY);
            _flipZ = d.getValueAsBool("flipZ", _flipZ);

            updateMesh();       
  
            //setup input to take into account touchscreen hardware config
            input.init(d);

            //setup aspect ratios, for constraining cube scales
			setProjectionAspectRatios (
				d.getValueAsFloat ("projectionCentimeterWidth", 10f),
				d.getValueAsFloat ("projectionCentimeterHeight", 5f),
				d.getValueAsFloat ("projectionCentimeterDepth", 7f));


            //TODO these can come from the hardware
            Shader.SetGlobalFloat("_hardwareContrastMod", 1f);
            Shader.SetGlobalFloat("_sliceBrightnessR", 1f);
            Shader.SetGlobalFloat("_sliceBrightnessG", 1f);
            Shader.SetGlobalFloat("_sliceBrightnessB", 1f);

            return foundConfigFile;
        }

		//requires the physical dimensions of the projection, in Centimeters. Should not be public except for use by calibration tools or similar. 
		#if HYPERCUBE_DEV
		public 
		#endif
		void setProjectionAspectRatios(float xCm, float yCm, float zCm) 
		{
			aspectX = new Vector3(1f, yCm/xCm, zCm/xCm);
			aspectY = new Vector3(xCm/yCm, 1f, zCm / yCm);
			aspectZ = new Vector3(xCm/zCm, yCm / zCm, 1f);
		}



        void OnValidate()
        {

            if (slices < 1)
                slices = 1;

            if (!sliceMesh)
                return;

            if (preview)
            {
                preview.sliceCount = slices;
                preview.sliceDistance = 1f / (float)slices;
                preview.updateMesh();
            }

            updateMesh();
            resetTransform();
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                resetTransform();
            }
        }



        public void toggleFlipX()
        {
            _flipX = !_flipX;
            updateMesh();
        }
        public void toggleFlipY()
        {
            _flipY = !_flipY;
            updateMesh();
        }
        public void toggleFlipZ()
        {
            _flipZ = !_flipZ;
            updateMesh();
        }


        public float getScreenAspectRatio()
        {
            if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
                return customWidth / customHeight;
            else
                return (float)Screen.width / (float)Screen.height;
        }

        void resetTransform() //size the mesh appropriately to the screen
        {
            if (!sliceMesh)
                return;

            if (Screen.width < 1 || Screen.height < 1)
                return; //wtf.


            float xPixel = 1f / (float)Screen.width;
            float yPixel = 1f / (float)Screen.height;

                   float outWidth = (float)Screen.width;  //used in horizontal slicer
            if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
            {
                xPixel = 1f / customWidth;
                yPixel = 1f / customHeight;
                          outWidth = customWidth; //used in horizontal slicer
            }

            float aspectRatio = getScreenAspectRatio();
            sliceMesh.transform.localPosition = new Vector3(-(xPixel * aspectRatio * outWidth), 1f, zPos); //this puts the pivot of the mesh at the upper left 
            sliceMesh.transform.localScale = new Vector3( aspectRatio * 2f, 2f, 1f); //the camera size is 1f, therefore the view is 2f big.  Here we scale the mesh to match the camera's view 1:1

        }

        //this is part of the code that tries to map the player to a particular screen (this appears to be very flaky in Unity)
        public void setToDisplay(int displayNum)
        {
            if (displayNum == 0 || displayNum >= Display.displays.Length)
                return;

            GetComponent<Camera>().targetDisplay = displayNum;
            Display.displays[displayNum].Activate();
        }



        public void setTone(float value)
        {
            if (!sliceMesh)
                return;

            MeshRenderer r = sliceMesh.GetComponent<MeshRenderer>();
            if (!r)
                return;
            foreach (Material m in r.sharedMaterials)
            {
                m.SetFloat("_Mod", value);
            }
        }


        public void updateMesh()
        {
            if (!sliceMesh)
                return;

            if (slices < 1)
                slices = 1;


            if (canvasMaterials.Count == 0)
            {
                Debug.LogError("Canvas materials have not been set!  Please define what materials you want to apply to each slice in the hypercubeCanvas component.");
                return;
            }

            if (slices < 1)
            {
                slices = 1;
                return;
            }

            if (slices > canvasMaterials.Count)
            {
                Debug.LogWarning("Can't add more than " + canvasMaterials.Count + " slices, because only " + canvasMaterials.Count + " canvas materials are defined.");
                slices = canvasMaterials.Count;
                return;
            }

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int[]> submeshes = new List<int[]>(); //the triangle list(s)
            Material[] faceMaterials = new Material[slices];

            bool outFlipX = _flipX; //true values
            bool outFlipY = _flipY;
            bool outFlipZ = _flipZ;
            //modifiers
            if (flipX)
                outFlipX = !outFlipX;
            if (flipY)
                outFlipY = !outFlipY;
            if (flipZ)
                outFlipZ = !outFlipZ;


            //create the mesh
            int vertCount = 0;

            for (int s = 0; s < slices; s++)
            {

                Vector2 UV_ul = new Vector2(0f, 0f);
                Vector2 UV_br = new Vector2(1f, 1f);

                if (outFlipX && outFlipY)
                {
                    UV_ul.Set(1f, 0f);
                    UV_br.Set(0f, 1f);
                }
                else if (!outFlipX && !outFlipY)
                {
                    UV_ul.Set(0f, 1f);
                    UV_br.Set(1f, 0f);
                }
                else if (outFlipX && !outFlipY)
                {
                    UV_ul.Set(1f, 1f);
                    UV_br.Set(0f, 0f);
                }

                //if we are drawing occluded mode, modify the UV's so that they make sense.
                if (_drawOccludedMode)
                {
                    float sliceMod = 1f / (float)slices;
                    UV_ul.y *= sliceMod;
                    UV_br.y *= sliceMod;

                    UV_ul.y += (sliceMod * s);
                    UV_br.y += (sliceMod * s);
                }


                //we generate each slice mesh out of 4 interpolated parts.
                List<int> tris = new List<int>();

                vertCount += generateSliceShard(UV_ul, UV_br, vertCount, ref verts, ref tris, ref uvs); 

                submeshes.Add(tris.ToArray());

                //every face has a separate material/texture  
                if (_drawOccludedMode)
                    faceMaterials[s] = occlusionMaterial; //here it just uses 1 material, but the slices have different uv's if we are in occlusion mode
                else if (!outFlipZ)
                    faceMaterials[s] = canvasMaterials[s];
                else
                    faceMaterials[s] = canvasMaterials[slices - s - 1];
            }


            MeshRenderer r = sliceMesh.GetComponent<MeshRenderer>();
            if (!r)
                r = sliceMesh.AddComponent<MeshRenderer>();

            MeshFilter mf = sliceMesh.GetComponent<MeshFilter>();
            if (!mf)
                mf = sliceMesh.AddComponent<MeshFilter>();

            Mesh m = mf.sharedMesh;
            if (!m)
                return; //probably some in-editor state where things aren't init.
            m.Clear();

            m.SetVertices(verts);
            m.SetUVs(0, uvs);

            m.subMeshCount = slices;
            for (int s = 0; s < slices; s++)
            {
                m.SetTriangles(submeshes[s], s);
            }

            //normals are necessary for the transparency shader to work (since it uses it to calculate camera facing)
            Vector3[] normals = new Vector3[verts.Count];
            for (int n = 0; n < verts.Count; n++)
                normals[n] = Vector3.forward;

            m.normals = normals;

#if HYPERCUBE_DEV

            if (calibrator && calibrator.gameObject.activeSelf && calibrator.enabled)
                r.materials = calibrator.getMaterials();
            else
#endif
                r.materials = faceMaterials; //normal path

            m.RecalculateBounds();
        }

        //this is used to generate each of 4 sections of every slice.
        //therefore 1 central column and 1 central row of verts are overlapping per slice, but that is OK.  Keeping the interpolation isolated to this function helps readability a lot
        //returns amount of verts created
        int generateSliceShard(Vector2 topLeftUV, Vector2 bottomRightUV, int startingVert, ref  List<Vector3> verts, ref List<int> triangles, ref List<Vector2> uvs)
        {
            int vertCount = 0;
            for (var i = 0; i <= tesselation; i++)
            {
                //for every "i", or row, we are going to make a start and end point.
                //lerp between the top left and bottom left, then lerp between the top right and bottom right, and save the vectors

                float rowLerpValue = (float)i / (float)tesselation;

                Vector2 newLeftEndpoint = Vector2.Lerp(topLeft, bottomLeft, rowLerpValue);
                Vector2 newRightEndpoint = Vector2.Lerp(topRight, bottomRight, rowLerpValue);

                for (var j = 0; j <= tesselation; j++)
                {
                    //Now that we have our start and end coordinates for the row, iteratively lerp between them to get the "columns"
                    float columnLerpValue = (float)j / (float)tesselation;

                    //now get the final lerped vector
                    Vector2 lerpedVector = Vector2.Lerp(newLeftEndpoint, newRightEndpoint, columnLerpValue);


                  

                    //add it
                    verts.Add(new Vector3(lerpedVector.x, lerpedVector.y, 0f));
                    vertCount++;
                }
            }

            //triangles
            //we only want < tesselation because the very last verts in both directions don't need triangles drawn for them.
            int currentTriangle = 0;
            for (var i = 0; i < tesselation; i++)
            {
                for (int j = 0; j < tesselation; j++)
                {
                    currentTriangle = startingVert + j;
                    triangles.Add(currentTriangle + i * (tesselation + 1)); //width in verts
                    triangles.Add((currentTriangle + 1) + i * (tesselation + 1));
                    triangles.Add(currentTriangle + (i + 1) * (tesselation + 1));

                    triangles.Add((currentTriangle + 1) + i * (tesselation + 1));
                    triangles.Add((currentTriangle + 1) + (i + 1) * (tesselation + 1));
                    triangles.Add(currentTriangle + (i + 1) * (tesselation + 1));
                }
            }

            //uvs
            for (var i = 0; i <= tesselation; i++)
            {
                for (var j = 0; j <= tesselation; j++)
                {
                    Vector2 targetUV = new Vector2((float)j / (float)tesselation, (float)i / (float)tesselation);  //0-1 UV target

                    //add lerped uv
                    uvs.Add(new Vector2(
                        Mathf.Lerp(topLeftUV.x, bottomRightUV.x, targetUV.x),
                        Mathf.Lerp(topLeftUV.y, bottomRightUV.y, targetUV.y)
                        ));
                }
            }

            return vertCount;
        }


    }

}