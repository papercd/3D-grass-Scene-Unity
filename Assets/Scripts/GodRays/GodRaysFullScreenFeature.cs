using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util; // Added this using for convenience

public class GodRaysRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        // Copy material is optional, but needed for Pass 2 if not using a simple blit
        public Material copyMaterial;
        public Material godRayMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public Settings settings = new Settings();
    private GodRaysPass godRaysPass;

    public override void Create()
    {
        godRaysPass = new GodRaysPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.godRayMaterial == null)
            return;

        godRaysPass.Setup();
        renderer.EnqueuePass(godRaysPass);
    }

    protected override void Dispose(bool disposing)
    {
        godRaysPass?.Dispose();
    }

    class GodRaysPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        private const string passName = "God Rays Pass";

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;
            internal Material material;
            internal TextureHandle depthTexture; // Added handle for the depth texture
        }

        public GodRaysPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;

            // CORRECT: We configure the pass to require the depth texture as an input
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup()
        {
            material = settings.godRayMaterial;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Empty - using RecordRenderGraph instead (This is correct for RG compliance)
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
                return;

            // Get camera and resource data
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // Get the required handles
            TextureHandle sourceTexture = resourceData.activeColorTexture;
            // FIX: Retrieve the depth texture handle from the resources
            TextureHandle depthTexture = resourceData.activeDepthTexture;

            // Create descriptor for temporary texture
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            // Create temporary render texture
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                "_GodRaysTemp",
                false,
                FilterMode.Bilinear
            );

            // --- Pass 1: Render God Rays (Source -> Temp) ---
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // Set up pass data
                passData.source = sourceTexture;
                passData.destination = tempTexture;
                passData.material = material;
                passData.depthTexture = depthTexture; // FIX: Assign the depth texture handle

                // Declare texture usage
                builder.UseTexture(passData.source, AccessFlags.Read);
                // FIX: Declare reading the Depth Texture
                builder.UseTexture(passData.depthTexture, AccessFlags.Read);

                // Set render target
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true); // Needed for sampling depth

                // Set the render function
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture("_CameraDepthTexture", data.depthTexture);
                    // Use Blitter to draw full-screen with our material
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), // Scale and offset
                        data.material, 0); 	     // Material and pass index
                });
            }

            // --- Pass 2: Copy result back to camera target (Temp -> Source) ---
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("God Rays Copy Back", out var passData))
            {
                passData.source = tempTexture;
                passData.destination = resourceData.activeColorTexture;
                passData.material = settings.copyMaterial; // Use copyMaterial for clarity

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Simple copy blit using the dedicated copy material
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0),
                        data.material, 0);
                });
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}