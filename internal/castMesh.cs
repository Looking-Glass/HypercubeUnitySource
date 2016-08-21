using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//This script manages the canvas surface
//the canvas surface translates the rendered slices into a form that the Volume can display properly.

namespace hypercube
{


    [ExecuteInEditMode]
    [RequireComponent(typeof(hypercube.calibrator))]
    [RequireComponent(typeof(dataFileDict))]
    public class castMesh : MonoBehaviour
    {
        public int slices = 12;
        public int getSliceCount() { return slices; } //a safe accessor, since its accessed constantly.

        public bool flipX = false;
        public bool flipY = false;
        public bool flipZ = false;

        public float sliceOffsetX;
        public float sliceOffsetY;    
        public float sliceWidth;
        public float sliceHeight;
        public float sliceGap;
        public float zPos = .01f;
        [Range(1, 20)]
        public int tesselation = 8;
        public GameObject sliceMesh;

        [Tooltip("The materials set here will be applied to the dynamic mesh")]
        public List<Material> canvasMaterials = new List<Material>();

        [HideInInspector]
        public bool usingCustomDimensions = false; //this is an override so that the canvas can be told to obey the dimensions of some particular output w/h screen other than the game window

        float customWidth;
        float customHeight;

        //individual calibration offsets
        Vector2[] ULOffsets = null;
        Vector2[] UROffsets = null;
        Vector2[] LLOffsets = null;
        Vector2[] LROffsets = null;
        Vector2[] MOffsets = null;
        Vector2[] skews = null;
        Vector2[] bows = null;

        public hypercubePreview preview = null;
        calibrator calibrator = null;
        public Material casterMaterial;

        [Tooltip("This path is how we find the calibration and system settings in the internal drive for the Volume in use. Don't change unless you know what you are changing.")]
        public string relativeSettingsPath;  

        void Start()
        {
            calibrator = GetComponent<calibrator>();

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

        public void copyCurrentSliceCalibration(int fromSlice)
        {
            //note this should only work on other even/odds respectively since they need their own calibrations
            for (int s = 0; s < slices; s++)
            {
                ULOffsets[s] = ULOffsets[fromSlice];
                UROffsets[s] = UROffsets[fromSlice];
                LLOffsets[s] = LLOffsets[fromSlice];
                LROffsets[s] = LROffsets[fromSlice];
                MOffsets[s] = MOffsets[fromSlice];
                skews[s] = skews[fromSlice];
                bows[s] = bows[fromSlice];
            }
            updateMesh();
        }

#if HYPERCUBE_DEV
        public void saveConfigSettings()
        {
            dataFileDict d = GetComponent<dataFileDict>();
            if (!d)
                return;

            d.fileName = hypercube.utils.getConfigPath(relativeSettingsPath);    //return it to the dataFileDict as an absolute path within that drive if we find it  (ie   G:/volumeConfigurationData/prefs.txt).
       

            d.setValue("sliceCount", slices);
            d.setValue("offsetX", sliceOffsetX);
            d.setValue("offsetY", sliceOffsetY);
            d.setValue("sliceWidth", sliceWidth);
            d.setValue("pixelsPerSlice", sliceHeight);
            d.setValue("sliceGap", sliceGap);
            d.setValue("flipX", flipX);
            d.setValue("flipY", flipY);
            d.setValue("flipZ", flipZ);

            saveCalibrationOffsets(d);

            Debug.Log("Settings saved to config file: " + d.fileName);
        }
#else
        public void saveConfigSettings()
        {
            Debug.LogWarning("Overwriting calibration settings from this application will overwrite the settings for ALL Volume apps!\nUse dataFileDict to manage any custom settings yourself.");
        } 
#endif

        public void loadSettings()
        {
            
            dataFileDict d = GetComponent<dataFileDict>();

            //use this path as a base path to search for the drive provided with Volume.
            d.fileName = hypercube.utils.getConfigPath(relativeSettingsPath);    //return it to the dataFileDict as an absolute path within that drive if we find it  (ie   G:/volumeConfigurationData/prefs.txt).
            
            d.clear();

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Loaded calibration settings from file."); //these force the editor to mark the canvas as dirty and save what is loaded.
#endif

            if (!d.load()) //we failed to load the file!  ...use backup defaults.
                Debug.LogWarning("Could not read calibration data from Volume!\nIs Volume connected via USB? Using defaults..."); //This will never be as good as using the config stored with the hardware and the view will have distortions in Volume's display.

            slices = d.getValueAsInt("sliceCount", slices);
            sliceOffsetX = d.getValueAsFloat("offsetX", sliceOffsetX);
            sliceOffsetY = d.getValueAsFloat("offsetY", sliceOffsetY);
            sliceWidth = d.getValueAsFloat("sliceWidth", sliceWidth);
            sliceHeight = d.getValueAsFloat("pixelsPerSlice", sliceHeight);
            sliceGap = d.getValueAsFloat("sliceGap", sliceGap);
            flipX = d.getValueAsBool("flipX", flipX);
            flipY = d.getValueAsBool("flipY", flipY);
            flipZ = d.getValueAsBool("flipZ", flipZ);

            setCalibrationOffsets(d, slices);
            updateMesh();       
  
            //setup input to take into account touchscreen hardware config
            if (input.get())
                input.get().init(d);
        }

        //tweaks to the cube design to offset physical distortions
        public void setCalibrationOffsets(dataFileDict d, int maxSlices)
        {
            ULOffsets = new Vector2[maxSlices]; //init our calibration vars
            UROffsets = new Vector2[maxSlices];
            LLOffsets = new Vector2[maxSlices];
            LROffsets = new Vector2[maxSlices];
            MOffsets = new Vector2[maxSlices];
            skews = new Vector2[maxSlices];
            bows = new Vector2[maxSlices];

            for (int s = 0; s < maxSlices; s++)
            {
                setCalibrationOffset(s,
                d.getValueAsFloat("s" + s + "_ULx", 0f),
                d.getValueAsFloat("s" + s + "_ULy", 0f),
                d.getValueAsFloat("s" + s + "_URx", 0f),
                d.getValueAsFloat("s" + s + "_URy", 0f),
                d.getValueAsFloat("s" + s + "_LLx", 0f),
                d.getValueAsFloat("s" + s + "_LLy", 0f),
                d.getValueAsFloat("s" + s + "_LRx", 0f),
                d.getValueAsFloat("s" + s + "_LRy", 0f),
                d.getValueAsFloat("s" + s + "_Mx", 0f),
                d.getValueAsFloat("s" + s + "_My", 0f),
                d.getValueAsFloat("s" + s + "_Sx", 0f),
                d.getValueAsFloat("s" + s + "_Sy", 0f),
                d.getValueAsFloat("s" + s + "_Bx", 0f),
                d.getValueAsFloat("s" + s + "_By", 0f)
                    );
            }
        } 
        //this call is separate, so it can be flexible enough to accept different ways of storing the calibration data
        public void setCalibrationOffset(int slice, float _ULx, float _ULy, float _URx, float _URy, float _LLx, float _LLy, float _LRx, float _LRy, float _Mx, float _My, float _Sx, float _Sy, float _Bx, float _By)
        {
            if (slice < 0 || slice >= bows.Length)
                return;

            ULOffsets[slice].x = _ULx;
            ULOffsets[slice].y = _ULy;
            UROffsets[slice].x = _URx;
            UROffsets[slice].y = _URy;
            LLOffsets[slice].x = _LLx;
            LLOffsets[slice].y = _LLy;
            LROffsets[slice].x = _LRx;
            LROffsets[slice].y = _LRy;
            MOffsets[slice].x = _Mx;
            MOffsets[slice].y = _My;
            skews[slice].x = _Sx;
            skews[slice].y = _Sy;
            bows[slice].x = _Bx;
            bows[slice].y = _By;
        }
        public void saveCalibrationOffsets(dataFileDict d)
        {
            for (int s = 0; s < ULOffsets.Length; s++)
            {
                d.setValue("s" + s + "_ULx", ULOffsets[s].x);
                d.setValue("s" + s + "_ULy", ULOffsets[s].y);
                d.setValue("s" + s + "_URx", UROffsets[s].x);
                d.setValue("s" + s + "_URy", UROffsets[s].y);
                d.setValue("s" + s + "_LLx", LLOffsets[s].x);
                d.setValue("s" + s + "_LLy", LLOffsets[s].y);
                d.setValue("s" + s + "_LRx", LROffsets[s].x);
                d.setValue("s" + s + "_LRy", LROffsets[s].y);
                d.setValue("s" + s + "_Mx", MOffsets[s].x);
                d.setValue("s" + s + "_My", MOffsets[s].y);
                d.setValue("s" + s + "_Sx", skews[s].x);
                d.setValue("s" + s + "_Sy", skews[s].y);
                d.setValue("s" + s + "_Bx", bows[s].x);
                d.setValue("s" + s + "_By", bows[s].y);
            }

            d.save();
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


        public void makeBowAdjustment(int slice, bool x, float amount)
        {

            if (x)
                bows[slice].x += amount;
            else
                bows[slice].y += amount;
        
            updateMesh();
        }
        public void makeSkewAdjustment(int slice, bool x, float amount)
        {

            if (x)
                skews[slice].x += amount;
            else
                skews[slice].y += amount;

            updateMesh();
        }

#if HYPERCUBE_DEV
        public bool makeAdjustment(int slice, canvasEditMode m, bool x, float amount)
        {
            if (slice < 0)
                return false;
            if (slice >= ULOffsets.Length)
                return false;

            //flip it to keep things intuitive
            if (flipX)
            {
                if (x)
                    amount = -amount;
                if (m == canvasEditMode.UL)
                    m = canvasEditMode.UR;
                else if (m == canvasEditMode.UR)
                    m = canvasEditMode.UL;
                else if (m == canvasEditMode.LL)
                    m = canvasEditMode.LR;
                else if (m == canvasEditMode.LR)
                    m = canvasEditMode.LL;
            }
            if (flipY)
            {
                if (!x)
                    amount = -amount;
                if (m == canvasEditMode.UL)
                    m = canvasEditMode.LL;
                else if (m == canvasEditMode.UR)
                    m = canvasEditMode.LR;
                else if (m == canvasEditMode.LL)
                    m = canvasEditMode.UL;
                else if (m == canvasEditMode.LR)
                    m = canvasEditMode.UR;
            }


            if (m == canvasEditMode.UL)
            {
                if (x)
                    ULOffsets[slice].x += amount;
                else
                    ULOffsets[slice].y += amount;
            }
            else if (m == canvasEditMode.UR)
            {
                if (x)
                    UROffsets[slice].x += amount;
                else
                    UROffsets[slice].y += amount;
            }
            else if (m == canvasEditMode.LL)
            {
                if (x)
                    LLOffsets[slice].x += amount;
                else
                    LLOffsets[slice].y += amount;
            }
            else if (m == canvasEditMode.LR)
            {
                if (x)
                    LROffsets[slice].x += amount;
                else
                    LROffsets[slice].y += amount;
            }
            else if (m == canvasEditMode.M)
            {
                if (x)
                    MOffsets[slice].x += amount;
                else
                    MOffsets[slice].y += amount;
            }

            updateMesh();

            return true;
        }
#endif

        public void flip()
        {
            flipX = !flipX;
            updateMesh();
        }
        public void sliceHeightUp()
        {
            sliceHeight += .2f;
            updateMesh();
        }
        public void sliceHeightDown()
        {
            sliceHeight -= .2f;
            updateMesh();
        }
        public void nudgeUp()
        {
            sliceOffsetY += .2f;
            updateMesh();
        }
        public void nudgeDown()
        {
            sliceOffsetY -= .2f;
            updateMesh();
        }
        public void nudgeLeft()
        {
            sliceOffsetX -= 1f;
            updateMesh();
        }
        public void nudgeRight()
        {
            sliceOffsetX += 1f;
            updateMesh();
        }
        public void widthUp()
        {
            sliceWidth += 1f;
            updateMesh();
        }
        public void widthDown()
        {
            sliceWidth -= 1f;
            updateMesh();
        }
        public void setPreset1()
        {
            sliceHeight = 120f;
            updateMesh();
        }
        public void setPreset2()
        {
            sliceHeight = 68f;
            updateMesh();
        }

        public float getAspectRatio()
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

            //       float outWidth = (float)Screen.width;  //used in horizontal slicer
            if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
            {
                xPixel = 1f / customWidth;
                yPixel = 1f / customHeight;
                //          outWidth = customWidth; //used in horizontal slicer
            }

            float aspectRatio = getAspectRatio();
            sliceMesh.transform.localScale = new Vector3(sliceWidth * xPixel * aspectRatio, (float)slices * sliceHeight * 2f * yPixel, 1f); //the *2 is because the view is 2 units tall


            //vert slicer
            sliceMesh.transform.localPosition = new Vector3(xPixel * sliceOffsetX, (yPixel * sliceOffsetY * 2f) - 1f, zPos); //the -1f is the center vertical on the screen, the *2 is because the view is 2 units tall
            //horizontal slicer
            // sliceMesh.transform.localPosition = new Vector3((xPixel * aspectRatio * outWidth) + (xPixel * sliceOffsetY), (yPixel * sliceOffsetX * 2f), zPos); //this assumes the mesh is rotated 90 degrees to the left
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
            if (slices < 1)
                slices = 1;

            if (skews == null || skews.Length < slices) //if they don't exist yet, just use temporary values
            {
                ULOffsets = new Vector2[slices];
                UROffsets = new Vector2[slices];
                LLOffsets = new Vector2[slices];
                LROffsets = new Vector2[slices];
                MOffsets = new Vector2[slices];
                skews = new Vector2[slices];
                bows = new Vector2[slices];
                for (int s = 0; s < slices; s++)
                {
                    ULOffsets[s] = new Vector2(0f, 0f);
                    UROffsets[s] = new Vector2(0f, 0f);
                    LLOffsets[s] = new Vector2(0f, 0f);
                    LROffsets[s] = new Vector2(0f, 0f);
                    MOffsets[s] = new Vector2(0f, 0f);
                    skews[s] = new Vector2(0f, 0f);
                    bows[s] = new Vector2(0f, 0f);
                }
            }

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
            if (sliceHeight < 1)
            {
                sliceHeight = 1;
                return;
            }
            if (sliceWidth < 1)
            {
                sliceWidth = 1;
                return;
            }

            if (tesselation < 1)
            {
                tesselation = 1;
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

            //create the mesh
            float size = 1f / (float)slices;
            int vertCount = 0;
            float pixelSliceGap = (1f / sliceHeight) * sliceGap * size;
            for (int s = 0; s < slices; s++)
            {
                float yPos = ((float)s * size) + (s * pixelSliceGap);
                Vector2 topL = new Vector2(-1f + ULOffsets[s].x, yPos + size + ULOffsets[s].y); //top left          
                Vector2 topM = new Vector2(MOffsets[s].x, yPos + size + Mathf.Lerp(ULOffsets[s].y, UROffsets[s].y, Mathf.InverseLerp(-1f + ULOffsets[s].x, 1f + UROffsets[s].x, MOffsets[s].x))); //top middle

                Vector2 topR = new Vector2(1f + UROffsets[s].x, yPos + size + UROffsets[s].y); //top right

                Vector2 midL = new Vector2(-1f + Mathf.Lerp(ULOffsets[s].x, LLOffsets[s].x, Mathf.InverseLerp(size + ULOffsets[s].y, LLOffsets[s].y, (size / 2) + MOffsets[s].y)), yPos + (size / 2) + MOffsets[s].y); //middle left
                Vector2 middle = new Vector2(MOffsets[s].x, yPos + (size / 2) + MOffsets[s].y); //center
                Vector2 midR = new Vector2(1f + Mathf.Lerp(UROffsets[s].x, LROffsets[s].x, Mathf.InverseLerp(size + UROffsets[s].y, LROffsets[s].y, (size / 2) + MOffsets[s].y)), yPos + (size / 2) + MOffsets[s].y); //middle right

                Vector2 lowL = new Vector2(-1f + LLOffsets[s].x, yPos + LLOffsets[s].y); //bottom left
                Vector2 lowM = new Vector2(MOffsets[s].x, yPos + Mathf.Lerp(LLOffsets[s].y, LROffsets[s].y, Mathf.InverseLerp(-1f + LLOffsets[s].x, 1f + LROffsets[s].x, MOffsets[s].x))); //bottom middle
                Vector2 lowR = new Vector2(1f + LROffsets[s].x, yPos + LROffsets[s].y); //bottom right      

                //skews
                topM.x += skews[s].x;
                lowM.x -= skews[s].x;
                midL.y += skews[s].y;
                midR.y -= skews[s].y;

                //interpolate the alternate axis on the skew so that edges will always be straight ( fix elbows caused when we skew)
                topM.y = Mathf.Lerp(topL.y, topR.y, Mathf.InverseLerp(topL.x, topR.x, topM.x));
                lowM.y = Mathf.Lerp(lowL.y, lowR.y, Mathf.InverseLerp(lowL.x, lowR.x, lowM.x));
                midL.x = Mathf.Lerp(topL.x, lowL.x, Mathf.InverseLerp(topL.y, lowL.y, midL.y));
                midR.x = Mathf.Lerp(topR.x, lowR.x, Mathf.InverseLerp(topR.y, lowR.y, midR.y));

                Vector2 UV_ul = new Vector2(0f, 0f);
                Vector2 UV_mid = new Vector2(.5f, .5f);
                Vector2 UV_br = new Vector2(1f, 1f);
                Vector2 UV_left = new Vector2(0f, .5f);
                Vector2 UV_bottom = new Vector2(.5f, 1f);
                Vector2 UV_top = new Vector2(.5f, 0f);
                Vector2 UV_right = new Vector2(1f, .5f);

                if (flipX && !flipY)
                {
                    UV_ul.Set(1f, 0f);
                    UV_br.Set(0f, 1f);
                    UV_left.Set(1f, .5f);
                    UV_right.Set(0f, .5f);
                }
                else if (!flipX && flipY)
                {
                    UV_ul.Set(0f, 1f);
                    UV_br.Set(1f, 0f);
                    UV_bottom.Set(.5f, 0f);
                    UV_top.Set(.5f, 1f);
                }
                else if (flipX && flipY)
                {
                    UV_ul.Set(1f, 1f);
                    UV_br.Set(0f, 0f);
                    UV_bottom.Set(.5f, 0f);
                    UV_top.Set(.5f, 1f);
                    UV_left.Set(1f, .5f);
                    UV_right.Set(0f, .5f);
                }

                //we generate each slice mesh out of 4 interpolated parts.
                List<int> tris = new List<int>();

                vertCount += generateSliceShard(topL, topM, midL, middle, UV_ul, UV_mid, bows[s], 1f, 1f, vertCount, ref verts, ref tris, ref uvs); //top left shard
                vertCount += generateSliceShard(topM, topR, middle, midR, UV_top, UV_right, bows[s], 1f, 0f, vertCount, ref verts, ref tris, ref uvs); //top right shard
                vertCount += generateSliceShard(midL, middle, lowL, lowM, UV_left, UV_bottom, bows[s], 0f, 1f, vertCount, ref verts, ref tris, ref uvs); //bottom left shard
                vertCount += generateSliceShard(middle, midR, lowM, lowR, UV_mid, UV_br, bows[s], 0f, 0f, vertCount, ref verts, ref tris, ref uvs); //bottom right shard

                submeshes.Add(tris.ToArray());

                //every face has a separate material/texture   
                if (!flipZ)
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

            if (!calibrator)
                calibrator = GetComponent<calibrator>();

#if HYPERCUBE_DEV
            if (calibrator.enabled)
                r.materials = calibrator.getMaterials();
            else
#endif
                r.materials = faceMaterials; //normal path

            m.RecalculateBounds();
        }


        //this is used to generate each of 4 sections of every slice.
        //therefore 1 central column and 1 central row of verts are overlapping per slice, but that is OK.  Keeping the interpolation isolated to this function helps readability a lot
        //returns amount of verts created
        int generateSliceShard(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Vector2 topLeftUV, Vector2 bottomRightUV, Vector2 bow, float xBowPhase, float yBowPhase, int startingVert, ref  List<Vector3> verts, ref List<int> triangles, ref List<Vector2> uvs)
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

                    //add bow distortion compensation
                    lerpedVector.x += (1f - Mathf.Cos(xBowPhase - rowLerpValue)) * bow.y;
                    lerpedVector.y += (1f - Mathf.Cos(yBowPhase - columnLerpValue)) * bow.x;
                    lerpedVector.x -= bow.y * .5f; //the two lines above pivot the bowing on the centerpoint of the slice. The two following lines change the pivot to the corner points of articulation so that the center is what moves.
                    lerpedVector.y -= bow.x * .5f;

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