using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using System.Collections.Generic;

namespace Seanzo.PostProcess.Effects
{
    public class SeanzoPostProcessFeature : ScriptableRendererFeature
    {
        private SeanzoPostProcessPass postChainPass;

        public override void Create()
        {
            postChainPass = new SeanzoPostProcessPass();
            postChainPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(postChainPass);
        }

        protected override void Dispose(bool disposing)
        {
            postChainPass?.Cleanup();
        }
    }

    public class SeanzoPostProcessPass : ScriptableRenderPass
    {
        private Dictionary<string, Material> materials = new();

        private static readonly string[] ShaderNames = new[] {
            "Hidden/Workbench/DepthDependent",
            "Hidden/Workbench/Color",
            "Hidden/Workbench/Analog",
            "Hidden/Workbench/Painterly",
            "Hidden/Workbench/Retro",
            "Hidden/Workbench/Distortion",
            "Hidden/Workbench/Framing"
        };

        public SeanzoPostProcessPass()
        {
            EnsureMaterials();
        }

        private void EnsureMaterials()
        {
            foreach (string shaderName in ShaderNames)
            {
                if (materials.TryGetValue(shaderName, out Material existing) && existing != null)
                    continue;

                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                    materials[shaderName] = new Material(shader);
            }
        }

        private Material GetMaterial(string shaderName)
        {
            if (materials.TryGetValue(shaderName, out Material mat) && mat != null)
                return mat;

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                return null;

            mat = new Material(shader);
            materials[shaderName] = mat;
            return mat;
        }

        public void Cleanup()
        {
            foreach (Material mat in materials.Values)
            {
                if (mat != null)
                    CoreUtils.Destroy(mat);
            }
            materials.Clear();
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle currentSource = resourceData.activeColorTexture;

            var desc = new TextureDesc(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height)
            {
                name = "SeanzoPostProcessTemp",
                clearBuffer = false,
                depthBufferBits = 0,
                colorFormat = cameraData.cameraTargetDescriptor.graphicsFormat
            };

            bool anyEffectExecuted = false;

            // Helper for running a pass using AddBlitPass (the proper RenderGraph way)
            void RunPass<T>(string shaderName, int passIndex, System.Action<Material, T> setupParams)
                where T : VolumeComponent, IPostProcessComponent
            {
                var component = stack.GetComponent<T>();
                if (component == null || !component.IsActive()) return;

                Material mat = GetMaterial(shaderName);
                if (mat == null) return;

                // Set material parameters before creating the blit pass
                setupParams(mat, component);

                TextureHandle dest = renderGraph.CreateTexture(desc);

                // Use AddBlitPass - the recommended way to blit with custom materials in RenderGraph
                // This automatically handles _BlitTexture binding and fullscreen triangle rendering
                var blitParams = new RenderGraphUtils.BlitMaterialParameters(currentSource, dest, mat, passIndex);
                renderGraph.AddBlitPass(blitParams, $"SeanzoPostProcess_{typeof(T).Name}");

                currentSource = dest;
                anyEffectExecuted = true;
            }

            // --- EXECUTION ORDER ---

            // Depth Dependent
            RunPass<Fog>("Hidden/Workbench/DepthDependent", 0, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.density.value);
                mat.SetFloat("_FloatParam2", c.startDistance.value);
                mat.SetFloat("_FloatParam3", c.endDistance.value);
                mat.SetColor("_VectorParam1", c.color.value);
            });

            RunPass<EdgeDetection>("Hidden/Workbench/DepthDependent", 1, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.threshold.value);
                mat.SetFloat("_FloatParam2", c.thickness.value);
                mat.SetFloat("_FloatParam3", c.intensity.value);
                mat.SetColor("_VectorParam1", c.edgeColor.value);
            });

            // Color
            RunPass<Posterize>("Hidden/Workbench/Color", 0, (mat, c) => mat.SetFloat("_FloatParam1", c.levels.value));

            // Analog
            RunPass<Scanlines>("Hidden/Workbench/Analog", 0, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.intensity.value);
                mat.SetFloat("_FloatParam2", c.frequency.value);
                mat.SetFloat("_FloatParam3", c.thickness.value);
                mat.SetFloat("_FloatParam4", c.speed.value);
            });
            RunPass<TrackingDistortion>("Hidden/Workbench/Analog", 1, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.intensity.value);
                mat.SetFloat("_FloatParam2", c.speed.value);
                mat.SetFloat("_FloatParam3", c.bandHeight.value);
            });
            RunPass<ColorBleed>("Hidden/Workbench/Analog", 2, (mat, c) => mat.SetFloat("_FloatParam1", c.amount.value));
            RunPass<StaticNoise>("Hidden/Workbench/Analog", 3, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.intensity.value);
                mat.SetFloat("_FloatParam2", c.blockSize.value);
                mat.SetInt("_IntParam1", c.colored.value ? 1 : 0);
            });

            // Painterly
            RunPass<Kuwahara>("Hidden/Workbench/Painterly", 0, (mat, c) => mat.SetFloat("_FloatParam1", (float)c.radius.value));
            RunPass<OilPaint>("Hidden/Workbench/Painterly", 1, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.strokeSize.value);
                mat.SetFloat("_FloatParam2", c.smoothness.value);
                mat.SetFloat("_FloatParam3", c.directionWeight.value);
                mat.SetFloat("_FloatParam4", c.sharpness.value);
            });
            RunPass<Watercolor>("Hidden/Workbench/Painterly", 2, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.bleedRadius.value);
                mat.SetFloat("_FloatParam2", c.edgeThreshold.value);
                mat.SetFloat("_FloatParam3", c.saturationBoost.value);
                mat.SetFloat("_FloatParam4", c.paperTexture.value);
                mat.SetVector("_VectorParam1", new Vector4(c.wetEdge.value, c.granulation.value, 0f, 0f));
            });

            // Retro
            RunPass<Halftone>("Hidden/Workbench/Retro", 0, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.dotSize.value);
                mat.SetFloat("_FloatParam2", c.angle.value);
                mat.SetFloat("_FloatParam3", c.intensity.value);
            });
            RunPass<Dithering>("Hidden/Workbench/Retro", 1, (mat, c) => mat.SetFloat("_FloatParam1", c.levels.value));
            RunPass<Pixelate>("Hidden/Workbench/Retro", 3, (mat, c) => mat.SetFloat("_FloatParam1", (float)c.blockSize.value));

            // Distortion
            RunPass<WaveDistortion>("Hidden/Workbench/Distortion", 0, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.amplitude.value);
                mat.SetFloat("_FloatParam2", c.frequency.value);
                mat.SetFloat("_FloatParam3", c.speed.value);
            });
            RunPass<GlitchBlock>("Hidden/Workbench/Distortion", 1, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.intensity.value);
                mat.SetFloat("_FloatParam2", c.blockSize.value);
                mat.SetFloat("_FloatParam3", c.shiftAmount.value);
            });

            // Framing
            RunPass<Letterbox>("Hidden/Workbench/Framing", 0, (mat, c) => mat.SetFloat("_FloatParam1", c.aspect.value));
            RunPass<Border>("Hidden/Workbench/Framing", 1, (mat, c) => {
                mat.SetFloat("_FloatParam1", c.width.value);
                mat.SetColor("_VectorParam1", c.color.value);
            });

            // Final Blit
            // Only blit if we actually processed something (currentSource is a temp texture)
            // OR if we need to copy back to camera target.
            if (anyEffectExecuted)
            {
                RecordFinalBlit(renderGraph, currentSource, resourceData.activeColorTexture);
            }
        }

        private void RecordFinalBlit(RenderGraph renderGraph, TextureHandle source, TextureHandle dest)
        {
            // Simple copy blit back to the camera target
            var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, dest, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(blitParams, "SeanzoPostProcess_FinalBlit");
        }
    }
}
