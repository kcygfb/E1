using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// Keeps every runtime UI label containing Chinese characters on the shared ZCOOL font.
    /// English-only and numeric labels retain their authored fonts.
    /// </summary>
    internal sealed class ChineseFontRuntime : MonoBehaviour
    {
        private const float RescanInterval = 0.5f;

        private TMP_FontAsset _tmpFont;
        private Font _legacyFont;
        private float _nextRescanTime;
        private readonly HashSet<TMP_Text> _pendingTmpTexts = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _tmpFont = ChineseFontProvider.TmpFont;
            _legacyFont = ChineseFontProvider.LegacyFont;

            if (_tmpFont != null)
            {
                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null)
                {
                    fallbacks = new List<TMP_FontAsset>();
                    TMP_Settings.fallbackFontAssets = fallbacks;
                }

                if (!fallbacks.Contains(_tmpFont))
                    fallbacks.Add(_tmpFont);
            }

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ApplyToLoadedText();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRescanTime)
                return;

            _nextRescanTime = Time.unscaledTime + RescanInterval;
            ApplyToLoadedText();
        }

        private void LateUpdate()
        {
            if (_pendingTmpTexts.Count == 0)
                return;

            foreach (var text in _pendingTmpTexts)
                Apply(text);

            _pendingTmpTexts.Clear();
        }

        private void OnDestroy()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyToLoadedText();
        }

        private void HandleTextChanged(Object changedObject)
        {
            // Changing a TMP font from inside TEXT_CHANGED can leave the current mesh using
            // UVs from the old atlas with the new font material. Apply after TMP has finished
            // its update instead, which is especially important for dynamic Chinese glyphs.
            if (changedObject is TMP_Text text &&
                _tmpFont != null &&
                text.font != _tmpFont &&
                ContainsChinese(text.text))
            {
                _pendingTmpTexts.Add(text);
            }
        }

        private void ApplyToLoadedText()
        {
            if (_tmpFont != null)
            {
                foreach (var text in FindObjectsByType<TMP_Text>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    Apply(text);
                }
            }

            if (_legacyFont != null)
            {
                foreach (var text in FindObjectsByType<Text>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (text.font != _legacyFont && ContainsChinese(text.text))
                        text.font = _legacyFont;
                }
            }
        }

        private void Apply(TMP_Text text)
        {
            if (_tmpFont != null && text != null && text.font != _tmpFont && ContainsChinese(text.text))
                text.font = _tmpFont;
        }

        private static bool ContainsChinese(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var character in value)
            {
                if (character is >= '\u3400' and <= '\u4DBF' or
                    >= '\u4E00' and <= '\u9FFF' or
                    >= '\uF900' and <= '\uFAFF')
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class ChineseFontRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<ChineseFontRuntime>() != null)
                return;

            var runtimeObject = new GameObject(nameof(ChineseFontRuntime));
            runtimeObject.hideFlags = HideFlags.HideAndDontSave;
            runtimeObject.AddComponent<ChineseFontRuntime>();
        }
    }
}
