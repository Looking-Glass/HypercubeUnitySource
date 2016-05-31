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

    public Material highlightMat;
    public Material calibrationMat;
    public Mesh aQuad;
    public Mesh aWorkingMesh;

   // public bool forceLoadFromFile = false;

    canvasEditMode m;
    int currentSlice;

    void Start()
    {
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

        updateSelection();
    }


    void updateSelection()
    {
        current = "s" + currentSlice + "  " + m.ToString();

        Material mat = GetComponent<MeshRenderer>().sharedMaterial;
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

        //handle geometry
        if (calibration)
        {
            //construct the mesh from scratch
            transform.localPosition = Vector3.zero;
            GetComponent<MeshFilter>().mesh = aWorkingMesh;
            updateMesh();
        }
        else
        {
            //set to slice:
            float sliceSize = 1f / (float)cam.slices;
            transform.localPosition = new Vector3(0f, 0f, (currentSlice * sliceSize) - .5f + (sliceSize / 2)); //the -.5f is an offset because 0 is the center of the cube, sliceSize/2 puts it in the center of the slice
            GetComponent<MeshFilter>().mesh = aQuad;
        }
    }


    public void updateMesh()
    {      
        if (cam.slices < 1)
            return;  

        
        int sliceCount = cam.slices;         

        Vector3[] verts = new Vector3[4 * sliceCount]; //4 verts in a quad * slices * dimensions  
        Vector2[] uvs = new Vector2[4 * sliceCount];
        Vector3[] normals = new Vector3[4 * sliceCount]; //normals are necessary for the transparency shader to work (since it uses it to calculate camera facing)
        List<int> normalTris = new List<int>(); //the triangle list(s)
        List<int> highlightTris = new List<int>(); 

        float sliceSize = 1f / (float)sliceCount;

        //create the mesh

        for (int z = 0; z < sliceCount; z++)
        {
            int v = z * 4;
            float zPos = ((float)z * sliceSize) - .5f + (sliceSize / 2); //the -.5f is an offset because 0 is the center of the cube, sliceSize/2 puts it in the center of the slice

            verts[v + 0] = new Vector3(-.5f, .5f, zPos); //top left
            verts[v + 1] = new Vector3(.5f, .5f, zPos); //top right
            verts[v + 2] = new Vector3(.5f, -.5f, zPos); //bottom right
            verts[v + 3] = new Vector3(-.5f, -.5f, zPos); //bottom left
            normals[v + 0] = new Vector3(0, 0, 1);
            normals[v + 1] = new Vector3(0, 0, 1);
            normals[v + 2] = new Vector3(0, 0, 1);
            normals[v + 3] = new Vector3(0, 0, 1);

            uvs[v + 0] = new Vector2(0, 1);
            uvs[v + 1] = new Vector2(1, 1);
            uvs[v + 2] = new Vector2(1, 0);
            uvs[v + 3] = new Vector2(0, 0);


            int[] tris = new int[6];
            tris[0] = v + 0; //1st tri starts at top left
            tris[1] = v + 1;
            tris[2] = v + 2;
            tris[3] = v + 2; //2nd triangle begins here
            tris[4] = v + 3;
            tris[5] = v + 0; //ends at bottom right     
            if (z == currentSlice)
                highlightTris.AddRange(tris);
            else
                normalTris.AddRange(tris);
        }


        MeshRenderer r = GetComponent<MeshRenderer>();
        MeshFilter mf = GetComponent<MeshFilter>();

        Mesh m = mf.sharedMesh;
        if (!m)
            m = new Mesh(); //probably some in-editor state where things aren't init.
        m.Clear();
        m.vertices = verts;
        m.uv = uvs;
        m.normals = normals;

        m.subMeshCount = 2;
        m.SetTriangles(highlightTris, 0); //selected material
        m.SetTriangles(normalTris, 1); //unselected material

        Material[] faceMaterials = new Material[2];
        faceMaterials[0] = highlightMat;
        faceMaterials[1] = calibrationMat;

        r.sharedMaterials = faceMaterials;

        m.RecalculateBounds();
    }
}
