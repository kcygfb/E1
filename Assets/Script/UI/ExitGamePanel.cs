using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiKs.UI
{
    public enum ExitConfirmationAction
    {
        QuitApplication,
        ReturnToMainMenu
    }

    /// <summary>
    /// 退出游戏确认弹窗。
    /// <para>
    /// 三种方式呼出：场景中名为 "Settings" 或 "🍎" 的 Button 点击、按 ESC 键。
    /// 按钮绑定在场景加载后自动完成，也可手动调用 <see cref="BindSettingsButtons"/>；
    /// ESC 由 <see cref="ExitGamePanelInput"/> 组件监听（存在即生效，且始终只会有一个实例）。
    /// 弹窗来自 Resources/UI/退出界面.prefab，其中：
    /// "退出按钮" → 主界面退出程序，其他场景重置本局并返回主界面；"X" → 关闭弹窗。
    /// </para>
    /// </summary>
    public sealed class ExitGamePanel : MonoBehaviour
    {
        private const string PrefabResourceName = "UI/\u9000\u51fa\u754c\u9762"; // UI/退出界面
        private const string QuitButtonName = "\u9000\u51fa\u6309\u94ae"; // 退出按钮
        private const string CloseButtonName = "X";
        private const float PopInDuration = 0.2f;

        public const string MainMenuSceneName = "MainMenu";

        /// <summary>
        /// 非主界面确认退出时发出的语义请求。UI 层不直接依赖或调用游戏状态仓库。
        /// </summary>
        public static event Action ReturnToMainMenuRequested;

        /// <summary>点击可呼出退出弹窗的场景按钮名集合（设置按钮与主界面苹果按钮）。</summary>
        private static readonly string[] TriggerButtonNames = { "Settings", "\U0001F34E" }; // 🍎

        private static ExitGamePanel _instance;

        private Button _quitButton;
        private Button _closeButton;
        private CanvasGroup _canvasGroup;
        private Tween _popTween;
        private bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            ReturnToMainMenuRequested = null;
            ExitGamePanelInput.ResetStaticState();
        }

        /// <summary>
        /// 每次场景加载后自动绑定场景中的触发按钮（"Settings"/"🍎"），并启用 ESC 键监听。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBindSettingsButtons()
        {
            BindSettingsButtons();
            ExitGamePanelInput.EnsureExists();
        }

        /// <summary>把当前场景中所有可触发的 Button（"Settings"/"🍎"）绑定为打开弹窗。可重复调用，不会重复绑定。</summary>
        public static void BindSettingsButtons()
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (var button in buttons)
            {
                if (button == null || !IsTriggerButton(button.name))
                    continue;

                button.onClick.RemoveListener(Show);
                button.onClick.AddListener(Show);
            }
        }

        /// <summary>判断场景物体名是否为退出弹窗的触发按钮。</summary>
        private static bool IsTriggerButton(string objectName)
        {
            for (var i = 0; i < TriggerButtonNames.Length; i++)
            {
                if (objectName == TriggerButtonNames[i])
                    return true;
            }
            return false;
        }

        /// <summary>打开退出弹窗。任何场景的触发按钮或 ESC 都会调用这里。</summary>
        public static void Show()
        {
            ExitGamePanelInput.EnsureExists();

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

        /// <summary>退出弹窗当前是否处于打开状态。</summary>
        public static bool IsVisible =>
            _instance != null && _instance.gameObject.activeSelf;

        /// <summary>切换退出弹窗的显示状态（ESC 键使用）。</summary>
        public static void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
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
            ExitGamePanelInput.EnsureExists();
            return _instance;
        }

        private static Canvas FindDisplayCanvas()
        {
            Canvas overlayCanvas = null;
            Canvas cameraCanvas = null;
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
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
            ExitGamePanelInput.EnsureExists();
        }

        private void Start()
        {
            // Awake 可能发生在场景切换的销毁阶段，Start 时（新场景正常运行）再保底一次
            ExitGamePanelInput.EnsureExists();
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

            // 注意：不能在这里调用 ExitGamePanelInput.EnsureExists()。
            // OnDestroy 处于场景销毁阶段，此时 new GameObject 会残留并报
            // "Some objects were not cleaned up when closing the scene"。
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
                    _quitButton.onClick.RemoveListener(ConfirmExit);
                    _quitButton.onClick.AddListener(ConfirmExit);
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
            ExitGamePanelInput.EnsureExists();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_quitButton != null)
                _quitButton.interactable = true;

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

        public static ExitConfirmationAction GetConfirmationAction(string sceneName)
        {
            return string.Equals(sceneName, MainMenuSceneName, StringComparison.Ordinal)
                ? ExitConfirmationAction.QuitApplication
                : ExitConfirmationAction.ReturnToMainMenu;
        }

        private static void ConfirmExit()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (GetConfirmationAction(sceneName) == ExitConfirmationAction.QuitApplication)
            {
                QuitApplication();
                return;
            }

            var handler = ReturnToMainMenuRequested;
            if (handler == null)
            {
                Debug.LogError("[ExitGamePanel] 没有注册返回主界面的游戏会话处理器，已取消退出请求。");
                return;
            }

            if (_instance != null && _instance._quitButton != null)
                _instance._quitButton.interactable = false;
            Hide();
            handler.Invoke();
        }

        private static void QuitApplication()
        {
            Debug.Log("[ExitGamePanel] 主界面确认退出游戏。");

#if UNITY_EDITOR
            // 编辑器内 Application.Quit() 不生效，通过反射停止 Play Mode 以便测试。
            // 使用反射是为了不引入 UnityEditor 程序集依赖（KiKs.UI 是运行时程序集）。
            var editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
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

        /// <summary>
        /// 全局监听 ESC 键呼出/关闭退出弹窗的组件。
        /// 任何场景中存在一个该组件（或直接调用 <see cref="EnsureExists"/>）即可生效，
        /// 重复实例会自动销毁，保证全局只有一个监听者。不跨场景常驻，随场景切换重新挂载即可。
        /// </summary>
        [DisallowMultipleComponent]
        private sealed class ExitGamePanelInput : MonoBehaviour
        {
            private static ExitGamePanelInput _active;

            public static void ResetStaticState()
            {
                _active = null;
            }

            /// <summary>确保当前场景中存在一个监听者（首次触发自动创建，幂等）。</summary>
            public static void EnsureExists()
            {
                if (_active != null)
                    return;

                var existing = FindAnyObjectByType<ExitGamePanelInput>();
                if (existing != null)
                {
                    _active = existing;
                    return;
                }

                var host = new GameObject(nameof(ExitGamePanelInput));
                _active = host.AddComponent<ExitGamePanelInput>();
            }

            private void Awake()
            {
                if (_active != null && _active != this)
                {
                    Destroy(gameObject);
                    return;
                }

                _active = this;
            }

            private void OnDestroy()
            {
                if (_active == this)
                    _active = null;
            }

            private void Update()
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    Toggle();
            }
        }
    }
}
