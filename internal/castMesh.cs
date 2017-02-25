/*  
Hypercube: Volume Plugin is released under the MIT License:

Copyright 2016 Looking Glass Factory, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do 
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//This script manages the final mesh that is displayed on Volume (the castMesh)
//the surface of the castMesh translates the rendered slices into a form that compensates for distortions in the display

namespace hypercube
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(dataFileDict))]
    public class castMesh : MonoBehaviour
    {
#if HYPERCUBE_DEV
        public Shader sullyColorShader;
        public RenderTexture testingTexture;
#endif

        public readonly string usbConfigPath = "volumeCalibrationData";
        public readonly string basicSettingsFileName = "settings_basic.txt";
        public readonly string perfectSlicesFileName = "settings_perfectSlices.txt";
        public readonly string calibratedSlicesFileName = "settings_calibratedSlices.txt";

        public readonly static int defaultSliceCount = 10; //used to generate previews when volume is not connected.

        public string volumeModelName { get; private set; }
        public float volumeHardwareVer { get; private set; }
        public static castMesh canvas { get; private set; } //access the existing canvas from anywhere

        public static int rttResX { get; private set; } //these are the render texture resolutions per slice.
        public static int rttResY { get; private set; }

        //stored aspect ratio multipliers, each with the corresponding axis set to 1
        public Vector3 aspectX { get; private set; }
        public Vector3 aspectY { get; private set; }
        public Vector3 aspectZ { get; private set; }

        public bool hasUSBBasic { get; private set; }
        public bool hasCalibration { get; private set; }
        private string pcbSettings = "";
        public void setPCBbasicSettings(string _pcbSettings)
        {
            if (pcbSettings != "")
                Debug.LogWarning("PCB basic settings seem to have been asked for more than once!");
            pcbSettings = _pcbSettings;
 #if HYPERCUBE_DEV
            if (calibratorBasic) calibratorBasic.pcbText.color = Color.yellow;
#endif
            //TODO convert and use the settings if appropriate
            if (hasUSBBasic)
                input.init(GetComponent<dataFileDict>()); //we now have our touchpanel, it still needs to get its init.
        }


        public int getSliceCount() { if (calibrationData == null) return 1; return calibrationData.GetLength(0); } //a safe accessor, since its accessed constantly.

        Vector2[,,] calibrationData = null;

        public bool flipX = false;  //modifier values, by the user.
        public bool flipY = false;
        public bool flipZ = false;


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

        public GameObject sliceMesh;

        [Tooltip("The materials set here will be applied to the dynamic mesh")]
        public List<Material> canvasMaterials = new List<Material>();
        public Material occlusionMaterial;
        public Shader casterShader;

        [HideInInspector]
        public bool usingCustomDimensions = false; //this is an override so that the canvas can be told to obey the dimensions of some particular output w/h screen other than the game window

        float customWidth;
        float customHeight;

        

        public hypercubePreview preview = null;

#if HYPERCUBE_DEV
        public calibrator currentCalibrator = null;

        public vertexCalibrator calibratorV;
        public basicSettingsAdjustor calibratorBasic;
#endif

        public bool _setCalibration(Vector2[,,] data)
        {
            if (data == null)
            {
                Debug.LogWarning("Bad calibration sent to castMesh!");
                return false;
            }

            calibrationData = data;

            Shader.SetGlobalFloat("_sliceCount", (float)getSliceCount()); //let any shaders that need slice count, know what it is currently.

            updateMesh();

            hasCalibration = true;

            return true;
        }

        private void Awake()
        {
            //sane defaults, in case we never connect
            rttResX = 1024; //default rtt res
            rttResY = 512; 
            setProjectionAspectRatios(10f, 5f, 7f);
            int defaultSliceCount = 10;
            calibrationData = new Vector2[defaultSliceCount,2,2]; //default configuration when nothing is connected. a 10 slice system.
            for (int s = 0; s < defaultSliceCount; s++)
            {
                for (int y = 0; y < calibrationData.GetLength(1); y++)
                {
                    for (int x = 0; x < calibrationData.GetLength(2); x++)
                    {
                        //y offset
                        float yMod = 1f / (float)defaultSliceCount;
                        calibrationData[s, x, y] = new Vector2((float)x, ((float)y * yMod) + (s * yMod) ); //x and y can be used raw here, because there are only 2 points: 0 and 1
                    }
                }
            }

            hasUSBBasic = false;
            hasCalibration = false;

            canvas = this; //note that castMesh is in effect a singleton, since the prefab also carries the 'input' class which is a singleton.
        }

        void Start()
        {
#if !UNITY_EDITOR
            Debug.Log("Loading Hypercube Tools v" + hypercubeCamera.version + " on  Unity v" + Application.unityVersion);
#endif


            if (!loadSettingsFromUSB())
            {

#if !HYPERCUBE_INPUT
                
#if UNITY_EDITOR
            Debug.LogWarning("HYPERCUBE: Can't load settings. Please run: Hypercube > Load Volume Friendly Unity Prefs to allow Hypercube to read settings off the Volume USB.");
#else
            //TODO show only an interactive preview?
            Debug.LogWarning("No calibration found on USB drive, (and PCB read is not allowed due to preprocessor settings!)");
#endif
#endif
                //poll, and try to use the settings on the PCB once they come in.
                Shader.SetGlobalFloat("_sliceCount", defaultSliceCount); //temporarily set this for shaders, just in case for the meantime.
            
                usePreviewCam(true);//let the user just see the preview
            }
            else 
                usePreviewCam(false); //make sure the normal slice display is shown.

        }

        //the preview cam, if it exists can be used to display to a user if we have no calibration / no volume detected at all
        //then a coherent scene at least is shown on the monitor
        void usePreviewCam(bool onOff)
        {
            if (!preview)
                preview = hypercubePreview.preview;
            if (!preview)
                preview = GameObject.FindObjectOfType<hypercubePreview>();
            
            if (!preview)
            {
                GetComponent<Camera>().enabled = true;
                return;
            }

            if (onOff) //use preview cam
            {
                GetComponent<Camera>().enabled = false;
                preview.previewCamera.SetActive(true);
            }
            else //normal behavior
            {
                GetComponent<Camera>().enabled = true;
                preview.previewCamera.SetActive(false);
            }
        }

        public void setCustomWidthHeight(float w, float h)
        {
            if (w == 0 || h == 0) //bogus values. Possible, if the window is still setting up.
                return;

            usingCustomDimensions = true;
            customWidth = w;
            customHeight = h;
        }



        public bool loadSettingsFromUSB()
        {
            hasCalibration = false;

            dataFileDict d = GetComponent<dataFileDict>();

            d.clear();

            bool foundCalibrationFile = false;
           
            if (hypercube.utils.getConfigPathToFile(usbConfigPath + "/" + basicSettingsFileName, out d.fileName) && d.load()) // (ie   G:/volumeConfigurationData/prefs.txt)
            {
                hasUSBBasic = true;
#if HYPERCUBE_DEV
                if (calibratorBasic) calibratorBasic.usbText.color = Color.yellow;
#endif

                string calibrationFile = "";
                if (d.getValueAsBool("FPGA", false))
                {
                    if (hypercube.utils.getConfigPathToFile(usbConfigPath + "/" + perfectSlicesFileName, out calibrationFile))
                        foundCalibrationFile = true;
                    else if (utils.getConfigPathToFile(perfectSlicesFileName, out calibrationFile))
                        foundCalibrationFile = true;
                }
                else
                {
                    calibrationFile = calibratedSlicesFileName;
                    if (utils.getConfigPathToFile(usbConfigPath + "/" + calibratedSlicesFileName, out calibrationFile))
                        foundCalibrationFile = true;
                    else if (utils.getConfigPathToFile(calibratedSlicesFileName, out calibrationFile))
                        foundCalibrationFile = true;
                }

                    
                if (foundCalibrationFile)
                {
                    applyLoadedSettings(d);

                    // apply the usb calibration
                    Vector2[,,] v;
                    byte[] fileContents = System.IO.File.ReadAllBytes(calibrationFile);
                    if (utils.bin2Vert(fileContents, out v) && _setCalibration(v))
                    {
#if HYPERCUBE_DEV
                        if (calibratorBasic) calibratorBasic.reloadDataFile(); //we may have received a delayed update from the pcb, make sure any gui in the calibration is updated.
                        if (calibratorV) calibratorV.setLoadedVertices(v, true);
#endif
                    }
                    else
                        Debug.LogWarning("Failed to apply calibration found on the USB: " + calibrationFile);
                        
                }
            }
            else 
            {
                //we failed to load the file!  ...the calling method will try the PCB now
            }
                
            return hasCalibration;
        }

#if HYPERCUBE_DEV
        public
#endif
        void applyLoadedSettings(dataFileDict d)
        {             

            //sanity check.  If the dataFileDict doesn't even have these, we are looking at trash data.
            //ignore it and keep using preview cam if possible.
            if (!d.hasKey("volumeModelName") &&
                !d.hasKey("volumeHardwareVersion") &&
                !d.hasKey("volumeResX")
            )
                return;


            usePreviewCam(false); 

            volumeModelName = d.getValue("volumeModelName", "UNKNOWN!");
            volumeHardwareVer = d.getValueAsFloat("volumeHardwareVersion", -9999f);

            rttResX = d.getValueAsInt("sliceResX", rttResX);
            rttResY = d.getValueAsInt("sliceResY", rttResY);

#if !UNITY_EDITOR
            //set the res, if it is different.
            int resXpref = d.getValueAsInt("volumeResX", 1920);
            int resYpref = d.getValueAsInt("volumeResY", 1080);

            if (Screen.width != resXpref || Screen.height != resYpref)
                Screen.SetResolution(resXpref, resYpref, true);
#endif

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

        }


		//requires the physical dimensions of the projection, in Centimeters. Should not be public except for use by calibration tools or similar. 
#if HYPERCUBE_DEV
		public 
#endif
		void setProjectionAspectRatios(float xCm, float yCm, float zCm) 
		{
            if (xCm == 0f) //sanity check
            {
                xCm = 1f;
                Debug.LogWarning("Bad aspect ratio was given! Fixing...");
            }

            if (yCm == 0f)
            {
                yCm = 1f;
                Debug.LogWarning("Bad aspect ratio was given! Fixing...");
            }

            if (zCm == 0f)
            {
                zCm = 1f;
                Debug.LogWarning("Bad aspect ratio was given! Fixing...");
            }
                

			aspectX = new Vector3(1f, yCm/xCm, zCm/xCm);
			aspectY = new Vector3(xCm/yCm, 1f, zCm / yCm);
			aspectZ = new Vector3(xCm/zCm, yCm / zCm, 1f);
		}



        void OnValidate()
        {
            if (!sliceMesh)
                return;

            updateMesh();
            resetTransform();
        }

        float usbSettingsTimer = 5f; //used if we have no calibration on start.
        void Update()
        {
            if (!hasCalibration)
            {
                //keep trying to find usb settings...
                usbSettingsTimer -= Time.deltaTime;
                if (usbSettingsTimer < 0f)
                {
                    usbSettingsTimer = 1f;
                    if (loadSettingsFromUSB())
                        return;
                }

                //we don't have calibration from usb, if we have pcbSettings try using them
                else if (pcbSettings != "" && input.touchPanel != null) 
                {
                    //we appear to have found some settings from the pcb. Try once more to prefer the USB
                    if (loadSettingsFromUSB())
                        return; //we found USB, get out of this.
                     
                    dataFileDict d = GetComponent<dataFileDict>();
                    if (!d.loadFromString(pcbSettings))
                        Debug.LogWarning("USB settings not found, and PCB basic settings appear to be invalid.");

                    applyLoadedSettings(d);
                    pcbSettings = ""; //we applied them, let it be blank to keep this from iterating.
                }              
            }

            if (transform.hasChanged)
            {
                resetTransform();
            }

            //make sure we are using proper textures at all times
            if (hypercubeCamera.mainCam == null)
                return;

            if (canvasMaterials.Count > 0 && canvasMaterials[0].mainTexture == null || 
                occlusionMaterial.mainTexture != hypercubeCamera.mainCam.occlusionRTT)
            {
                for (int i = 0; i < canvasMaterials.Count && i < hypercubeCamera.mainCam.sliceTextures.Length; i++)
                {
                    canvasMaterials[i].mainTexture = hypercubeCamera.mainCam.sliceTextures[i]; 
                }
                occlusionMaterial.mainTexture = hypercubeCamera.mainCam.occlusionRTT;
                updateMesh();
            }

        }

        public float getScreenAspectRatio()
        {
            float w = 0f;
            float h = 0f;
            getScreenDims(ref w, ref h);
            return w / h;
        }
        public void getScreenDims(ref float w, ref float h)
        {
            if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
            {
                w = customWidth;
                h = customHeight;
                return;            
            }
            w = (float)Screen.width;
            h = (float)Screen.height;
        }

        void resetTransform() //size the mesh appropriately to the screen
        {
            if (!sliceMesh)
                return;

            if (Screen.width < 1 || Screen.height < 1)
                return; //wtf.


            float xPixel = 1f / (float)Screen.width;
           // float yPixel = 1f / (float)Screen.height;

            float outWidth = (float)Screen.width;  //used in horizontal slicer
            if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
            {
                xPixel = 1f / customWidth;
                //yPixel = 1f / customHeight;
                outWidth = customWidth; //used in horizontal slicer
            }

            float aspectRatio = getScreenAspectRatio();
            sliceMesh.transform.localPosition = new Vector3(-(xPixel * aspectRatio * outWidth), -1f, zPos); //this puts the pivot of the mesh at the upper left 
            sliceMesh.transform.localScale = new Vector3( aspectRatio * 2f, 2f, 1f); //the camera size is 1f, therefore the view is 2f big.  Here we scale the mesh to match the camera's view 1:1

        }

        //this is part of the code that tries to map the player to a particular screen (this appears to be very flaky in Unity)
   /*     public void setToDisplay(int displayNum)
        {
            if (displayNum == 0 || displayNum >= Display.displays.Length)
                return;

            GetComponent<Camera>().targetDisplay = displayNum;
            Display.displays[displayNum].Activate();
        }
*/


        public void setTone(float value)
        {
            if (!sliceMesh)
                return;

            MeshRenderer r = sliceMesh.GetComponent<MeshRenderer>();
            if (!r)
                return;
            foreach (Material m in r.sharedMaterials)
            {
                if (m)
                    m.SetFloat("_Mod", value);
            }
        }


        public void updateMesh()
        {
            if (!sliceMesh || calibrationData == null)
                return;

            if (getSliceCount() != canvasMaterials.Count)
            {
                //fill the material array in such a way as to respect any elements that have been overriden by the dev.
                while (canvasMaterials.Count > getSliceCount())
                    canvasMaterials.RemoveAt(canvasMaterials.Count - 1);//remove extras.
                
                for (int i = 0; i < getSliceCount(); i++)
                {
                    if (i >= canvasMaterials.Count)
                        canvasMaterials.Add(new Material(casterShader));
                    else if (canvasMaterials[i] == null)
                        canvasMaterials[i] = new Material(casterShader);
                    
                    //the textures are taken care of below.

                }
                System.GC.Collect();
            }

            //make sure the proper dynamic textures are in place
            if (hypercubeCamera.mainCam)
            {
                for (int i = 0; i < getSliceCount() && i < hypercubeCamera.mainCam.sliceTextures.Length; i++)
                {
                    canvasMaterials[i].mainTexture = hypercubeCamera.mainCam.sliceTextures[i];
                }
                occlusionMaterial.mainTexture = hypercubeCamera.mainCam.occlusionRTT;
            }



            int slices = getSliceCount();

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> colors = new List<Color>();
            List<int[]> submeshes = new List<int[]>(); //the triangle list(s)
            Material[] faceMaterials = new Material[slices];

            //create the mesh
            int vertCount = 0;

            for (int s = 0; s < slices; s++)
            {

                //we generate each slice mesh out of 4 interpolated parts.
                List<int> tris = new List<int>();

                vertCount += generateSlice(vertCount, s, ref verts, ref tris, ref uvs, ref colors); 

                submeshes.Add(tris.ToArray());

                //every face has a separate material/texture  
                if (_drawOccludedMode)
                    faceMaterials[s] = occlusionMaterial; //here it just uses 1 material, but the slices have different uv's if we are in occlusion mode
                else if (!flipZ)
                    faceMaterials[s] = canvasMaterials[s]; //normal
                else
                    faceMaterials[s] = canvasMaterials[slices - s - 1]; //reverse z
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

            if (currentCalibrator && currentCalibrator.gameObject.activeSelf && currentCalibrator.enabled)
                r.materials = currentCalibrator.getMaterials();
            else
#endif
                r.materials = faceMaterials; //normal path

            m.RecalculateBounds();
        }
            
        //returns amount of verts created
        int generateSlice(int startingVert, int slice, ref  List<Vector3> verts, ref List<int> triangles, ref List<Vector2> uvs, ref List<Color> colors)
        {
            int vertCount = 0;
            int xTesselation = calibrationData.GetLength(1);
            int yTesselation = calibrationData.GetLength(2);
            for (var y = 0; y < yTesselation; y++)
            {
                for (var x = 0; x < xTesselation; x++)
                {
                    //add it
                    verts.Add(new Vector3(calibrationData[slice, x, y].x, calibrationData[slice, x, y].y, 0f)); //note these values are 0-1
                    vertCount++;
                }
            }

            //triangles
            //we only want < tesselation because the very last verts in both directions don't need triangles drawn for them.
            int currentTriangle = 0;
          //  int xspot = xTesselation - 1;
         //   int yspot = yTesselation - 1;
            for (var y = 0; y < yTesselation -1; y++)
            {
                for (int x = 0; x < xTesselation -1; x++)
                {
                    currentTriangle = startingVert + x;
                    triangles.Add(currentTriangle + ((y + 1) * xTesselation)); //bottom left
                    triangles.Add((currentTriangle + 1) + (y * xTesselation));  //top right
                    triangles.Add(currentTriangle + (y * xTesselation)); //top left

                    triangles.Add(currentTriangle + ((y + 1) * xTesselation)); //bottom left
                    triangles.Add((currentTriangle + 1) + ((y + 1) * xTesselation)); //bottom right
                    triangles.Add((currentTriangle + 1) + (y * xTesselation)); //top right
                }
            }

            //uvs
            if (_drawOccludedMode)
            {
                float sliceMod = 1f / (float)getSliceCount();

                float UVW = 1f / (float)(xTesselation -1); //-1 makes sure the UV gets to the end
                float UVH = sliceMod * 1f / (float)(yTesselation -1 );
                float heightOffset = sliceMod * slice;
                for (var y = 0; y < yTesselation; y++)
                {
                    for (var x = 0; x < xTesselation; x++)
                    {
                        Vector2 targetUV = new Vector2(x * UVW, heightOffset + (y * UVH));  //0-1 UV target

                        uvs.Add(targetUV);

                        colors.Add(new Color(targetUV.x, targetUV.y, slice * .001f, 1f)); //note the current slice is stored in the blue channel
                    }
                }
            }
            else //normal UV path 0 -1
            {
                float UVW = 1f / (float)(xTesselation -1); //-1 makes sure the UV gets to the end
                float UVH = 1f / (float)(yTesselation -1 );
                for (var y = 0; y < yTesselation; y++)
                {
                    for (var x = 0; x < xTesselation; x++)
                    {
                        Vector2 targetUV = new Vector2(x * UVW, y * UVH);  //0-1 UV target

                        uvs.Add(targetUV);

                        colors.Add(new Color(targetUV.x, targetUV.y, slice * .001f, 1f)); //note the current slice is stored in the blue channel
                    }
                }
            }


            return vertCount;
        }

#if HYPERCUBE_DEV
        public bool generateSullyTextures(int w, int h, string filePath)
        {
            if (!sullyColorShader)
            {
                Debug.LogWarning("No sully shader defined!");
                return false;
            }

            Camera c = GetComponent<Camera>();
            RenderTexture rtt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBFloat);
            c.targetTexture = rtt;
            //c.targetTexture = testingTexture;
            c.RenderWithShader(sullyColorShader, "");


            Texture2D rTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            RenderTexture.active = rtt;
            rTex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0, false);
            rTex.Apply();

            int slices = getSliceCount();
            Color clr = new Color();
            Color32[] xColors = new Color32[w * h];
            Color32[] yColors = new Color32[w * h];

            try
            {

                int n = 0;
                for (int y = h - 1; y >= 0; y--)
                {
                    for (int x = 0; x < w; x++)
                    {
                        clr = rTex.GetPixel(x, y);
                        if (!floatToColor(clr.r * w, out xColors[n]))
                        {
                            c.targetTexture = null;
                            return false;
                        }
                        if (!floatToColor(
                           ((clr.g / slices) * h) +  //the position within the current slice
                           (((clr.b * 1000) / (float)slices) * (float)h), out yColors[n]))  //plus the slice's height position within the entire image. The 10 is because the slice number is encoded as .1 per slice
                        {
                            c.targetTexture = null;
                            return false;
                        }
                        n++;
                    }
                }
            }
            catch 
            {
                c.targetTexture = null;
                Debug.LogError("Something was wrong with the sully texture.  Check that the proper shader was set in the castMesh sullyColorShader.");
                return false;
            }

            Texture2D xTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            xTex.SetPixels32(xColors);
            xTex.Apply();
            Texture2D yTex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            yTex.SetPixels32(yColors);
            yTex.Apply();

            c.targetTexture = null;

            System.IO.File.WriteAllBytes(filePath + "/xSullyTex.png", xTex.EncodeToPNG());
            System.IO.File.WriteAllBytes(filePath + "/ySullyTex.png", yTex.EncodeToPNG());

            // testMaterial.SetTexture("_MainTex", xTex);  //test
            // testMaterial2.SetTexture("_MainTex", yTex);
            return true;
        }

        //this method maps a float into a rgb 24 bit color space with max value of 4096
        static bool floatToColor(float v, out Color32 c)
        {
            if (v < 0 || v > 4096)
            {
                Debug.LogWarning("Invalid value of " + v + " passed into the sully calibration texture generation! Must be 0-4096");
                c = new Color32();
                return false;
            }

            byte r = (byte)((int)v >> 4); //this assumes the maximum value will never pass 0-4096 (12 bits)
            byte g = (byte)(((int)v & 0x0F) << 4); //the least significant part of the integer part of the float stored at the start of g
            double rawFrac = v - (int)v;
            int fracV = (int)(4096 * rawFrac);  //just the fractional part, scaled up to 12 bits
            g |= (byte)(fracV >> 8); //the most significant part of fractional part of the float stored in the back of g
            byte b = (byte)(fracV & 0xFF);
            c = new Color32(r, g, b, byte.MaxValue);
            return true;

        }
#endif
    }

}