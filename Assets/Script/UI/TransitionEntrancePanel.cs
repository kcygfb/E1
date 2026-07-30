using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 入场面板：独立管理入场动画（溶解揭示新场景）。
    /// 拥有自己的 Image、材质实例和动画参数。
    /// slider 0→2 与出场同向（BL→TR），通过 Before=遮罩/After=透明实现揭示效果。
    /// </summary>
    public class TransitionEntrancePanel : MonoBehaviour
    {
        [Header("Shader 目标")]
        [SerializeField] private Image transitionImage;
        [SerializeField] private Material transitionMaterial;

        [Header("入场时间设置")]
        [SerializeField] private float sliderDuration = 1.2f;
        [SerializeField] private float alphaDuration = 0.4f;
        [SerializeField] private float sliderTarget = 2f;

        [Header("入场曲线")]
        [SerializeField] private Ease sliderEase = Ease.InOutCubic;
        [SerializeField] private Ease alphaEase = Ease.OutQuad;

        private Sequence _sequence;
        private Material _runtimeMat;

        public bool IsPlaying => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();

        /// <summary>初始化运行时材质实例。由 TransitionEffect 在 Awake 中调用。</summary>
        public void Init()
        {
            var sourceMat = transitionMaterial != null ? transitionMaterial : transitionImage?.material;
            if (sourceMat != null)
            {
                _runtimeMat = new Material(sourceMat);
                _runtimeMat.SetFloat("_Trans_After_Alpha", 0f);
                _runtimeMat.SetFloat("_Slider", 0f);
                if (transitionImage != null)
                    transitionImage.material = _runtimeMat;
            }

        }

        /// <summary>设置为完全覆盖状态（入场动画开始前调用）。</summary>
        public void SetFullCover()
        {
            _sequence?.Kill();
            _sequence = null;

            var mat = _runtimeMat;
            if (mat != null)
            {
                mat.SetFloat("_Trans_After_Alpha", 1f);
                mat.SetFloat("_Slider", 0f);
            }
        }

        /// <summary>播放入场动画：从覆盖状态溶解揭示新场景。</summary>
        public void PlayEntrance(Action onComplete = null)
        {
            _sequence?.Kill();

            var mat = _runtimeMat;
            if (mat == null) { Debug.LogError("[TransitionEntrancePanel] material not found for entrance"); return; }

            var startSlider = mat.GetFloat("_Slider");
            var startAlpha = mat.GetFloat("_Trans_After_Alpha");

            _sequence = DOTween.Sequence().SetUpdate(true);

            // Phase 1: slider 0→target（与出场同向，BL→TR 溶解）
            var slider = startSlider;
            _sequence.Append(DOTween.To(
                () => slider, v => { slider = v; mat.SetFloat("_Slider", v); },
                sliderTarget, sliderDuration).SetEase(sliderEase));

            // Phase 2: alpha 1→0 (揭示新场景)
            var alpha = startAlpha;
            _sequence.Append(DOTween.To(
                () => alpha, v => { alpha = v; mat.SetFloat("_Trans_After_Alpha", v); },
                0f, alphaDuration).SetEase(alphaEase));

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                mat.SetFloat("_Trans_After_Alpha", 0f);
                mat.SetFloat("_Slider", 0f);
                onComplete?.Invoke();
            });
        }

        /// <summary>立即重置到初始状态。</summary>
        public void Reset()
        {
            _sequence?.Kill();
            _sequence = null;
            var mat = _runtimeMat;
            if (mat != null)
            {
                mat.SetFloat("_Trans_After_Alpha", 0f);
                mat.SetFloat("_Slider", 0f);
            }
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            if (_runtimeMat != null) Destroy(_runtimeMat);
        }
    }
}
