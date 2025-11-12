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

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AuraAPI.URP
{
    /// <summary>
    /// URP Render Feature for Aura Volumetric Lighting
    /// </summary>
    public class VolumetricLightingFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// URP Render Pass for Aura Volumetric Lighting
        /// </summary>
        class VolumetricLightingPass : ScriptableRenderPass
        {
            private Material _compositeMaterial;
            private string _profilerTag = "Aura Volumetric Lighting";
            private Aura _auraInstance;

            public VolumetricLightingPass(Material compositeMaterial)
            {
                _compositeMaterial = compositeMaterial;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public void SetAura(Aura aura)
            {
                _auraInstance = aura;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // Request depth if the compute uses it later
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_auraInstance == null || !_auraInstance.isActiveAndEnabled)
                    return;

                if (_compositeMaterial == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

                // Let Aura prepare data and dispatch compute
                _auraInstance.InternalBeforeComposite(ref renderingData);
                _auraInstance.DispatchVolumetricCompute(cmd, ref renderingData);

                // Composite onto camera color target
                // The volumetric data is already set as a global texture (Aura_VolumetricLightingTexture)
                // by the Frustum.ComputeData() call, so we just need to blit with the composite shader
                cmd.BeginSample(_profilerTag);
                
                // Get camera color target handle
                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
                
                // Blit with composite material - the shader will sample from the global volumetric texture
                // We blit from the camera target to itself, which applies the volumetric fog
                Blit(cmd, cameraColorTarget, cameraColorTarget, _compositeMaterial, 0);
                
                cmd.EndSample(_profilerTag);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        /// <summary>
        /// Settings for the Volumetric Lighting Feature
        /// </summary>
        [System.Serializable]
        public class Settings
        {
            public Shader compositeShader;
        }

        public Settings settings = new Settings();
        private VolumetricLightingPass _pass;
        private Material _material;

        public override void Create()
        {
            if (settings.compositeShader == null)
                return;

            _material = CoreUtils.CreateEngineMaterial(settings.compositeShader);
            _pass = new VolumetricLightingPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null) 
                return;

            var aura = renderingData.cameraData.camera.GetComponent<Aura>();
            _pass.SetAura(aura);
            
            if (aura != null && aura.isActiveAndEnabled)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _material != null)
            {
                CoreUtils.Destroy(_material);
            }
        }
    }
}
