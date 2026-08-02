using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ayy.OutlineWithPostEffect
{
    [RequireComponent(typeof(Canvas))]
    public class UIOutlineEffect : MonoBehaviour
    {
        [Header("References")]
        public Camera uiCamera;
        public Material outlineMaterial;

        [Header("Settings")]
        [Range(0, 10)] public int blurTimes = 3;
        public Vector2Int renderTextureSize = new Vector2Int(1920, 1080);

        private RenderTexture _uiRT;
        private RenderTexture _maskRT;
        private RenderTexture _tempRT1;
        private RenderTexture _tempRT2;
        private RenderTexture _displayRT;

        private static readonly int kUIOutlineMask = Shader.PropertyToID("_UIOutlineMask");

        private void Start()
        {
            int w = renderTextureSize.x;
            int h = renderTextureSize.y;

            _uiRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            _uiRT.Create();

            _maskRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            _maskRT.Create();

            _tempRT1 = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            _tempRT1.Create();

            _tempRT2 = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            _tempRT2.Create();

            _displayRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear };
            _displayRT.Create();

            if (uiCamera != null)
            {
                uiCamera.targetTexture = _uiRT;
                uiCamera.cullingMask = 1 << 5; // UI layer
                uiCamera.clearFlags = CameraClearFlags.SolidColor;
                uiCamera.backgroundColor = Color.black;
            }
        }

        private void LateUpdate()
        {
            if (outlineMaterial == null || _uiRT == null) return;
            ProcessOutline();

            // Push displayRT to the UIBlitRenderFeature
            var feature = FindUIBlitFeature();
            if (feature != null)
                feature.SetSource(_displayRT);
        }

        private UIBlitRenderFeature FindUIBlitFeature()
        {
            var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
            if (urpAsset == null) return null;
            foreach (var r in urpAsset.rendererDataList)
            {
                if (r is UnityEngine.Rendering.Universal.UniversalRendererData urd)
                {
                    foreach (var f in urd.rendererFeatures)
                    {
                        if (f is UIBlitRenderFeature bf)
                            return bf;
                    }
                }
            }
            return null;
        }

        private void ProcessOutline()
        {
            Graphics.Blit(_uiRT, _maskRT, outlineMaterial, 0);
            Graphics.Blit(_maskRT, _tempRT2, outlineMaterial, 1);
            for (int i = 0; i < blurTimes; i++)
            {
                Graphics.Blit(_tempRT2, _tempRT1, outlineMaterial, 2);
                Graphics.Blit(_tempRT1, _tempRT2, outlineMaterial, 3);
            }
            Shader.SetGlobalTexture(kUIOutlineMask, _tempRT2);
            Graphics.Blit(_uiRT, _displayRT, outlineMaterial, 4);
        }

        private void OnDestroy()
        {
            ReleaseRT(_uiRT);
            ReleaseRT(_maskRT);
            ReleaseRT(_tempRT1);
            ReleaseRT(_tempRT2);
            ReleaseRT(_displayRT);

            if (uiCamera != null)
                uiCamera.targetTexture = null;
        }

        private void ReleaseRT(RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                DestroyImmediate(rt);
            }
        }
    }
}
