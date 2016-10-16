using UnityEngine;
using System.Collections;
using System;

namespace hypercube
{


    [ImageEffectOpaque]
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("")]
    public class softOverlap : MonoBehaviour
    {
        /// Provides a shader property that is set in the inspector
        /// and a material instantiated from the shader
        public hypercubeCamera cam;
        public Shader softSliceShader;
        public Shader softSliceOccludeShader;

        private Material m_Material;


        protected virtual void Start()
        {
            // Disable if we don't support image effects
            if (!SystemInfo.supportsImageEffects)
            {
                Destroy(this);
                return;
            }

            // Disable the image effect if the shader can't
            // run on the users graphics card
            if (softSliceShader && !softSliceShader.isSupported)
                softSliceShader = null;

            if (softSliceOccludeShader && !softSliceOccludeShader.isSupported)
                softSliceOccludeShader = null;
        }


        protected Material material
        {
            get
            {
                if (m_Material == null)
                {
                    if (cam.softSliceMethod == hypercubeCamera.renderMode.OCCLUDING && softSliceOccludeShader)
                        m_Material = new Material(softSliceOccludeShader);
                    else if (softSliceShader)
                        m_Material = new Material(softSliceShader);
                    else
                    {
                        Destroy(this);
                        return null;
                    }

                    m_Material.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    if (cam.softSliceMethod == hypercubeCamera.renderMode.OCCLUDING && softSliceOccludeShader)
                        m_Material.shader = softSliceOccludeShader;
                    else if (softSliceShader)
                        m_Material.shader = softSliceShader;
                    else
                        return null;
                }


                return m_Material;
            }
        }


        protected virtual void OnDestroy()
        {
            if (m_Material)
            {
                DestroyImmediate(m_Material);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {

            if (cam.overlap > 0f)
                Graphics.Blit(source, destination, material);
        }
    }

}