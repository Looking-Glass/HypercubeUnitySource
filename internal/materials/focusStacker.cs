using UnityEngine;
using System.Collections;
using System;

//[ImageEffectOpaque]
[ExecuteInEditMode]
//[RequireComponent(typeof(Camera))]
[AddComponentMenu("")]
public class focusStacker : MonoBehaviour
{
    public float sampleRange = .002f; //how far to sample for contrast from the current pixel in UV space
    public float sampleDifferenceMod = .4f;
    public float sampleOffset = 1.5f;
    public float sampleContrast = 50f;
    
    public Shader focusStackShader;
    public RenderTexture target;

    private Material m_Material;


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


    protected Material material
    {
        get
        {
            if (m_Material == null)
            {
                m_Material = new Material(focusStackShader);
                m_Material.hideFlags = HideFlags.HideAndDontSave;
                m_Material.SetFloat("_SampleRange", sampleRange);
                m_Material.SetFloat("_SampleContrastEffect", sampleDifferenceMod);
                m_Material.SetFloat("_Brightness", sampleOffset);
                m_Material.SetFloat("_Contrast", sampleContrast);
            }
            return m_Material;
        }
    }

    void OnValidate()
    {
        material.SetFloat("_SampleRange", sampleRange);
        m_Material.SetFloat("_SampleContrastEffect", sampleDifferenceMod);
        m_Material.SetFloat("_Brightness", sampleOffset);
        m_Material.SetFloat("_Contrast", sampleContrast);
    }


    protected virtual void OnDestroy()
    {
        if (m_Material)
        {
            DestroyImmediate(m_Material);
        }
    }

    public void processFrame(Texture source)
    {
        if (!target)
            return;

        Graphics.Blit(source, target, material);
    }
}


