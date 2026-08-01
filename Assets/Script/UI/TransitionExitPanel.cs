using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 出场面板：独立管理出场动画（溶解覆盖屏幕）。
    /// 拥有自己的 Image、材质实例和动画参数。
    /// </summary>
    public class TransitionExitPanel : MonoBehaviour
    {
        [Header("Shader 目标")]
        [SerializeField] private Image transitionImage;
        [SerializeField] private Material transitionMaterial;
        internal Material SourceMaterial => transitionMaterial != null ? transitionMaterial : transitionImage?.material;

        [Header("中间图片")]
        [SerializeField] private Image centerImage;
        [SerializeField] private float centerImageScale = 1f;
        /// <summary>供 TransitionEffect.SyncFromScenePanels 读取</summary>
        internal Image CenterImage => centerImage;

        [Header("时间设置")]
        [SerializeField] private float alphaDuration = 0.5f;
        [SerializeField] private float centerFadeInDuration = 0.3f;
        [SerializeField] private float sliderDuration = 1.5f;
        [SerializeField] private float sliderTarget = 2f;
        [SerializeField] private float endHoldDuration = 0.2f;

        [Header("曲线")]
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
                {
                    transitionImage.material = _runtimeMat;
                    var c = transitionImage.color; c.a = 0f; transitionImage.color = c;
                }
            }
        }

        /// <summary>从源材质创建新 runtime 材质并应用到 Image。</summary>
        public void CopyMaterialFrom(Material srcMat)
        {
            if (srcMat == null || transitionImage == null) return;

            transitionImage.enabled = false;

            var oldMat = _runtimeMat;
            _runtimeMat = new Material(srcMat);
            transitionImage.material = _runtimeMat;

            if (oldMat != null) Destroy(oldMat);
        }

        /// <summary>播放出场动画（用 Inspector 默认参数）。</summary>
        public void PlayExit(Action onComplete = null)
        {
            PlayExit(null, onComplete);
        }

        /// <summary>播放出场动画（自定义参数）。</summary>
        public void PlayExit(TransitionConfig config, Action onComplete = null)
        {
            _sequence?.Kill();

            var mat = _runtimeMat;
            if (mat == null) { Debug.LogError("[TransitionExitPanel] material not found"); return; }

            var cfgAlphaDur = config?.alphaDuration ?? alphaDuration;
            var cfgCenterDur = config?.centerFadeInDuration ?? centerFadeInDuration;
            var cfgSliderDur = config?.sliderDuration ?? sliderDuration;
            var cfgSliderTarget = config?.sliderTarget ?? sliderTarget;
            var cfgHoldDur = config?.endHoldDuration ?? endHoldDuration;
            var cfgSliderEase = config?.sliderEase ?? sliderEase;
            var cfgAlphaEase = config?.alphaEase ?? alphaEase;
            var cfgCenterScale = config?.centerScale ?? centerImageScale;

            if (config != null)
            {
                if (config.beforeTexture != null)
                    mat.SetTexture("_Trans_Before", config.beforeTexture);
                if (config.afterTexture != null)
                    mat.SetTexture("_Trans_After", config.afterTexture);
                if (config.tintColor.HasValue)
                    mat.SetColor("_Color", config.tintColor.Value);
            }

            mat.SetFloat("_Trans_After_Alpha", 0f);
            mat.SetFloat("_Slider", 0f);

            if (transitionImage != null)
            {
                transitionImage.enabled = true;
                var c = transitionImage.color; c.a = 1f; transitionImage.color = c;
            }

            if (centerImage != null)
            {
                if (config?.centerSprite != null)
                    centerImage.sprite = config.centerSprite;
                if (config?.centerColor.HasValue == true)
                    centerImage.color = new Color(config.centerColor.Value.r, config.centerColor.Value.g, config.centerColor.Value.b, 0f);
                else
                {
                    var c = centerImage.color; c.a = 0f; centerImage.color = c;
                }
                centerImage.gameObject.SetActive(false);
                centerImage.transform.localScale = Vector3.zero;
            }

            _sequence = DOTween.Sequence();

            // Phase 1: Trans_After_Alpha 0→1
            var alpha = 0f;
            _sequence.Append(DOTween.To(
                () => alpha, v => { alpha = v; mat.SetFloat("_Trans_After_Alpha", v); },
                1f, cfgAlphaDur).SetEase(cfgAlphaEase));

            // Phase 2: 中间图片出现
            if (centerImage != null)
            {
                _sequence.AppendCallback(() => centerImage.gameObject.SetActive(true));
                _sequence.Append(centerImage.DOFade(1f, cfgCenterDur));
                _sequence.Join(centerImage.transform.DOScale(cfgCenterScale, cfgCenterDur).SetEase(Ease.OutBack));
            }

            // Phase 3: Slider 0→target
            var slider = 0f;
            _sequence.Append(DOTween.To(
                () => slider, v => { slider = v; mat.SetFloat("_Slider", v); },
                cfgSliderTarget, cfgSliderDur).SetEase(cfgSliderEase));

            // Phase 4: 保持
            _sequence.AppendInterval(cfgHoldDur);

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                onComplete?.Invoke();
            });
        }

        /// <summary>立即重置到出场前状态。</summary>
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
            if (centerImage != null)
            {
                centerImage.gameObject.SetActive(false);
                var c = centerImage.color; c.a = 0f; centerImage.color = c;
                centerImage.transform.localScale = Vector3.zero;
            }
            if (transitionImage != null)
            {
                var ic = transitionImage.color; ic.a = 0f; transitionImage.color = ic;
            }
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            if (_runtimeMat != null) Destroy(_runtimeMat);
        }
    }
}
