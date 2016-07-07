using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this script does nothing.
//It's purpose is so that the hypercube can query if an appropriate canvas exists in the scene.
//If it doesn't exist, it will create one.

public enum canvasEditMode
{
    UL = 0,
    UR,
    LL,
    LR,
    M
}

[ExecuteInEditMode]
public class hypercubeCanvas : MonoBehaviour 
{

    public bool flipX = false;
    public float sliceOffsetX = 0;
    public float sliceOffsetY = 0;
    int sliceCount = 12; //this is given by the attached hypercube
    public float sliceWidth = 1920;
    public float sliceHeight = 108;
    public float zPos = .01f;
    public GameObject sliceMesh;

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

    public void setCustomWidthHeight(float w, float h)
    {
        if (w == 0 || h == 0) //bogus values. Possible, if the window is still setting up.
            return;

        usingCustomDimensions = true;
        customWidth = w;
        customHeight = h;
    }

  
    //tweaks to the cube design to offset physical distortions
    public void setCalibrationOffsets(dataFileAssoc d, int maxSlices)
    {
        ULOffsets = new Vector2[maxSlices]; //init our calibration vars
        UROffsets = new Vector2[maxSlices];
        LLOffsets = new Vector2[maxSlices];
        LROffsets = new Vector2[maxSlices];
        MOffsets = new Vector2[maxSlices];

        for (int s = 0; s < maxSlices; s++)
        {
            ULOffsets[s].x = d.getValueAsFloat("s" + s + "_ULx", 0f);
            ULOffsets[s].y = d.getValueAsFloat("s" + s + "_ULy", 0f);
            UROffsets[s].x = d.getValueAsFloat("s" + s + "_URx", 0f);
            UROffsets[s].y = d.getValueAsFloat("s" + s + "_URy", 0f);
            LLOffsets[s].x = d.getValueAsFloat("s" + s + "_LLx", 0f);
            LLOffsets[s].y = d.getValueAsFloat("s" + s + "_LLy", 0f);
            LROffsets[s].x = d.getValueAsFloat("s" + s + "_LRx", 0f);
            LROffsets[s].y = d.getValueAsFloat("s" + s + "_LRy", 0f);
            MOffsets[s].x = d.getValueAsFloat("s" + s + "_Mx", 0f);
            MOffsets[s].y = d.getValueAsFloat("s" + s + "_My", 0f);
        }
    }

    public void saveCalibrationOffsets(dataFileAssoc d)
    {
        for (int s = 0; s < ULOffsets.Length; s++)
        {
            d.setValue("s" + s + "_ULx", ULOffsets[s].x.ToString(), true);
            d.setValue("s" + s + "_ULy", ULOffsets[s].y.ToString(), true);
            d.setValue("s" + s + "_URx", UROffsets[s].x.ToString(), true);
            d.setValue("s" + s + "_URy", UROffsets[s].y.ToString(), true);
            d.setValue("s" + s + "_LLx", LLOffsets[s].x.ToString(), true);
            d.setValue("s" + s + "_LLy", LLOffsets[s].y.ToString(), true);
            d.setValue("s" + s + "_LRx", LROffsets[s].x.ToString(), true);
            d.setValue("s" + s + "_LRy", LROffsets[s].y.ToString(), true);
            d.setValue("s" + s + "_Mx", MOffsets[s].x.ToString(), true);
            d.setValue("s" + s + "_My", MOffsets[s].y.ToString(), true);
        }
    }

    void OnValidate()
    {

        if (sliceCount < 1)
            sliceCount = 1;

        if (!sliceMesh)
            return;

        updateMesh(sliceCount);
        resetTransform();       
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            resetTransform();
        }
    }

    public int getSliceCount()
    {
        return sliceCount;
    }

    public bool makeAdjustment(int slice, canvasEditMode m, bool x, float amount)
    {
        if (slice < 0)
            return false;
        if (slice >= ULOffsets.Length)
            return false;

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

        updateMesh(sliceCount);

        return true;
    }
    public void flip()
    {
        flipX = !flipX;
        updateMesh(sliceCount);
    }
    public void sliceHeightUp()
    {
        sliceHeight += .2f;
        updateMesh(sliceCount);
    }
    public void sliceHeightDown()
    {
        sliceHeight -= .2f;
        updateMesh(sliceCount);
    }
    public void nudgeUp()
    {
        sliceOffsetY += .2f;
        updateMesh(sliceCount);
    }
    public void nudgeDown()
    {
        sliceOffsetY -= .2f;
        updateMesh(sliceCount);
    }
    public void nudgeLeft()
    {
        sliceOffsetX -= 1f;
        updateMesh(sliceCount);
    }
    public void nudgeRight()
    {
        sliceOffsetX += 1f;
        updateMesh(sliceCount);
    }
    public void widthUp()
    {
        sliceWidth += 1f;
        updateMesh(sliceCount);
    }
    public void widthDown()
    {
        sliceWidth -= 1f;
        updateMesh(sliceCount);
    }
    public void setPreset1()
    {
        sliceHeight = 120f;
        updateMesh(sliceCount);
    }
    public void setPreset2()
    {
        sliceHeight = 68f;
        updateMesh(sliceCount);
    }


    void resetTransform() //size the mesh appropriately to the screen
    {
        if (!sliceMesh)
            return;

        if (Screen.width < 1 || Screen.height < 1)
            return; //wtf.


        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float xPixel = 1f / (float)Screen.width;
        float yPixel = 1f / (float)Screen.height;

        float outWidth = (float)Screen.width; 

        if (usingCustomDimensions && customWidth > 2 && customHeight > 2)
        {
            aspectRatio = customWidth / customHeight;
            xPixel = 1f /customWidth;
            yPixel = 1f / customHeight;

            outWidth = customWidth;
        }



        sliceMesh.transform.localScale = new Vector3(sliceWidth * xPixel * aspectRatio, (float)sliceCount * sliceHeight * 2f * yPixel, 1f); //the *2 is because the view is 2 units tall
      //  sliceMesh.transform.localPosition = new Vector3(xPixel * sliceOffsetX, (yPixel * sliceOffsetY * 2f) - 1f, zPos); //the -1f is the center vertical on the screen, the *2 is because the view is 2 units tall

        //horizontal slicer
        sliceMesh.transform.localPosition = new Vector3((xPixel * aspectRatio * outWidth) + (xPixel * sliceOffsetY), (yPixel * sliceOffsetX * 2f), zPos); //this assumes the mesh is rotated 90 degrees to the left
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

    
    public void updateMesh(int _sliceCount)
    {
        if (_sliceCount < 1)
            return;

        sliceCount = _sliceCount;

        if (ULOffsets == null || ULOffsets.Length < sliceCount) //if they don't exist yet, just use temporary values
        {
            ULOffsets = new Vector2[sliceCount];
            UROffsets = new Vector2[sliceCount];
            LLOffsets = new Vector2[sliceCount];
            LROffsets = new Vector2[sliceCount];
            MOffsets = new Vector2[sliceCount];
            for (int s = 0; s < sliceCount; s++)
            {
                ULOffsets[s] = new Vector2(0f,0f);
                UROffsets[s] = new Vector2(0f, 0f);
                LLOffsets[s] = new Vector2(0f, 0f);
                LROffsets[s] = new Vector2(0f, 0f);
                MOffsets[s] = new Vector2(0f, 0f);
            }
        }

        //each slice is constructed from 8 triangles radiating from a vert in the center of the slice.
        if (canvasMaterials.Count == 0)
        {
            Debug.LogError("Canvas materials have not been set!  Please define what materials you want to apply to each slice in the hypercubeCanvas component.");
            return;
        }

        if (sliceCount < 1 )
        {
            sliceCount = 1;
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

        if (sliceCount > canvasMaterials.Count)
        {
            Debug.LogWarning("Can't add more than " + canvasMaterials.Count + " slices, because only " + canvasMaterials.Count + " canvas materials are defined.");
            sliceCount = canvasMaterials.Count;
            return;
        }

        Vector3[] verts = new Vector3[9 * sliceCount]; //9 verts in each slice 
        Vector2[] uvs = new Vector2[9 * sliceCount];
        Vector3[] normals = new Vector3[9 * sliceCount]; //normals are necessary for the transparency shader to work (since it uses it to calculate camera facing)
        List<int[]> submeshes = new List<int[]>(); //the triangle list(s)
        Material[] faceMaterials = new Material[sliceCount];

        //create the mesh
        float size = 1f / (float)sliceCount;
        for (int s = 0; s < sliceCount; s++)
        {
            int v = s * 9;
            float yPos =  (float)s * size;

            verts[v + 0] = new Vector3(-1f + ULOffsets[s].x, yPos + size + ULOffsets[s].y, 0f); //top left          
            verts[v + 1] = new Vector3(MOffsets[s].x, yPos + size + Mathf.Lerp(ULOffsets[s].y, UROffsets[s].y, Mathf.InverseLerp(-1f + ULOffsets[s].x, 1f + UROffsets[s].x, MOffsets[s].x)), 0f); //top middle
            verts[v + 2] = new Vector3(1f + UROffsets[s].x, yPos + size + UROffsets[s].y, 0f); //top right

            verts[v + 3] = new Vector3(-1f + Mathf.Lerp(ULOffsets[s].x, LLOffsets[s].x, Mathf.InverseLerp(size + ULOffsets[s].y, LLOffsets[s].y, (size / 2) + MOffsets[s].y)), yPos + (size / 2) + MOffsets[s].y, 0f); //middle left
            verts[v + 4] = new Vector3(MOffsets[s].x, yPos + (size / 2) + MOffsets[s].y, 0f); //center
            verts[v + 5] = new Vector3(1f + +Mathf.Lerp(UROffsets[s].x, LROffsets[s].x, Mathf.InverseLerp(size + UROffsets[s].y, LROffsets[s].y, (size / 2) + MOffsets[s].y)), yPos + (size / 2) + MOffsets[s].y, 0f); //middle right

            verts[v + 6] = new Vector3(-1f + LLOffsets[s].x, yPos + LLOffsets[s].y, 0f); //bottom left
            verts[v + 7] = new Vector3(MOffsets[s].x, yPos + Mathf.Lerp(LLOffsets[s].y, LROffsets[s].y, Mathf.InverseLerp(-1f + LLOffsets[s].x, 1f + LROffsets[s].x, MOffsets[s].x)), 0f); //bottom middle
            verts[v + 8] = new Vector3(1f + LROffsets[s].x, yPos + LROffsets[s].y, 0f); //bottom right         

            normals[v + 0] = Vector3.forward;
            normals[v + 1] = Vector3.forward;
            normals[v + 2] = Vector3.forward;
            normals[v + 3] = Vector3.forward;
            normals[v + 4] = Vector3.forward;
            normals[v + 5] = Vector3.forward;
            normals[v + 6] = Vector3.forward;
            normals[v + 7] = Vector3.forward;
            normals[v + 8] = Vector3.forward;

            if (!flipX)
            {
                uvs[v + 0] = new Vector2(0, 0);
                uvs[v + 1] = new Vector2(.5f, 0);
                uvs[v + 2] = new Vector2(1, 0);
                uvs[v + 3] = new Vector2(0, .5f);
                uvs[v + 4] = new Vector2(.5f, .5f);
                uvs[v + 5] = new Vector2(1f, .5f);
                uvs[v + 6] = new Vector2(0, 1);
                uvs[v + 7] = new Vector2(.5f, 1);
                uvs[v + 8] = new Vector2(1, 1);
            }
            else
            {
                uvs[v + 0] = new Vector2(1, 0);
                uvs[v + 1] = new Vector2(.5f, 0);
                uvs[v + 2] = new Vector2(0, 0);
                uvs[v + 3] = new Vector2(1, .5f);
                uvs[v + 4] = new Vector2(.5f, .5f);
                uvs[v + 5] = new Vector2(0f, .5f);
                uvs[v + 6] = new Vector2(1, 1);
                uvs[v + 7] = new Vector2(.5f, 1);
                uvs[v + 8] = new Vector2(0, 1);
            }



            int[] tris = new int[24];  //8 tris in a circle around the center
            tris[0] = v + 0; //1st tri starts at top left
            tris[1] = v + 1;
            tris[2] = v + 4;
            tris[3] = v + 1; //2nd triangle begins here
            tris[4] = v + 2;
            tris[5] = v + 4;    
            tris[6] = v + 2; //3rd triangle begins here
            tris[7] = v + 5;
            tris[8] = v + 4;
            tris[9] = v + 5; //4th triangle begins here
            tris[10] = v + 8;
            tris[11] = v + 4;
            tris[12] = v + 8; //5th triangle begins here
            tris[13] = v + 7;
            tris[14] = v + 4;
            tris[15] = v + 7; //6th triangle begins here
            tris[16] = v + 6;
            tris[17] = v + 4;
            tris[18] = v + 6; //7th triangle begins here
            tris[19] = v + 3;
            tris[20] = v + 4;
            tris[21] = v + 3; //8th triangle begins here
            tris[22] = v + 0;
            tris[23] = v + 4; 
            submeshes.Add(tris);

            //every face has a separate material/texture     
            faceMaterials[s] = canvasMaterials[s];
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
        m.vertices = verts;
        m.uv = uvs;
        m.normals = normals;

        m.subMeshCount = sliceCount;
        for (int s = 0; s < sliceCount; s++)
        {
            m.SetTriangles(submeshes[s], s);
        }

        r.materials = faceMaterials;

        m.RecalculateBounds();
    }
	
}
