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

namespace hypercube
{

    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class hypercubePreview : MonoBehaviour
    {
        [Tooltip("If Hypercube does not detect a connected Volume, it will show the preview instead. Uncheck this to always show the sliced view anyway.")]
        public bool allowIntroView = true; 
        public static hypercubePreview preview; //access from anywhere.

        [HideInInspector]
        public int sliceCount = 12;
        public float sliceDistance = .1f;

        public GameObject previewCamera;

        public List<Material> previewMaterials;
        public Shader previewShader;
        public Material previewOccludedMaterial;

        bool occludedMode = false;
        public void setOccludedMode(bool onOff)
        {
            occludedMode = onOff;
            updateMesh();
        }

        void OnEnable()
        {
            preview = this;
        }

        void Start()
        {
            //try in a lazy way to connect ourselves
            if (castMesh.canvas && !castMesh.canvas.preview)
                castMesh.canvas.preview = this;

            if (hypercubeCamera.mainCam)
                updateMaterials(hypercubeCamera.mainCam); //try to ensure we get off to a good start.
        }

        void OnValidate()
        {
            updateMesh();
        }

        void Update()
        {
            //automatically update whatever we are doing to match the current hypercube camera.
            if (!hypercubeCamera.mainCam)
                return;

            //check for any bad or changed config, and rebuild accordingly.
            if (sliceCount != hypercubeCamera.mainCam.sliceTextures.Length || 
                previewOccludedMaterial.mainTexture == null ||
                previewMaterials.Count == 0 ||
				previewMaterials[0] == null ||
                previewMaterials[0].mainTexture == null ||
                previewMaterials[0].mainTexture.width != castMesh.rttResX ||
                previewMaterials[0].mainTexture.height != castMesh.rttResY 

            )
                updateMaterials(hypercubeCamera.mainCam);
                
        }

        public void updateMaterials(hypercubeCamera c)
        {
            //fill the material array in such a way as to respect any elements that have been overriden by the dev.


            int count = c.sliceTextures.Length;
            while (previewMaterials.Count > count)
                previewMaterials.RemoveAt(previewMaterials.Count - 1);//rip the end off if its too many.
            
            for (int i = 0; i < count; i++)
            {
                if (previewMaterials.Count <= i)
                    previewMaterials.Add(new Material(previewShader));
                else if (previewMaterials[i] == null)
                    previewMaterials[i] = new Material(previewShader);

                previewMaterials[i].mainTexture = c.sliceTextures[i];
            }

            previewOccludedMaterial.mainTexture = c.occlusionRTT; //don't forget also to update our occlusion rtt
            updateMesh();
        }

        public void updateMesh()
        {
            sliceCount = previewMaterials.Count;

            if (sliceCount < 1)
                return;
                
            sliceDistance = 1f / (float)sliceCount;

            Vector3[] verts = new Vector3[4 * sliceCount]; //4 verts in a quad * slices * dimensions  
            Vector2[] uvs = new Vector2[4 * sliceCount];
            Vector3[] normals = new Vector3[4 * sliceCount]; //normals are necessary for the transparency shader to work (since it uses it to calculate camera facing)
            List<int[]> submeshes = new List<int[]>(); //the triangle list(s)
            Material[] faceMaterials = new Material[sliceCount];

            //create the mesh
            for (int z = 0; z < sliceCount; z++)
            {
                int v = z * 4;
                float zPos = (float)z * sliceDistance;

                verts[v + 0] = new Vector3(-.5f, .5f, zPos); //top left
                verts[v + 1] = new Vector3(.5f, .5f, zPos); //top right
                verts[v + 2] = new Vector3(.5f, -.5f, zPos); //bottom right
                verts[v + 3] = new Vector3(-.5f, -.5f, zPos); //bottom left
                normals[v + 0] = new Vector3(0, 0, 1);
                normals[v + 1] = new Vector3(0, 0, 1);
                normals[v + 2] = new Vector3(0, 0, 1);
                normals[v + 3] = new Vector3(0, 0, 1);

                if (!occludedMode)
                {
                    uvs[v + 0] = new Vector2(0, 1);
                    uvs[v + 1] = new Vector2(1, 1);
                    uvs[v + 2] = new Vector2(1, 0);
                    uvs[v + 3] = new Vector2(0, 0);
                }
                else
                {
                    float sliceMod = 1f / (float)sliceCount;
                    uvs[v + 0] = new Vector2(0, sliceMod * (float)(z + 1));
                    uvs[v + 1] = new Vector2(1, sliceMod * (float)(z + 1));
                    uvs[v + 2] = new Vector2(1, sliceMod * (float)z);
                    uvs[v + 3] = new Vector2(0, sliceMod * (float)z);
                }


                int[] tris = new int[6];
                tris[0] = v + 0; //1st tri starts at top left
                tris[1] = v + 1;
                tris[2] = v + 2;
                tris[3] = v + 2; //2nd triangle begins here
                tris[4] = v + 3;
                tris[5] = v + 0; //ends at bottom right       
                submeshes.Add(tris);

                //every face has a separate material/texture  
                if (!occludedMode)   
                    faceMaterials[z] = previewMaterials[z];
                else
                    faceMaterials[z] = previewOccludedMaterial;
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

            m.subMeshCount = sliceCount;
            for (int s = 0; s < sliceCount; s++)
            {
                m.SetTriangles(submeshes[s], s);
            }

            r.sharedMaterials = faceMaterials;

            m.RecalculateBounds();
        }
    }

}
