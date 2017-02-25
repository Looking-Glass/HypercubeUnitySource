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