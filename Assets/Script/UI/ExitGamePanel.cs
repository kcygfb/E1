using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 退出游戏确认弹窗。
    /// <para>
    /// 用法：场景中任意名为 "Settings" 的按钮会自动绑定为打开弹窗（场景加载后自动绑定，
    /// 也支持手动调用 <see cref="BindSettingsButtons"/> 或 <see cref="Show"/>）。
    /// 弹窗来自 Resources/UI/退出界面.prefab，其中：
    /// "退出按钮" → 退出游戏；"X" → 关闭弹窗。
    /// </para>
    /// </summary>
    public sealed class ExitGamePanel : MonoBehaviour
    {
        private const string PrefabResourceName = "UI/\u9000\u51fa\u754c\u9762"; // UI/退出界面
        private const string SettingsButtonName = "Settings";
        private const string QuitButtonName = "\u9000\u51fa\u6309\u94ae"; // 退出按钮
        private const string CloseButtonName = "X";
        private const float PopInDuration = 0.2f;

        private static ExitGamePanel _instance;

        private Button _quitButton;
        private Button _closeButton;
        private CanvasGroup _canvasGroup;
        private Tween _popTween;
        private bool _initialized;

        /// <summary>
        /// 每次场景加载后自动把场景中所有名为 "Settings" 的按钮绑定为打开弹窗。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBindSettingsButtons()
        {
            BindSettingsButtons();
        }

        /// <summary>把当前场景中所有名为 "Settings" 的 Button 绑定为打开弹窗。可重复调用，不会重复绑定。</summary>
        public static void BindSettingsButtons()
        {
            var buttons = Object.FindObjectsByType<Button>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (var button in buttons)
            {
                if (button == null || button.name != SettingsButtonName)
                    continue;

                button.onClick.RemoveListener(Show);
                button.onClick.AddListener(Show);
            }
        }

        /// <summary>打开退出弹窗。任何场景的 Settings 按钮都会调用这里。</summary>
        public static void Show()
        {
            var panel = EnsureInstance();
            if (panel != null)
                panel.Present();
        }

        /// <summary>关闭退出弹窗。</summary>
        public static void Hide()
        {
            if (_instance != null)
                _instance.Dismiss();
        }

        private static ExitGamePanel EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var canvas = FindDisplayCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[ExitGamePanel] 未找到活动的 Screen Space Canvas，无法显示退出界面。");
                return null;
            }

            var prefab = Resources.Load<GameObject>(PrefabResourceName);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[ExitGamePanel] 缺少 Resources/" + PrefabResourceName + ".prefab，退出界面无法显示。");
                return null;
            }

            var panelObject = Instantiate(prefab, canvas.transform, false);
            panelObject.name = "\u9000\u51FA\u754C\u9762(\u8FD0\u884C\u65F6)"; // 退出界面(运行时)

            _instance = panelObject.GetComponent<ExitGamePanel>();
            if (_instance == null)
                _instance = panelObject.AddComponent<ExitGamePanel>();

            _instance.Initialize();
            panelObject.SetActive(false);

            // 补一次绑定，避免场景加载时序导致个别按钮漏绑
            BindSettingsButtons();
            return _instance;
        }

        private static Canvas FindDisplayCanvas()
        {
            Canvas overlayCanvas = null;
            Canvas cameraCanvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (var canvas in canvases)
            {
                if (!canvas.isActiveAndEnabled || !canvas.isRootCanvas)
                    continue;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    if (overlayCanvas == null || canvas.sortingOrder >= overlayCanvas.sortingOrder)
                        overlayCanvas = canvas;
                }
                else if (canvas.renderMode == RenderMode.ScreenSpaceCamera &&
                         (cameraCanvas == null || canvas.sortingOrder >= cameraCanvas.sortingOrder))
                {
                    cameraCanvas = canvas;
                }
            }

            return overlayCanvas ?? cameraCanvas;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
                _popTween = null;
            }

            if (_instance == this)
                _instance = null;
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name == QuitButtonName)
                {
                    _quitButton = button;
                    _quitButton.onClick.RemoveListener(QuitGame);
                    _quitButton.onClick.AddListener(QuitGame);
                }
                else if (button.name == CloseButtonName)
                {
                    _closeButton = button;
                    _closeButton.onClick.RemoveListener(Hide);
                    _closeButton.onClick.AddListener(Hide);
                }
            }

            if (_quitButton == null)
                Debug.LogWarning("[ExitGamePanel] prefab 中未找到名为 \u201c\u9000\u51FA\u6309\u94ae\u201d 的 Button。", this);
            if (_closeButton == null)
                Debug.LogWarning("[ExitGamePanel] prefab 中未找到名为 \u201cX\u201d 的 Button。", this);
        }

        private void Present()
        {
            Initialize();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // 弹出动画：淡入 + 轻微放大（不受暂停影响，用 unscaled time）
            if (_popTween != null)
                _popTween.Kill();

            _canvasGroup.alpha = 0f;
            var targetScale = transform.localScale;
            transform.localScale = targetScale * 0.92f;

            _popTween = DOTween.Sequence()
                .Join(_canvasGroup.DOFade(1f, PopInDuration))
                .Join(transform.DOScale(targetScale, PopInDuration))
                .SetUpdate(true)
                .SetEase(Ease.OutBack);
        }

        private void Dismiss()
        {
            if (_popTween != null)
            {
                _popTween.Kill();
                _popTween = null;
            }

            gameObject.SetActive(false);
        }

        private static void QuitGame()
        {
            Debug.Log("[ExitGamePanel] 收到退出请求。");

#if UNITY_EDITOR
            // 编辑器内 Application.Quit() 不生效，通过反射停止 Play Mode 以便测试。
            // 使用反射是为了不引入 UnityEditor 程序集依赖（KiKs.UI 是运行时程序集）。
            var editorApplicationType = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            if (editorApplicationType != null)
            {
                var isPlayingProperty = editorApplicationType.GetProperty(
                    "isPlaying",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (isPlayingProperty != null)
                {
                    isPlayingProperty.SetValue(null, false);
                    Debug.Log("[ExitGamePanel] 已停止 Play Mode（编辑器内测试，打包后由 Application.Quit 退出）。");
                    return;
                }
            }

            Debug.LogWarning("[ExitGamePanel] 无法自动停止 Play Mode，请手动停止播放。");
#else
            Application.Quit();
#endif
        }
    }
}
