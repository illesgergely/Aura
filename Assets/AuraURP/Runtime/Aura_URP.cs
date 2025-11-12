///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///                                                                                                                                                             ///
///     MIT License                                                                                                                                             ///
///                                                                                                                                                             ///
///     Copyright (c) 2016 Raphaël Ernaelsten (@RaphErnaelsten)                                                                                                 ///
///                                                                                                                                                             ///
///     Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),      ///
///     to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,                  ///
///     and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:              ///
///                                                                                                                                                             ///
///     The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.                          ///
///                                                                                                                                                             ///
///     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,     ///
///     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER      ///
///     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS    ///
///     IN THE SOFTWARE.                                                                                                                                        ///
///                                                                                                                                                             ///
///     PLEASE CONSIDER CREDITING AURA IN YOUR PROJECTS. IF RELEVANT, USE THE UNMODIFIED LOGO PROVIDED IN THE "LICENSE" FOLDER.                                 ///
///                                                                                                                                                             ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if UNITY_PIPELINE_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AuraAPI
{
    /// <summary>
    /// URP-specific functionality for Aura volumetric lighting
    /// </summary>
    public partial class Aura : MonoBehaviour
    {
        #region URP Private Members
        private RenderTexture _lightingVolumeComposite;
        private bool _urpInitialized;
        #endregion

        #region URP Lifecycle Methods
        
        private void InitializeURP()
        {
            if (_urpInitialized)
                return;

            AllocateCompositeTarget();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRenderingURP;
            RenderPipelineManager.endCameraRendering += OnEndCameraRenderingURP;
            
            _urpInitialized = true;
        }

        private void DisposeURP()
        {
            if (!_urpInitialized)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRenderingURP;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRenderingURP;

            if (_lightingVolumeComposite != null)
            {
                _lightingVolumeComposite.Release();
                _lightingVolumeComposite = null;
            }

            _urpInitialized = false;
        }

        private void AllocateCompositeTarget()
        {
            Camera cam = GetComponent<Camera>();
            int width = Mathf.Max(1, cam.pixelWidth);
            int height = Mathf.Max(1, cam.pixelHeight);

            if (_lightingVolumeComposite != null)
            {
                if (_lightingVolumeComposite.width == width && _lightingVolumeComposite.height == height)
                    return;

                _lightingVolumeComposite.Release();
            }

            _lightingVolumeComposite = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _lightingVolumeComposite.Create();
        }

        private void OnBeginCameraRenderingURP(ScriptableRenderContext context, Camera camera)
        {
            if (camera != GetComponent<Camera>())
                return;

            // Update frustum at the beginning of camera rendering
            UpdateFrustrumURP();
        }

        private void OnEndCameraRenderingURP(ScriptableRenderContext context, Camera camera)
        {
            if (camera != GetComponent<Camera>())
                return;

            // Increment frame counter
            ++Aura.FrameId;
        }

        private void UpdateFrustrumURP()
        {
            if (frustum != null)
            {
                frustum.ComputeData();
            }
        }

        #endregion

        #region URP Public Methods Called by Render Pass

        /// <summary>
        /// Called by the URP render pass before compositing
        /// Prepares per-frame data for volumetric lighting
        /// </summary>
        public void InternalBeforeComposite(ref RenderingData renderingData)
        {
            // Update lights manager
            if (Aura.LightsManager != null)
            {
                Aura.LightsManager.Update();
            }

            // Set global frame ID
            Shader.SetGlobalInt("_frameID", Aura.FrameId);

            // Ensure composite target is allocated and correct size
            AllocateCompositeTarget();
        }

        /// <summary>
        /// Called by the URP render pass to dispatch volumetric compute shaders
        /// </summary>
        public void DispatchVolumetricCompute(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (computeDataComputeShader == null || _lightingVolumeComposite == null)
                return;

            // For MVP: simple dispatch to write volumetric data to the composite texture
            // This is a placeholder that the existing Frustum.ComputeData() will handle
            // through its internal compute shader dispatches

            // The actual compute work is done in UpdateFrustrumURP -> frustum.ComputeData()
            // Here we just ensure the output texture is set for the post-process accumulation
            
            // Clear the composite target
            cmd.SetRenderTarget(_lightingVolumeComposite);
            cmd.ClearRenderTarget(false, true, Color.clear);
        }

        /// <summary>
        /// Gets the composite texture containing the volumetric lighting result
        /// </summary>
        public RenderTexture GetCompositeTexture()
        {
            return _lightingVolumeComposite;
        }

        #endregion
    }
}

#endif // UNITY_PIPELINE_URP
