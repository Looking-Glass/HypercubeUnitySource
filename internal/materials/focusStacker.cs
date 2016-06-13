using UnityEngine;
using System.Collections;
using System;


public enum maskBlurType
{
    RANGE1 = 0,
    RANGE2,
    RANGE3,
    SOFT
}


//[ImageEffectOpaque]
[ExecuteInEditMode]
//[RequireComponent(typeof(Camera))]
[AddComponentMenu("")]
public class focusStacker : MonoBehaviour
{
    public float sampleRange = .002f; //how far to sample for contrast from the current pixel in UV space
   // public float sampleDifferenceMod = .4f;
    public float edgeSuppression = .97f;
    public float sampleBrightness = 1.5f;
    public float sampleContrast = 50f;
    public bool viewMask;

    public float maskStrength = 1f;
    public bool viewMaskProcessed;

    public maskBlurType maskType = maskBlurType.RANGE3;

    public Shader focusStackMaskShader;
    public Shader focusStackShader;
    public RenderTexture maskTarget;
    public RenderTexture outTarget;

    private Material m_maskMaterial;
    private Material m_stackMaterial;

    protected virtual void Start()
    {
        // Disable if we don't support image effects
        if (!SystemInfo.supportsImageEffects)
        {
            enabled = false;
            return;
        }

        // Disable the image effect if the shader can't
        // run on the users graphics card
        if (!focusStackShader || !focusStackShader.isSupported)
            enabled = false;
    }

    protected void applySettings()
    {
        maskMaterial.SetFloat("_SampleRange", sampleRange);
        maskMaterial.SetFloat("_Brightness", sampleBrightness);
        maskMaterial.SetFloat("_Contrast", sampleContrast);
        maskMaterial.SetFloat("_EdgeSuppression", 1 - edgeSuppression);

        stackMaterial.SetFloat("_PixelSizeX", sampleRange);
        stackMaterial.SetFloat("_PixelSizeY", sampleRange);
        stackMaterial.SetFloat("_ExpansionStrength", maskStrength);
        if (viewMaskProcessed)
            stackMaterial.EnableKeyword("SHOWMASK");
        else
            stackMaterial.DisableKeyword("SHOWMASK");

        if (maskType == maskBlurType.RANGE1)
        {
            stackMaterial.EnableKeyword("RANGE_1");
            stackMaterial.DisableKeyword("RANGE_2");
            stackMaterial.DisableKeyword("RANGE_3");
            stackMaterial.DisableKeyword("RANGE_SOFT");
        }
        else if (maskType == maskBlurType.RANGE2)
        {
            stackMaterial.EnableKeyword("RANGE_1");
            stackMaterial.EnableKeyword("RANGE_2");
            stackMaterial.DisableKeyword("RANGE_3");
            stackMaterial.DisableKeyword("RANGE_SOFT");
        }
        else if (maskType == maskBlurType.RANGE3)
        {
            stackMaterial.EnableKeyword("RANGE_1");
            stackMaterial.EnableKeyword("RANGE_2");
            stackMaterial.EnableKeyword("RANGE_3");
            stackMaterial.DisableKeyword("RANGE_SOFT");
        }
        else if (maskType == maskBlurType.SOFT)
        {
            stackMaterial.DisableKeyword("RANGE_1");
            stackMaterial.DisableKeyword("RANGE_2");
            stackMaterial.DisableKeyword("RANGE_3");
            stackMaterial.EnableKeyword("RANGE_SOFT");
        }


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
    protected Material stackMaterial
    {
        get
        {
            if (m_stackMaterial == null)
            {
                m_stackMaterial = new Material(focusStackShader);
                m_stackMaterial.hideFlags = HideFlags.HideAndDontSave;
                applySettings();
            }
            return m_stackMaterial;
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

    public void processFrame(Texture source)
    {
        if (!outTarget)
            return;
     
        if (viewMask)
        {
            Graphics.Blit(source, outTarget, maskMaterial);
        }
        else
        {
            stackMaterial.SetTexture("_OriginalTex",source);
            Graphics.Blit(source, maskTarget, maskMaterial);
            Graphics.Blit(maskTarget, outTarget, stackMaterial);
        }
    }
}


