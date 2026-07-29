using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 转场配置：每次播放时可以覆盖默认值。
    /// 不传则用 Inspector 上的默认值。
    /// </summary>
    [Serializable]
    public class TransitionConfig
    {
        /// <summary>替换中间图片，null 则保持原图片</summary>
        public Sprite centerSprite;
        /// <summary>中间图片颜色（含 alpha 基色）</summary>
        public Color? centerColor;
        /// <summary>中间图片缩放</summary>
        public float? centerScale;
        /// <summary>替换 Trans_Before 贴图</summary>
        public Texture2D beforeTexture;
        /// <summary>替换 Trans_After 贴图</summary>
        public Texture2D afterTexture;
        /// <summary>材质整体染色（shader 的 _Color）</summary>
        public Color? tintColor;
        public float? alphaDuration;
        public float? centerFadeInDuration;
        public float? sliderDuration;
        public float? sliderTarget;
        public float? endHoldDuration;
        public Ease? sliderEase;
        public Ease? alphaEase;
    }

    /// <summary>
    /// 全局转场效果控制器（DontDestroyOnLoad 单例）。
    /// 流程: Trans_After_Alpha 0→1 → 中间图片出现 → Slider 0→target 曲线推进 → 完成
    /// 用法: TransitionEffect.Instance.TransitionTo("Card");
    ///       TransitionEffect.Instance.TransitionTo("Card", new TransitionConfig { ... });
    /// </summary>
    public class TransitionEffect : MonoBehaviour
    {
        public static TransitionEffect Instance { get; private set; }

        [Header("Shader 目标")]
        [SerializeField] private Image transitionImage;
        [SerializeField] private Material transitionMaterial;

        [Header("中间图片")]
        [SerializeField] private Image centerImage;
        [SerializeField] private float centerImageScale = 1f;

        [Header("时间设置")]
        [SerializeField] private float alphaDuration = 0.5f;
        [SerializeField] private float centerFadeInDuration = 0.3f;
        [SerializeField] private float sliderDuration = 1.5f;
        [SerializeField] private float sliderTarget = 2f;
        [SerializeField] private float endHoldDuration = 0.2f;

        [Header("曲线")]
        [SerializeField] private Ease sliderEase = Ease.InOutCubic;
        [SerializeField] private Ease alphaEase = Ease.OutQuad;

        [Header("入场设置")]
        [SerializeField] private bool playEntranceAfterLoad = true;
        [SerializeField] private float entranceSliderDuration = 1.2f;
        [SerializeField] private float entranceAlphaDuration = 0.4f;
        [SerializeField] private Ease entranceSliderEase = Ease.InOutCubic;
        [SerializeField] private Ease entranceAlphaEase = Ease.OutQuad;

        private Sequence _sequence;
        private Material _runtimeMat;
        private Action _pendingEntranceComplete;

        public bool IsPlaying => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // 脱离父级，成为 root GameObject（DontDestroyOnLoad 要求）
            // 注意：SetParent(null) 会导致 Canvas 自动切到 WorldSpace，需要记录并在之后恢复
            if (transform.parent != null)
            {
                var cv = GetComponent<Canvas>();
                var savedRenderMode = cv != null ? cv.renderMode : RenderMode.ScreenSpaceOverlay;
                var savedSortingOrder = cv != null ? cv.sortingOrder : 100;
                transform.SetParent(null);
                if (cv != null)
                {
                    cv.renderMode = savedRenderMode;
                    cv.sortingOrder = savedSortingOrder;
                    cv.overrideSorting = true;
                }
            }
            DontDestroyOnLoad(gameObject);

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

        private void OnDestroy()
        {
            _sequence?.Kill();
            if (_runtimeMat != null) Destroy(_runtimeMat);
            if (Instance == this) Instance = null;
        }

        /// <summary>转场并加载场景（用 Inspector 默认参数）。</summary>
        public void TransitionTo(string sceneName, Action onComplete = null)
        {
            TransitionTo(sceneName, null, onComplete);
        }

        /// <summary>转场并加载场景（自定义参数）。出场动画完成后加载场景，场景加载后自动播放入场动画。</summary>
        public void TransitionTo(string sceneName, TransitionConfig config, Action onComplete = null)
        {
            Play(config, () =>
            {
                onComplete?.Invoke();
                if (playEntranceAfterLoad)
                {
                    _pendingEntranceComplete = null;
                    SceneManager.sceneLoaded += OnSceneLoadedEntrance;
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    SceneManager.LoadScene(sceneName);
                }
            });
        }

        /// <summary>只播放转场效果（用 Inspector 默认参数）。</summary>
        public void Play(Action onComplete = null)
        {
            Play(null, onComplete);
        }

        /// <summary>只播放转场效果（自定义参数）。</summary>
        public void Play(TransitionConfig config, Action onComplete = null)
        {
            _sequence?.Kill();

            var mat = _runtimeMat;
            if (mat == null) { Debug.LogError("[TransitionEffect] material not found"); return; }

            // 解析配置，null 则用默认值
            var cfgAlphaDur = config?.alphaDuration ?? alphaDuration;
            var cfgCenterDur = config?.centerFadeInDuration ?? centerFadeInDuration;
            var cfgSliderDur = config?.sliderDuration ?? sliderDuration;
            var cfgSliderTarget = config?.sliderTarget ?? sliderTarget;
            var cfgHoldDur = config?.endHoldDuration ?? endHoldDuration;
            var cfgSliderEase = config?.sliderEase ?? sliderEase;
            var cfgAlphaEase = config?.alphaEase ?? alphaEase;
            var cfgCenterScale = config?.centerScale ?? centerImageScale;

            // 应用贴图/颜色覆盖
            if (config != null)
            {
                if (config.beforeTexture != null)
                    mat.SetTexture("_Trans_Before", config.beforeTexture);
                if (config.afterTexture != null)
                    mat.SetTexture("_Trans_After", config.afterTexture);
                if (config.tintColor.HasValue)
                    mat.SetColor("_Color", config.tintColor.Value);
            }

            // 重置 shader 参数
            mat.SetFloat("_Trans_After_Alpha", 0f);
            mat.SetFloat("_Slider", 0f);

            // 准备中间图片
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

        /// <summary>立即重置到转场前状态。</summary>
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
        }

        // ─── 入场动画 ───

        private void OnSceneLoadedEntrance(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedEntrance;
            if (this == null) return;
            // 延迟一帧确保新场景 UI 初始化完成
            System.Collections.IEnumerator DelayEntrance()
            {
                yield return null;
                PlayEntrance(_pendingEntranceComplete);
            }
            StartCoroutine(DelayEntrance());
        }

        /// <summary>播放入场动画：从遮罩状态反向溶解，揭示新场景。</summary>
        public void PlayEntrance(Action onComplete = null)
        {
            _sequence?.Kill();

            var mat = _runtimeMat;
            if (mat == null) { Debug.LogError("[TransitionEffect] material not found for entrance"); return; }

            // 当前处于出场结束状态: alpha=1, slider=target
            var startSlider = mat.GetFloat("_Slider");
            var startAlpha = mat.GetFloat("_Trans_After_Alpha");

            _sequence = DOTween.Sequence();

            // Phase 1: 反向 slider (target→0)
            var slider = startSlider;
            _sequence.Append(DOTween.To(
                () => slider, v => { slider = v; mat.SetFloat("_Slider", v); },
                0f, entranceSliderDuration).SetEase(entranceSliderEase));

            // Phase 2: 中间图片消失 (和 slider 并行)
            if (centerImage != null && centerImage.gameObject.activeSelf)
            {
                _sequence.Join(centerImage.DOFade(0f, entranceAlphaDuration));
                _sequence.Join(centerImage.transform.DOScale(0f, entranceAlphaDuration).SetEase(Ease.InBack));
            }

            // Phase 3: alpha 1→0 (揭示新场景)
            var alpha = startAlpha;
            _sequence.Append(DOTween.To(
                () => alpha, v => { alpha = v; mat.SetFloat("_Trans_After_Alpha", v); },
                0f, entranceAlphaDuration).SetEase(entranceAlphaEase));

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                mat.SetFloat("_Trans_After_Alpha", 0f);
                mat.SetFloat("_Slider", 0f);
                if (centerImage != null) centerImage.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}