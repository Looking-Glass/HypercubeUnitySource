using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class dicomMeshLoader: MonoBehaviour {

    
    public float valueMod = 1f;
    public float size = 1f;
    public int interpolatedSlices = 30;

    public GameObject volumeMesh;

    Texture3D tex = null;
    MeshRenderer r = null;
    public MeshRenderer getRenderer() { return r; }

    int lastInterpolatedSlices = 0;
    int imageSlices = 0;

    float ratioX = 1f; //these hold the ratio of:  trueImageWidth/textureWidth since the texture must be power of two.
    float ratioY = 1f;
    float ratioZ = 1f;



    void OnValidate() //things that can be modified directly on the component
    {
        if (lastInterpolatedSlices != interpolatedSlices && imageSlices > 0)
            createFrameMesh();  

        if (r)
            r.material.SetFloat("_Mod", valueMod);
    }


    public void updateThickness(float val)
    {
        interpolatedSlices = (int)((float)imageSlices * val);
        if (lastInterpolatedSlices != interpolatedSlices && imageSlices > 0)
            createFrameMesh();  
    }



    public virtual bool loadFrame(string dirPath, Material m) //load a 3D frame, not to be confused with a slice
    {
        // find out how may slices it has
        var info = new DirectoryInfo(dirPath);
        FileInfo[] fileInfo = info.GetFiles();
        List<FileInfo> slices = new List<FileInfo>();
        foreach (FileInfo file in fileInfo)
        {
            if (file.Name.EndsWith(".png") ||
                        file.Name.EndsWith(".jpg") ||
                        file.Name.EndsWith(".jpeg") ||
                        file.Name.EndsWith(".tif") ||
                        file.Name.EndsWith(".bmp")
                )
                slices.Add(file);
        }

        imageSlices = slices.Count;
        if (imageSlices < 1)
            return false;

        interpolatedSlices = imageSlices; //start off with the same geometric density in w/h/d

        //load the first texture to get the stats
        Texture2D tempTex = new Texture2D(2, 2);
        byte[] pngBytes = File.ReadAllBytes(slices[0].FullName);
        if (!tempTex.LoadImage(pngBytes))        // Load data into the texture.
            return false;

        int w = 1; //these hold the actual size of the 3D texture which may be larger than the size of the slices because it must conform to power of 2
        int h = 1;
        int d = 1; 
        while (w < tempTex.width)
            w *= 2; //the power of two width
        while (h < tempTex.height)
            h *= 2; //the power of two height
        while (d < imageSlices)
            d *= 2; //the power of two depth

        //determine the appropriate UV and scale values
        ratioX = (float)tempTex.width / (float)w;
        ratioY = (float)tempTex.height / (float)h;
        ratioZ = (float)imageSlices / (float)d;
        transform.localScale = new Vector3( (float)tempTex.width/(float)tempTex.height, 1f, 1f); //scale the mesh.  TODO - Z here SHOULD BE INFLUENCED BY THE DISTANCE BETWEEN SLICES!


        Color[] colors = null;
        Debug.Log("Creating 3D texture with W:" + w+ " H:" + h + " D:" +d);
        colors = new Color[w * h * d];
        //List<Color> colors = new List<Color>();

        //load all the slices into the 3D texture        
        for(int z = 0; z < d;  z++)
        {
            //load each texture.      
            if (z != 0 && z < imageSlices) //don't reload the first texture (and just reuse the last texture to make the data meet power of 2 depth)
            {
                pngBytes = File.ReadAllBytes(slices[z].FullName);
             //   DestroyImmediate(tempTex);
                tempTex = new Texture2D(w, h);
                if (!tempTex.LoadImage(pngBytes))        // Load data into the texture.
                    return false;
            }


      //      if (tempTex.width == w && tempTex.height == h)
       //         colors.AddRange(tempTex.GetPixels()); //this is an optimization to avoid setting every pixel manually for the texture
      //      else
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int index = x + (y * w) + (z * w * h);
                        if (y >= tempTex.height || x >= tempTex.width || z >= imageSlices) //fill any extra pixels with black.
                            colors[index] = new Color(0f, 0f, 0f);
                        else
                            colors[index] = tempTex.GetPixel(x, y);  
                        //if (y >= tempTex.height || x >= tempTex.width || z >= imageSlices) //fill any extra pixels with black.
                        //    colors.Add(Color.black);
                        // else
                        //     colors.Add(tempTex.GetPixel(x, y));  
                    }
                }
            }
         
       //     tempTex.Resize(2, 2);  //get our memory back.
        }


        tex = new Texture3D(w, h, d, tempTex.format, false);
        tex.SetPixels(colors);
        tex.Apply(); //send to gpu
        tex.wrapMode = TextureWrapMode.Clamp;

        if (!r)
        {
            if (!volumeMesh)
                volumeMesh = gameObject;

            r = volumeMesh.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.sharedMaterial.mainTexture = tex;
        }

        createFrameMesh();

        valueMod = .1f; //start off kind of transparent
        OnValidate();

        return true;
    }

    void createFrameMesh()
    {
        int slicesX = interpolatedSlices;
        int slicesY = interpolatedSlices;
        int slicesZ = imageSlices;

        if (slicesX < 1)
            slicesX = 1;
        if (slicesY < 1)
            slicesY = 1;
        if (slicesZ < 1)
            slicesZ = 1;

        lastInterpolatedSlices = interpolatedSlices;

        int faceCount = slicesX + slicesY + slicesZ;
        Vector3[] verts = new Vector3[4 * faceCount]; 
        Vector2[] uvs = new Vector2[4 * faceCount];
        Vector3[] normals = new Vector3[4 * faceCount]; //normals are necessary for the transparency shader to work (since it uses it to calculate camera facing)
        int[] tris = new int[6 * faceCount];  //triangle 1 verts + triangle 2 verts * (x + y +z)

        //create the slices along y , the main slices
        for (int y = 0; y < slicesY; y++)
        {
            int v = y * 4;
            float yPos = (1f/(float)slicesY) * (float)y * size;
            //yPos += 10; //TEMP!!
            verts[v + 0] = new Vector3(0, yPos, 0); //top left
            verts[v + 1] = new Vector3(0, yPos, size); //top right
            verts[v + 2] = new Vector3(size, yPos, size); //bottom right
            verts[v + 3] = new Vector3(size, yPos, 0); //bottom left
            normals[v + 0] = new Vector3(0, 1, 0);
            normals[v + 1] = new Vector3(0, 1, 0);
            normals[v + 2] = new Vector3(0, 1, 0);
            normals[v + 3] = new Vector3(0, 1, 0);
            uvs[v + 0] = new Vector2(0, 0); //these don't matter much since this mesh will have a 3D texture applied
            uvs[v + 1] = new Vector2(0, 1);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(1, 0);

            int t = y * 6;
            tris[t + 0] = v + 0; //1st tri starts at top left
            tris[t + 1] = v + 1;
            tris[t + 2] = v + 2;
            tris[t + 3] = v + 2; //2nd triangle begins here
            tris[t + 4] = v + 3;
            tris[t + 5] = v + 0; //ends at bottom right       
        }
        
        //create the slices along z, the side slices      
        for (int z = 0; z < slicesZ; z++) //the <= accounts for the end cap
        {
            int v = (z * 4) + (slicesY * 4); //the slicecount * 4 here must take into account the previous verts set for the existing slices                
            float zPos = 1f / (float)slicesZ *(float)z * size;
            //zPos += 10; //temp
            verts[v + 0] = new Vector3(0, size, zPos); //top left
            verts[v + 1] = new Vector3(size, size, zPos); //top right
            verts[v + 2] = new Vector3(size, 0, zPos); //bottom right
            verts[v + 3] = new Vector3(0, 0, zPos); //bottom left
            normals[v + 0] = new Vector3(0, 0, 1);
            normals[v + 1] = new Vector3(0, 0, 1);
            normals[v + 2] = new Vector3(0, 0, 1);
            normals[v + 3] = new Vector3(0, 0, 1);
            uvs[v + 3] = new Vector2(0, 0); //these don't matter much since this mesh will have a 3D texture applied
            uvs[v + 0] = new Vector2(0, 1);
            uvs[v + 1] = new Vector2(1, 1);
            uvs[v + 2] = new Vector2(1, 0);

            int t = (z * 6) + (slicesY * 6);
            tris[t + 0] = v + 0; //1st tri starts at top left
            tris[t + 1] = v + 1;
            tris[t + 2] = v + 2;
            tris[t + 3] = v + 2; //2nd triangle begins here
            tris[t + 4] = v + 3;
            tris[t + 5] = v + 0; //ends at bottom right       
        }

        //create the slices along x, the depth slices      
        for (int x = 0; x < slicesX; x++) //the <= accounts for the end cap
        {
            int v = (x * 4) + ((slicesY + slicesZ) * 4); //the slicecount * 4 here must take into account the previous verts set for the existing slices, and the 2 dimensions                
            float xPos = 1f / (float)slicesX *(float)x * size;
            //xPos += 10f; //TEMP
            verts[v + 0] = new Vector3(xPos, size, 0); //top left
            verts[v + 1] = new Vector3(xPos, size, size); //top right
            verts[v + 2] = new Vector3(xPos, 0, size); //bottom right
            verts[v + 3] = new Vector3(xPos, 0, 0); //bottom left
            normals[v + 0] = new Vector3(1, 0, 0); 
            normals[v + 1] = new Vector3(1, 0, 0);
            normals[v + 2] = new Vector3(1, 0, 0);
            normals[v + 3] = new Vector3(1, 0, 0);
            uvs[v + 3] = new Vector2(0, 0); //these don't matter much since this mesh will have a 3D texture applied
            uvs[v + 0] = new Vector2(0, 1);
            uvs[v + 1] = new Vector2(1, 1);
            uvs[v + 2] = new Vector2(1, 0);

            int t = (x * 6) + ((slicesY + slicesZ) * 6);
            tris[t + 0] = v + 0; //1st tri starts at top left
            tris[t + 1] = v + 1;
            tris[t + 2] = v + 2;
            tris[t + 3] = v + 2; //2nd triangle begins here
            tris[t + 4] = v + 3;
            tris[t + 5] = v + 0; //ends at bottom right   
        }


        if (!volumeMesh)
            volumeMesh = gameObject;

        MeshFilter mf = volumeMesh.GetComponent<MeshFilter>();
        if (!mf)
            mf = volumeMesh.AddComponent<MeshFilter>();

        Mesh m = mf.mesh;
        m.Clear();

        m.vertices = verts;
        m.uv = uvs;
        m.normals = normals;

        m.subMeshCount = 0;
        m.triangles = tris;

        r.material.SetVector( "_Scale" , new Vector4(ratioX, ratioY, ratioZ, 1f)); //size the used texture onto the entire cube (so that any black overlap that was created when making it power of 2 is UV'd outside the mesh)

        m.RecalculateBounds();
    }



	

}
