using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    [Serializable]
    public class TransitionConfig
    {
        public Sprite centerSprite;
        public Color? centerColor;
        public float? centerScale;
        public Texture2D beforeTexture;
        public Texture2D afterTexture;
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
    /// 单例持有自己的面板，场景加载时从新场景的 Teans2 提取材质资产引用。
    /// 材质更新全部延迟到 OnSceneLoadedEntrance / TransitionTo，不在 Awake 阶段碰单例面板。
    /// </summary>
    public class TransitionEffect : MonoBehaviour
    {
        public static TransitionEffect Instance { get; private set; }

        public static bool IsEntrancePlaying { get; private set; }

        public static System.Collections.IEnumerator WaitEntrance()
        {
            while (IsEntrancePlaying)
                yield return null;
        }

        [Header("面板引用")]
        [SerializeField] private TransitionExitPanel exitPanel;
        [SerializeField] private TransitionEntrancePanel entrancePanel;

        [Header("入场设置")]
        [SerializeField] private bool playEntranceAfterLoad = true;

        private Action _pendingEntranceComplete;

        // 暂存的源材质资产（跨场景存活，不依赖场景内 GameObject）
        private Material _pendingExitSourceMat;
        private Sprite _pendingExitCenterSprite;
        private Material _pendingEntranceSourceMat;

        public bool IsPlaying =>
            (exitPanel != null && exitPanel.IsPlaying) ||
            (entrancePanel != null && entrancePanel.IsPlaying);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // 新场景加载：只暂存源材质引用，不碰单例面板
                Instance.CaptureSourceMaterials(exitPanel, entrancePanel);
                Destroy(gameObject);
                return;
            }
            Instance = this;

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

            if (exitPanel != null) exitPanel.Init();
            if (entrancePanel != null) entrancePanel.Init();
        }

        /// <summary>从新场景的面板提取源材质引用。只存储，不应用。</summary>
        private void CaptureSourceMaterials(TransitionExitPanel sceneExit, TransitionEntrancePanel sceneEntrance)
        {
            if (sceneExit != null)
            {
                _pendingExitSourceMat = sceneExit.SourceMaterial;
                _pendingExitCenterSprite = sceneExit.CenterImage?.sprite;
            }
            if (sceneEntrance != null)
            {
                _pendingEntranceSourceMat = sceneEntrance.SourceMaterial;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TransitionTo(string sceneName, Action onComplete = null)
        {
            TransitionTo(sceneName, null, onComplete);
        }

        public void TransitionTo(string sceneName, TransitionConfig config, Action onComplete = null)
        {
            if (exitPanel == null)
            {
                Debug.LogError("[TransitionEffect] exitPanel not assigned");
                return;
            }

            // 应用上一个场景暂存的 exit 材质（供本次出场使用）
            if (_pendingExitSourceMat != null)
            {
                exitPanel.CopyMaterialFrom(_pendingExitSourceMat);
                _pendingExitSourceMat = null;
            }
            if (_pendingExitCenterSprite != null && exitPanel.CenterImage != null)
            {
                exitPanel.CenterImage.sprite = _pendingExitCenterSprite;
                _pendingExitCenterSprite = null;
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

        public void Play(Action onComplete = null)
        {
            Play(null, onComplete);
        }

        public void Play(TransitionConfig config, Action onComplete = null)
        {
            if (exitPanel == null) return;

            if (_pendingExitSourceMat != null)
            {
                exitPanel.CopyMaterialFrom(_pendingExitSourceMat);
                _pendingExitSourceMat = null;
            }
            if (_pendingExitCenterSprite != null && exitPanel.CenterImage != null)
            {
                exitPanel.CenterImage.sprite = _pendingExitCenterSprite;
                _pendingExitCenterSprite = null;
            }

            exitPanel.PlayExit(config, onComplete);
        }

        public void PlayEntrance(Action onComplete = null)
        {
            if (entrancePanel != null)
                entrancePanel.PlayEntrance(onComplete);
        }

        public void Reset()
        {
            exitPanel?.Reset();
            entrancePanel?.Reset();
        }

        private static List<AudioSource> _pausedAudio = new();

        private void OnSceneLoadedEntrance(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoadedEntrance;
            if (this == null) return;

            System.Collections.IEnumerator DelayEntrance()
            {
                yield return null;

                if (entrancePanel != null)
                {
                    // 不应用新场景的 entrance 材质！
                    // 入场动画用的是出发场景的材质（Init 时已设置好）
                    // 新场景的 entrance 材质等入场动画播完后才应用，供下次转场使用

                    entrancePanel.SetFullCover();
                    exitPanel?.Reset();
                    IsEntrancePlaying = true;
                    Time.timeScale = 0;

                    _pausedAudio.Clear();
                    foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                    {
                        if (src.isPlaying)
                        {
                            _pausedAudio.Add(src);
                            src.Pause();
                        }
                    }

                    entrancePanel.PlayEntrance(() =>
                    {
                        IsEntrancePlaying = false;
                        Time.timeScale = 1;
                        foreach (var src in _pausedAudio)
                        {
                            if (src != null) src.UnPause();
                        }
                        _pausedAudio.Clear();
                        _pendingEntranceComplete?.Invoke();

                        // 入场动画播完后，应用新场景的 entrance 材质，供下次转场使用
                        if (_pendingEntranceSourceMat != null)
                        {
                            entrancePanel.CopyMaterialFrom(_pendingEntranceSourceMat);
                            _pendingEntranceSourceMat = null;
                        }
                    });
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
