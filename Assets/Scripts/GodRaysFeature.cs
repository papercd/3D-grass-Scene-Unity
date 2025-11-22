using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

// Simplified RenderGraph version using Blitter utility
public class GodRaysFeatureSimple : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("God Rays Settings")]

        public Light mainLight;

        [Range(0.1f, 100f)]
        public float planeDistance = 20f;

        [Range(0.01f, 1f)]
        public float stepSize = 0.1f;

        [Range(4, 128)]
        public int maxSteps = 64;

        [Range(0f, 1f)]
        public float intensity = 0.5f;

        [Range(0f, 2f)]
        public float scatteringCoefficient = 1f;

        public Color tint = Color.white;

        [Header("Render Settings")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private GodRaysPass godRaysPass;
    private Material godRaysMaterial;

    public override void Create()
    {
        Shader shader = Shader.Find("Hidden/GodRays");
        if (shader == null)
        {
            Debug.LogError("GodRays shader not found!");
            return;
        }

        godRaysMaterial = CoreUtils.CreateEngineMaterial(shader);
        godRaysPass = new GodRaysPass(settings);
        godRaysPass.renderPassEvent = settings.renderPassEvent;
        godRaysPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (godRaysMaterial == null) return;

        if (settings.mainLight == null || settings.mainLight.type != LightType.Directional)
        {
            settings.mainLight = RenderSettings.sun;

            if (settings.mainLight == null)
            {
                // fallback: scan scene
                foreach (var light in GameObject.FindObjectsOfType<Light>())
                {
                    if (light.type == LightType.Directional)
                    {
                        settings.mainLight = light;
                        break;
                    }
                }
            }
        }
        /*
        if (settings.mainLight == null)
        {
            settings.mainLight = RenderSettings.sun;
        }*/

        godRaysPass.Setup(godRaysMaterial);
        renderer.EnqueuePass(godRaysPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(godRaysMaterial);
        godRaysPass?.Dispose();
    }

    class GodRaysPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;

        private static readonly int MainLightDirId = Shader.PropertyToID("_MainLightDir");
        private static readonly int PlaneDistanceId = Shader.PropertyToID("_PlaneDistance");
        private static readonly int StepSizeId = Shader.PropertyToID("_StepSize");
        private static readonly int MaxStepsId = Shader.PropertyToID("_MaxSteps");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int ScatteringId = Shader.PropertyToID("_ScatteringCoefficient");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        public GodRaysPass(Settings settings)
        {
            this.settings = settings;
            profilingSampler = new ProfilingSampler("God Rays");
        }

        public void Setup(Material material)
        {
            this.material = material;
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        private void UpdateMaterialProperties()
        {
            if (material == null || settings.mainLight == null)
                return;

            Vector3 lightDir = -settings.mainLight.transform.forward;
            material.SetVector(MainLightDirId, lightDir);
            material.SetFloat(PlaneDistanceId, settings.planeDistance);
            material.SetFloat(StepSizeId, settings.stepSize);
            material.SetInt(MaxStepsId, settings.maxSteps);
            material.SetFloat(IntensityId, settings.intensity);
            material.SetFloat(ScatteringId, settings.scatteringCoefficient);
            material.SetColor(TintId, settings.tint);
        }

        // RenderGraph path (Unity 2022.2+)
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null || settings.mainLight == null)
                return;

            // Update material properties
            UpdateMaterialProperties();

            // Get the current frame resources
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Check if we have a valid camera target
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;

            // Create a temporary texture for the blit
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_TempGodRaysTexture", false, FilterMode.Bilinear);

            // Use RenderGraph's utility to blit with material
            RenderGraphUtils.BlitMaterialParameters para = new RenderGraphUtils.BlitMaterialParameters(
                source, destination, material, 0);
            renderGraph.AddBlitPass(para, passName: "God Rays Blit");

            // Copy result back to camera color
            RenderGraphUtils.BlitMaterialParameters copyPara = new RenderGraphUtils.BlitMaterialParameters(
                destination, source, null, 0);
            renderGraph.AddBlitPass(copyPara, passName: "God Rays Copy");
        }

        // Fallback for compatibility mode (Render Graph disabled)
        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || settings.mainLight == null)
                return;

            UpdateMaterialProperties();

            CommandBuffer cmd = CommandBufferPool.Get("God Rays");

            // Get camera target
            var renderer = renderingData.cameraData.renderer;
            var source = renderer.cameraColorTargetHandle;

            // Get temporary RT
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            int tempId = Shader.PropertyToID("_TempGodRaysTexture");
            cmd.GetTemporaryRT(tempId, desc, FilterMode.Bilinear);

            // Blit
            Blit(cmd, source, tempId, material, 0);
            Blit(cmd, tempId, source);

            cmd.ReleaseTemporaryRT(tempId);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}