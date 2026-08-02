using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ayy.OutlineWithPostEffect
{
    public class UIBlitRenderFeature : ScriptableRendererFeature
    {
        public Material blitMaterial;
        public int passIndex = 5;

        private UIBlitRenderPass _pass;

        public override void Create()
        {
            _pass = new UIBlitRenderPass(blitMaterial, passIndex);
            _pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (blitMaterial == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;
            // Only run on cameras rendering to screen (not to RenderTexture)
            if (renderingData.cameraData.targetTexture != null) return;
            renderer.EnqueuePass(_pass);
        }

        public void SetSource(RenderTexture rt)
        {
            if (_pass != null)
                _pass.SetSource(rt);
        }
    }

    public class UIBlitRenderPass : ScriptableRenderPass
    {
        private Material _material;
        private int _passIndex;
        private RTHandle _sourceRT;

        private class PassData
        {
            public Material material;
            public int passIndex;
            public TextureHandle source;
        }

        public UIBlitRenderPass(Material material, int passIndex)
        {
            _material = material;
            _passIndex = passIndex;
            requiresIntermediateTexture = true;
        }

        public void SetSource(RenderTexture rt)
        {
            if (rt == null) { _sourceRT = null; return; }
            _sourceRT = RTHandles.Alloc(rt);
        }

        public void ReleaseSource()
        {
            if (_sourceRT != null)
            {
                _sourceRT.Release();
                _sourceRT = null;
            }
        }

        static void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            Blitter.BlitTexture(cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.passIndex);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_sourceRT == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraColor = resourceData.activeColorTexture;

            using (var builder = renderGraph.AddUnsafePass<PassData>("UI Blit To Screen", out var passData))
            {
                passData.material = _material;
                passData.passIndex = _passIndex;

                var uiTex = renderGraph.ImportTexture(_sourceRT);

                passData.source = uiTex;

                builder.UseTexture(uiTex, AccessFlags.Read);
                builder.UseTexture(cameraColor, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
