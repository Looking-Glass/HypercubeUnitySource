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


namespace hypercube
{

#if HYPERCUBE_DEV
    public enum distortionCompensationType
    {
        PIXEL,
        SPATIAL
    }

    public enum canvasEditMode
    {
        UL = 0,
        UR,
        LL,
        LR,
        M
    }
#endif

    public class calibrator : MonoBehaviour
    {
        
#if !HYPERCUBE_DEV
        [Header("-Requires HYPERCUBE_DEV define-")]

        //don't lose these references, but they are useless without the define so hide them.
        
        public castMesh canvas;
        [HideInInspector]
        public Texture2D calibrationCorner;
        [HideInInspector]
        public Texture2D calibrationCenter;
        [HideInInspector]
        public Material selectedMat;
        [HideInInspector]
        public Material offMat;
#else

        public string current;
        public castMesh canvas;
        public float brightness = 3f;

        [Tooltip("How sensitive do you want your calibrations to be.")]
        public float interval = 1f;
        [Tooltip("Pixel movement will cause the interval to cause interval * pixel movements. Spatial will feel more intuitive if you are working directly on the volume.")]
        public distortionCompensationType relativeTo = distortionCompensationType.SPATIAL;

        //these default to upside down because the 'default' orientation of Volume is upside down on the castMesh if viewed in a normal monitor
        //this may sound strange, but it keeps the most difficult code inside castMesh more readable and corresponding to intuition.
        public KeyCode nextSlice = KeyCode.R;
        public KeyCode prevSlice = KeyCode.F;
        public KeyCode highlightUL = KeyCode.Z;
        public KeyCode highlightUR = KeyCode.C;
        public KeyCode highlightLL = KeyCode.Q;
        public KeyCode highlightLR = KeyCode.E;
        public KeyCode highlightM = KeyCode.S;
        public KeyCode up = KeyCode.X;
        public KeyCode down = KeyCode.W;
        public KeyCode left = KeyCode.A;
        public KeyCode right = KeyCode.D;
        public KeyCode skewXUp = KeyCode.LeftArrow;
        public KeyCode skewXDn = KeyCode.RightArrow;
        public KeyCode skewYUp = KeyCode.UpArrow;
        public KeyCode skewYDn = KeyCode.DownArrow;
        public KeyCode bowXUp = KeyCode.L;
        public KeyCode bowXDn = KeyCode.O;
        public KeyCode bowYUp = KeyCode.K;
        public KeyCode bowYDn = KeyCode.Semicolon;

        public Texture2D calibrationCorner;
        public Texture2D calibrationCenter;
        public Material selectedMat;
        public Material offMat;

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
                    currentSlice = canvas.getSliceCount() - 1;
            }
            else if (Input.GetKeyDown(skewXDn))
            {
                float xPixel = 2f / canvas.sliceWidth;  //here it is 2 instead of 1 because x raw positions correspond from -1 to 1, while y raw positions correspond from 0 to 1
                if (relativeTo == distortionCompensationType.SPATIAL)
                    xPixel *= canvas.getSliceCount();
                canvas.makeSkewAdjustment(currentSlice, true, interval * xPixel);
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
            else if (Input.GetKeyDown(bowXUp))
            {
                float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
                canvas.makeBowAdjustment(currentSlice, true, -interval * yPixel);
            }
            else if (Input.GetKeyDown(bowXDn))
            {
                float yPixel = 1f / ((float)canvas.sliceHeight * canvas.getSliceCount());
                canvas.makeBowAdjustment(currentSlice, true, interval * yPixel);
            }
            else if (Input.GetKeyDown(bowYUp))
            {
                float xPixel = 2f / canvas.sliceWidth; //the xpixel makes the movement distance between x/y equivalent (instead of just a local transform)
                if (relativeTo == distortionCompensationType.SPATIAL)
                    xPixel *= canvas.getSliceCount();
                canvas.makeBowAdjustment(currentSlice, false, interval * xPixel);
            }
            else if (Input.GetKeyDown(bowYDn))
            {
                float xPixel = 2f / canvas.sliceWidth; //the xpixel makes the movement distance between x/y equivalent (instead of just a local transform)
                if (relativeTo == distortionCompensationType.SPATIAL)
                    xPixel *= canvas.getSliceCount();
                canvas.makeBowAdjustment(currentSlice, false, -interval * xPixel);
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
                canvas = GetComponent<castMesh>();
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
#endif

    }

}