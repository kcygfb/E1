using UnityEngine;
using UnityEngine.InputSystem;

namespace KiKs.Audio
{
    [DisallowMultipleComponent]
    [AddComponentMenu("KiKs/Audio/全局鼠标左键音效")]
    public sealed class GlobalMouseClickAudio : MonoBehaviour
    {
        private const string CueResourcePath = "Audio/GlobalMouseLeftClick";

        [Tooltip("自动从 Resources/Audio/GlobalMouseLeftClick 加载，无需场景注册。")]
        [SerializeField] private AudioCue clickCue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnGlobalManager()
        {
            _ = AudioManager.MasterVolume;

            var manager = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (manager != null && manager.GetComponent<GlobalMouseClickAudio>() == null)
                manager.gameObject.AddComponent<GlobalMouseClickAudio>();
        }

        private void Awake()
        {
            if (clickCue == null)
                clickCue = Resources.Load<AudioCue>(CueResourcePath);

            if (clickCue == null)
            {
                Debug.LogError(
                    $"[{nameof(GlobalMouseClickAudio)}] 找不到 Resources/{CueResourcePath}，全局鼠标左键音效已停用。",
                    this);
                enabled = false;
                return;
            }

            AudioManager.Preload(clickCue);
        }

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                AudioManager.TryPlay(clickCue);
        }
    }
}
