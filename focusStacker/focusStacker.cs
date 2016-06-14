using UnityEngine;
using System.Collections;
using System;


public enum maskBlurType
{
    OFF = 0,
    EDGE_DETECTION,
    MASK,
    OUTPUT
}


//[ImageEffectOpaque]
[ExecuteInEditMode]
//[RequireComponent(typeof(Camera))]
[AddComponentMenu("")]
public class focusStacker : MonoBehaviour
{
    public int effectResolutionX = 1024;
    public int effectResolutionY = 1024;

    public float sampleRange = .002f; //how far to sample for contrast from the current pixel in UV space
   // public float sampleDifferenceMod = .4f;
    public float edgeSuppression = 9.7f;
    public float sampleBrightness = 1.5f;
    public float sampleContrast = 3f;

    public float maskStrength = 3.5f;

    public maskBlurType maskType = maskBlurType.OUTPUT;

    public Shader focusStackMaskShader;
    public Shader HBlurShader;
    public Shader VBlurShader;

    private Material m_maskMaterial; //1st pass, detect the edges with high contrast (what is in focus)
    private Material m_HBlurMaterial; //2nd pass, blur it out a bit to capture pixels adjacent to the high contrast ones
    private Material m_VBlurMaterial; //3rd pass, complete the blur and apply it as a mask to the original image

    RenderTexture maskTarget;
    RenderTexture blurTarget;

    protected virtual void Start()
    {
        // Disable if we don't support image effects
        if (!SystemInfo.supportsImageEffects)
        {
            enabled = false;
            return;
        }

        if (!focusStackMaskShader || !HBlurShader  || !VBlurShader)
        {
            enabled = false;
            Debug.LogError("A shader related to the focus stacking is missing!");
        }

        // Disable the image effect if the shader can't
        // run on the users graphics card
        if (!focusStackMaskShader.isSupported  || !HBlurShader.isSupported || !VBlurShader.isSupported)
        {
            enabled = false;
            Debug.LogError("A shader related to the focus stacking is not supported!");
        }
    }

    protected void applySettings()
    {
        maskMaterial.SetFloat("_SampleRange", sampleRange * .001f); //a good sample range is about .002 for a 1k image, but this modifier makes it feel saner
        maskMaterial.SetFloat("_Brightness", sampleBrightness);
        maskMaterial.SetFloat("_Contrast", sampleContrast);
        maskMaterial.SetFloat("_EdgeSuppression", 1 - (edgeSuppression * .1f)); //nice edge suppression values are between .98 and .99, so this helps them appear slightly saner

        HBlurMaterial.SetFloat("_PixelSizeX", 1f / (float)effectResolutionX);
        VBlurMaterial.SetFloat("_PixelSizeY", 1f / (float)effectResolutionY);
        HBlurMaterial.SetFloat("_ExpansionStrength", maskStrength * 100);
        VBlurMaterial.SetFloat("_ExpansionStrength", maskStrength * 100);

        if (maskType == maskBlurType.OFF)
            maskMaterial.EnableKeyword("DISABLE");
        else
            maskMaterial.DisableKeyword("DISABLE");


        if (maskType == maskBlurType.MASK)
            VBlurMaterial.EnableKeyword("SHOWBLUR");
        else
            VBlurMaterial.DisableKeyword("SHOWBLUR");
    }


    protected Material maskMaterial
    {
        get
        {
            if (m_maskMaterial == null)
            {
                m_maskMaterial = new Material(focusStackMaskShader);
                m_maskMaterial.hideFlags = HideFlags.HideAndDontSave;
                applySettings();
            }
            return m_maskMaterial;
        }
    }
    protected Material HBlurMaterial
    {
        get
        {
            if (m_HBlurMaterial == null)
            {
                m_HBlurMaterial = new Material(HBlurShader);
                m_HBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
                applySettings();
            }
            return m_HBlurMaterial;
        }
    }
    protected Material VBlurMaterial
    {
        get
        {
            if (m_VBlurMaterial == null)
            {
                m_VBlurMaterial = new Material(VBlurShader);
                m_VBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
                applySettings();
            }
            return m_VBlurMaterial;
        }
    }



    void OnValidate()
    {
        applySettings();
    }


    protected virtual void OnDestroy()
    {
        if (m_maskMaterial)
        {
            DestroyImmediate(m_maskMaterial);
        }
    }

    public void processFrame(Texture source, RenderTexture outTarget)
    {
        if (!source || !outTarget)
            return;

        //update the effect resolution to the source resolution if they don't match
        if (!blurTarget || blurTarget.width != source.width || blurTarget.height != blurTarget.height)
        {
            blurTarget = new RenderTexture(source.width, source.height, 32, RenderTextureFormat.ARGB32);
            maskTarget = new RenderTexture(source.width, source.height, 32, RenderTextureFormat.ARGB32);
        }


        if (maskType == maskBlurType.OFF || maskType == maskBlurType.EDGE_DETECTION)
        {
            Graphics.Blit(source, outTarget, maskMaterial);
        }
        else if (maskType == maskBlurType.MASK || maskType == maskBlurType.OUTPUT)
        {
            VBlurMaterial.SetTexture("_OriginalTex",source);

            Graphics.Blit(source, maskTarget, maskMaterial); //pass 1: edge detection to determine in-focus areas
            Graphics.Blit(maskTarget, blurTarget, HBlurMaterial);  //pass 2: blur the edges horizontally
            Graphics.Blit(blurTarget, outTarget, VBlurMaterial); //pass 3: blur the edges vertically and composite it with the original.
        }
    }
}


