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

[ExecuteInEditMode]
public class hypercubeCamera : MonoBehaviour
{
     public const float version = 2.41f;

     //a static pointer to the last activated hypercubeCameraZ
     public static hypercubeCamera mainCam = null;  


    public enum renderMode
    {
        HARD = 0,
        PER_MATERIAL,
        POST_PROCESS,
        OCCLUDING
    }
    [Tooltip("This option chooses the rendering method of Hypercube:\n\nHARD - No slice blending. Blending will be OFF. \n\nPER_MATERIAL - Meshes will only be soft sliced if they use Hypercube/ shaders, but all objects will draw. Use this method and use Hypercube shaders if you want to have effects show well in Volume.\n\nPOST_PROCESS - Uses the depth buffer in a post process to calculate soft slicing. This means Shaders that do not ZWrite will be treated as empty space and draw black (effects, or transparent things). However, ANY opaque shader will be soft sliced. Use this if you want soft slicing, but don't want to use Hypercube shaders. \n\nOCCLUDING - Draws the scene one time, and then uses a post process to determine what slices a pixel draws to. The result is that pixels drawn to 'front' slices occlude pixels drawn behind them. Effects and transparent shaders will most likely draw to wrong slices with this method (because they typically use ZWrite OFF). Framing whole models (like a human head or whole opaque object) tend to show well with this method.\n")]
    public renderMode softSliceMethod;

    [Tooltip("The percentage of overdraw a slice will include of its neighbor slices.\n\nEXAMPLE: an overlap of 1 will include its front and back neighbor slices (not including their own overlaps)  into itself.\nAn overlap of .5 will include half of its front neighbor and half of its back neighbor slice.")]
    [Range(.001f, 5f)]
    public float overlap = 1f;

    [Tooltip("Sets how far into the slice blending will occur. Adjust to your preference, or use 'autoSoftness' to have it calculated for you based on the overlap setting.")]
    [Range(0.001f, .5f)]
    public float softness = .5f;

    [Tooltip("Auto-calculate softness based on the overlap.")]
    public bool autoSoftness = false;


    public enum scaleConstraintType
    {
        NONE = 0,
        X_RELATIVE,
        Y_RELATIVE,
        Z_RELATIVE
    }
    [Tooltip("This will ensure your Hypercube scale always matches the aspect ratios inside Volume 1:1.\nChoose which axis to leave free. The others will be constrained to match the value of that axis.")]
    public scaleConstraintType scaleConstraint = scaleConstraintType.NONE;


    [Tooltip("Use this to modify slices, for example each modifier can be used to add GUI, a background, or other change to a slice.\n\nNOTE:Use sliceModifier.updateSliceModifiers() to force them to update.\n\nNOTE: Slice Modifiers will not work in OCCLUDING render mode.")]
    public List<hypercube.sliceModifier> sliceModifiers;

    [Tooltip("If the hypercube_RTT camera is set to perspective, this will modify the FOV of each successive slice to create forced perspective effects.")]
    public float forcedPerspective = 0f; //0 is no forced perspective, other values force a perspective either towards or away from the front of the Volume.
    [Tooltip("Brightness is a final modifier on the output to Volume.\nCalculated value * Brightness = output")]
    public float brightness = 1f; //  a convenience way to set the brightness of the rendered textures. The proper way is to call 'setTone()' on the canvas
    [Tooltip("This can be used to differentiate between what is empty space, and what is 'black' in Volume.  This Color is ADDED to everything that has geometry.\n\nNOTE: The brighter the value here, the more color depth is effectively lost.")]
    public Color blackPoint;
    public bool autoHideMouse = true;
    

    
    public Camera renderCam;
    [HideInInspector]
    public RenderTexture[] sliceTextures;
    [HideInInspector]
    public RenderTexture occlusionRTT;
    public hypercube.castMesh castMeshPrefab;

    [HideInInspector]
    public hypercube.softOverlap softSlicePostProcess;
    [HideInInspector]
    public hypercube.slicePostProcess slicePost;
    hypercube.castMesh localCastMesh = null;

    hypercube.hypercubePreview preview = null;
   
    //store our camera values here.
    float[] nearValues;
    float[] farValues;

    void OnEnable()
    {
        hypercubeCamera.mainCam = this;
    }

    void Awake()
    {
        renderCam.depthTextureMode = DepthTextureMode.Depth;

        softSlicePostProcess = renderCam.GetComponent<hypercube.softOverlap>();
        slicePost = renderCam.GetComponent<hypercube.slicePostProcess>();
    }

    void Start()
    {
        if (!localCastMesh)
        {
            localCastMesh = hypercube.castMesh.canvas;
            if (!localCastMesh)
                localCastMesh = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (!localCastMesh)
            {
                //if no canvas exists. we need to have one or the hypercube is useless.
#if UNITY_EDITOR
                Cursor.visible = true;
                localCastMesh = UnityEditor.PrefabUtility.InstantiatePrefab(castMeshPrefab) as hypercube.castMesh;  //try to keep the prefab connection, if possible
#else
                Cursor.visible = false;
                localCastMesh = Instantiate(castMeshPrefab); //normal instantiation, lost the prefab connection
#endif
            }
        }

        if (!preview)
            preview = GameObject.FindObjectOfType<hypercube.hypercubePreview>();

        resetSettings();

		Shader.SetGlobalFloat("_hypercubeBrightnessMod", brightness); //ensure we start with correct settings
		Shader.SetGlobalColor("_blackPoint", blackPoint);
    }



    void Update()
    {

        if (autoHideMouse)
        {
#if !UNITY_EDITOR

            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            if (screenRect.Contains(Input.mousePosition))
                Cursor.visible = false;
            else
                Cursor.visible = true;          
#endif
        }

        if (!localCastMesh)
        {
            localCastMesh = hypercube.castMesh.canvas;
            if (!localCastMesh)
                localCastMesh = GameObject.FindObjectOfType<hypercube.castMesh>();
            if (!localCastMesh && sliceTextures.Length < hypercube.castMesh.defaultSliceCount)
                populateRTTs(hypercube.castMesh.defaultSliceCount, 512, 512); //the default RTT settings if no canvas found.
        }
        else
        {
            if (hypercube.sliceModifier.areModifiersNull() && sliceModifiers.Count > 0)
                hypercube.sliceModifier.updateSliceModifiers(localCastMesh.getSliceCount(), sliceModifiers);

            if (localCastMesh.getSliceCount() != sliceTextures.Length ||
              !occlusionRTT ||
              sliceTextures.Length == 0 ||
              sliceTextures[0] == null ||
              sliceTextures[0].width != hypercube.castMesh.rttResX ||
              sliceTextures[0].height != hypercube.castMesh.rttResY
          ) //dynamically fill the render textures
            {
                populateRTTs(localCastMesh.getSliceCount(), hypercube.castMesh.rttResX, hypercube.castMesh.rttResY);
            }
        }
            

        try
        {
            //maintain scale aspect ratio if desired.
            if (scaleConstraint == scaleConstraintType.NONE) 
            { }
            else if (scaleConstraint == scaleConstraintType.X_RELATIVE)
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x * localCastMesh.aspectX.y, transform.localScale.x * localCastMesh.aspectX.z);
            else if (scaleConstraint == scaleConstraintType.Y_RELATIVE)
                transform.localScale = new Vector3(transform.localScale.y * localCastMesh.aspectY.x, transform.localScale.y, transform.localScale.y * localCastMesh.aspectY.z);
            else if (scaleConstraint == scaleConstraintType.Z_RELATIVE)
                transform.localScale = new Vector3(transform.localScale.z * localCastMesh.aspectZ.x, transform.localScale.z * localCastMesh.aspectZ.y, transform.localScale.z);
        }
        catch
        {
            Debug.LogWarning("Hypercube configuration is invalid. The scale aspect ratio of the Volumetric display has not been set or has 0 values! Run calibrator again!");
        }

        if (transform.hasChanged)
        {
            resetSettings(); //you can comment this line out if you will not be scaling your cube during runtime
        }
        render();
    }

    void OnValidate()
    {
  
        if (!localCastMesh)
            localCastMesh = hypercube.castMesh.canvas;

        if (localCastMesh)
        {
            localCastMesh.setTone(brightness);
            localCastMesh.updateMesh();

            hypercube.sliceModifier.updateSliceModifiers(localCastMesh.getSliceCount(), sliceModifiers);
        }

        Shader.SetGlobalFloat("_hypercubeBrightnessMod", brightness);
        Shader.SetGlobalColor("_blackPoint", blackPoint);

        updateOverlap();

        if (!preview)
            preview = GameObject.FindObjectOfType<hypercube.hypercubePreview>();
        if (preview)
            preview.updateMesh();

        render();
    }

    public void updateOverlap()
    {
        if (softSliceMethod != renderMode.HARD)
        {
            if (autoSoftness)
                softness = overlap / ((overlap * 2f) + 1f); //this calculates exact interpolation between the end of a slice and the end of it's overlap area. Interestingly, imo it usually does not produce what the eye thinks are best results.             

            softness = Mathf.Clamp(softness, 0f, .5f);
            Shader.SetGlobalFloat("_softPercent", softness);
            Shader.SetGlobalFloat("_overlap", overlap);

            if (softSliceMethod != renderMode.PER_MATERIAL) //we use post process on both occluding and in post process SS
            {
                softSlicePostProcess.enabled = true;
                return;
            }
        }
        softSlicePostProcess.enabled = false;
    }



    public virtual void render()
    {
        int slices = sliceTextures.Length;
        if (slices == 0)
            return;

        if (overlap > 0f && softSliceMethod != renderMode.HARD)
        {
            if (softSliceMethod == renderMode.PER_MATERIAL)
                Shader.EnableKeyword("SOFT_SLICING");
            else
                renderCam.gameObject.SetActive(true); //setting it active/inactive is only needed so that OnRenderImage() will be called on softOverlap.cs for the post process effect. It is normally hidden so that the Unity camera icon won't interfere with viewing what is inside hypercube in the editor.            
        }

        if (nearValues == null || farValues == null)
        {
            resetSettings();
        }

        float baseViewAngle = renderCam.fieldOfView;
        
        if (softSliceMethod == renderMode.OCCLUDING) //this section is only relevant to occluding render style which renders the slices as a post process
        {                
            renderCam.targetTexture = occlusionRTT;

            if (nearValues == null)
                return;


            //x: near clip, y: far clip, z: overlap, w: depth curve
            renderCam.nearClipPlane = nearValues[0];
            renderCam.farClipPlane = farValues[slices - 1];
            Shader.SetGlobalVector("_NFOD", new Vector4(renderCam.nearClipPlane, renderCam.farClipPlane, overlap, 1f));               

            renderCam.Render();
        }
        else //normal rendering path with multiple slices
        {

            for (int i = 0; i < slices; i++)
            {
                //slice modifiers
                hypercube.sliceModifier m = hypercube.sliceModifier.getSliceModifier(i);
                if (m != null)
                {
                    renderCam.gameObject.SetActive(true); //has to be active, or post processes wont work.
                    slicePost.blend = m.blend;
                    slicePost.tex = m.tex;
                    slicePost.enabled = true;
                }
                else
                {
                    slicePost.enabled = false;
                    slicePost.blend = hypercube.slicePostProcess.blending.NONE;
                }
                    

                renderCam.fieldOfView = baseViewAngle + (i * forcedPerspective); //allow forced perspective or perspective correction

                renderCam.nearClipPlane = nearValues[i];
                renderCam.farClipPlane = farValues[i];
                renderCam.targetTexture = sliceTextures[i];
                renderCam.Render();
            }

            renderCam.fieldOfView = baseViewAngle;
            
        }

        if (overlap > 0f && softSliceMethod != renderMode.HARD)
        {
            if (softSliceMethod == renderMode.PER_MATERIAL)
                Shader.DisableKeyword("SOFT_SLICING");  //toggling this on/off allows the preview in the editor to continue to appear normal.            
        }
        
        renderCam.gameObject.SetActive(false);          
    }


    //NOTE that if a parent of the cube is scaled, and the cube is arbitrarily rotated inside of it, it will return wrong lossy scale.
    // see: http://docs.unity3d.com/ScriptReference/Transform-lossyScale.html
    //TODO change this to use a proper matrix to handle local scale in a hierarchy
    public void resetSettings()
    {

        nearValues = new float[sliceTextures.Length];
        farValues = new float[sliceTextures.Length];

        float sliceDepth = transform.lossyScale.z / (float)sliceTextures.Length;

        renderCam.aspect = transform.lossyScale.x / transform.lossyScale.y;
        renderCam.orthographicSize = .5f * transform.lossyScale.y;

        int sliceCount = sliceTextures.Length;
        if (sliceCount == 0)
            sliceCount = hypercube.castMesh.defaultSliceCount;

        for (int i = 0;  i < sliceTextures.Length; i++)
        {
            nearValues[i] = (i * sliceDepth) - (sliceDepth * overlap);
            farValues[i] = ((i + 1) * sliceDepth) + (sliceDepth * overlap);
        }


        updateOverlap();
    }

    void populateRTTs(int count, int resX, int resY)
    {
        if (resX == 0 || resY == 0) //probably initializing or something.
            return;
        
        if (sliceTextures == null || sliceTextures.Length != count || sliceTextures[0] == null || sliceTextures[0].width != resX || sliceTextures[0].height != resY)
        {
            List<RenderTexture> newTextures = new List<RenderTexture>();
            for (int i = 0; i < count; i++)
            {
                RenderTexture rtt = new RenderTexture(resX, resY, 16, RenderTextureFormat.ARGBFloat);
                rtt.wrapMode = TextureWrapMode.Clamp;
                rtt.filterMode = FilterMode.Trilinear;
                rtt.antiAliasing = 1;
                newTextures.Add(rtt);            
            }
            sliceTextures = newTextures.ToArray();
            resetSettings();
        }

        //give the occlusion RTT the same pixel density as the other methods
        occlusionRTT = new RenderTexture(resX, resY * count, 24, RenderTextureFormat.ARGBFloat);
        occlusionRTT.wrapMode = TextureWrapMode.Clamp;
        occlusionRTT.filterMode = FilterMode.Trilinear;
        occlusionRTT.antiAliasing = 1;
            
        //apply them to the castmesh if possible
        if (localCastMesh && count >= localCastMesh.canvasMaterials.Count)
        {           
            for (int i = 0; i < localCastMesh.canvasMaterials.Count; i++)
            {
                localCastMesh.canvasMaterials[i].mainTexture = sliceTextures[i];
            }
            localCastMesh.occlusionMaterial.mainTexture = occlusionRTT;
            localCastMesh.updateMesh();
        }

        //apply them to preview if possible
        if (preview && count != preview.previewMaterials.Count)
            preview.updateMaterials(this);


        System.GC.Collect();
    }




    //CUSTOM INSPECTOR STUFF ... this hides/shows the gui as needed
    /*  [UnityEditor.CustomEditor(typeof(hypercubeCamera))]
      public class MyScriptEditor : UnityEditor.Editor
      {
          public override void OnInspectorGUI()
          {
              hypercubeCamera c = target as hypercubeCamera;

              c.softSliceMethod = (hypercubeCamera.renderMode)UnityEditor.EditorGUILayout.EnumPopup("softSliceMethod", c.softSliceMethod);
              c.overlap = UnityEditor.EditorGUILayout.Slider("Overlap", c.overlap, .001f, 5f);

              if (c.softSliceMethod != renderMode.HARD)
              {
                  c.softness = UnityEditor.EditorGUILayout.Slider("Softness", c.softness, .001f, .5f);
                  c.autoSoftness = UnityEditor.EditorGUILayout.Toggle("Auto Softness", c.autoSoftness);
              }

  


          }
      }
      */
}