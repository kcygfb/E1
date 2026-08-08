using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>Short, reusable feedback for player actions rejected by the current rules.</summary>
    public sealed class WarningToast : MonoBehaviour
    {
        private const string PrefabResourceName = "UI/\u63d0\u793a\u6846";
        private const float VisibleDuration = 1.5f;
        private const float FadeDuration = 0.3f;

        private static WarningToast _instance;

        private TMP_Text _messageText;
        private CanvasGroup _canvasGroup;
        private Coroutine _hideRoutine;
        private bool _initialized;

        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var toast = EnsureInstance();
            if (toast != null)
                toast.Present(message);
        }

        private static WarningToast EnsureInstance()
        {
            if (_instance != null)
                return _instance;

            var canvas = FindDisplayCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[WarningToast] No active screen-space canvas was found.");
                return null;
            }

            var prefab = Resources.Load<GameObject>(PrefabResourceName);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[WarningToast] Missing Resources/" + PrefabResourceName + ".prefab. " +
                    "The warning toast cannot be displayed.");
                return null;
            }

            var toastObject = Instantiate(prefab, canvas.transform, false);
            toastObject.name = nameof(WarningToast);
            _instance = toastObject.GetComponent<WarningToast>();
            if (_instance == null)
                _instance = toastObject.AddComponent<WarningToast>();

            _instance.Initialize();
            toastObject.SetActive(false);
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
        }

        private void OnDisable()
        {
            _hideRoutine = null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            _messageText = GetComponentInChildren<TMP_Text>(true);
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void Present(string message)
        {
            Initialize();
            if (_messageText == null)
            {
                Debug.LogWarning("[WarningToast] The prefab has no TMP text component.", this);
                return;
            }

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _messageText.text = message;
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(VisibleDuration);

            for (var elapsed = 0f; elapsed < FadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = 1f - elapsed / FadeDuration;
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
