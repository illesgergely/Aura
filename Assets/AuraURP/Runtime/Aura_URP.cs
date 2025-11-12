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
        private bool _urpInitialized;
        #endregion

        #region URP Lifecycle Methods
        
        private void InitializeURP()
        {
            if (_urpInitialized)
                return;

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

            _urpInitialized = false;
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
        }

        /// <summary>
        /// Called by the URP render pass to dispatch volumetric compute shaders
        /// </summary>
        public void DispatchVolumetricCompute(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // The actual compute work is done in UpdateFrustrumURP -> frustum.ComputeData()
            // which dispatches the compute shaders and sets the global Aura_VolumetricDataTexture
            // This method is here for future extensions where we might want to do additional
            // compute work through the command buffer
        }

        #endregion
    }
}

#endif // UNITY_PIPELINE_URP
