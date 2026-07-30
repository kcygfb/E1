using System;
using UnityEngine;
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
    /// 协调出场面板 (TransitionExitPanel) 和入场面板 (TransitionEntrancePanel)。
    /// 出场和入场各自拥有独立的 Image、材质和代码。
    ///
    /// 流程: ExitPanel 覆盖屏幕 → 加载场景 → EntrancePanel 揭示新场景
    /// 用法: TransitionEffect.Instance.TransitionTo("Card");
    ///       TransitionEffect.Instance.TransitionTo("Card", new TransitionConfig { ... });
    /// </summary>
    public class TransitionEffect : MonoBehaviour
    {
        public static TransitionEffect Instance { get; private set; }

        [Header("面板引用")]
        [SerializeField] private TransitionExitPanel exitPanel;
        [SerializeField] private TransitionEntrancePanel entrancePanel;

        [Header("入场设置")]
        [SerializeField] private bool playEntranceAfterLoad = true;

        private Action _pendingEntranceComplete;

        public bool IsPlaying =>
            (exitPanel != null && exitPanel.IsPlaying) ||
            (entrancePanel != null && entrancePanel.IsPlaying);

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
                var cv = GetComponent<UnityEngine.Canvas>();
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

            // 初始化两个面板的运行时材质
            if (exitPanel != null) exitPanel.Init();
            if (entrancePanel != null) entrancePanel.Init();
        }

        private void OnDestroy()
        {
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
            if (exitPanel == null)
            {
                Debug.LogError("[TransitionEffect] exitPanel not assigned");
                return;
            }

            exitPanel.PlayExit(config, () =>
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

        /// <summary>只播放出场效果（用 Inspector 默认参数）。</summary>
        public void Play(Action onComplete = null)
        {
            Play(null, onComplete);
        }

        /// <summary>只播放出场效果（自定义参数）。</summary>
        public void Play(TransitionConfig config, Action onComplete = null)
        {
            if (exitPanel != null)
                exitPanel.PlayExit(config, onComplete);
        }

        /// <summary>播放入场动画：溶解揭示新场景。</summary>
        public void PlayEntrance(Action onComplete = null)
        {
            if (entrancePanel != null)
                entrancePanel.PlayEntrance(onComplete);
        }

        /// <summary>立即重置所有面板到转场前状态。</summary>
        public void Reset()
        {
            exitPanel?.Reset();
            entrancePanel?.Reset();
        }

        // ─── 入场动画 ───

        private void OnSceneLoadedEntrance(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedEntrance;
            if (this == null) return;

            System.Collections.IEnumerator DelayEntrance()
            {
                yield return null;

                // 先让入场面板接管覆盖，再隐藏出场面板（无缝衔接）
                if (entrancePanel != null)
                {
                    entrancePanel.SetFullCover();
                    exitPanel?.Reset();
                    entrancePanel.PlayEntrance(_pendingEntranceComplete);
                }
                else
                {
                    exitPanel?.Reset();
                }
            }
            StartCoroutine(DelayEntrance());
        }
    }
}
