using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Audio
{
    /// <summary>
    /// Persistent, pooled SFX service. It is created lazily when first used, so scene authors only
    /// register AudioCue assets in explicit Inspector fields. A configured instance may still be
    /// placed in the bootstrap scene to override pool sizes and initial volume.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "KiKs.Audio.MasterVolume";
        private const string SfxVolumeKey = "KiKs.Audio.SfxVolume";
        private const string UiVolumeKey = "KiKs.Audio.UiVolume";

        private sealed class Voice
        {
            public AudioSource Source;
            public AudioCue Cue;
            public AudioBus Bus;
            public float BaseVolume;
            public float StartedAt;
            public bool Active;
        }

        private static AudioManager _instance;
        private readonly List<Voice> _voices = new List<Voice>();
        private readonly Dictionary<AudioCue, float> _lastPlayedAt = new Dictionary<AudioCue, float>();
        private readonly HashSet<AudioClip> _reportedClipLoadFailures = new HashSet<AudioClip>();
        private readonly HashSet<AudioCue> _reportedInvalidCues = new HashSet<AudioCue>();

        [Header("Pool")]
        [Min(1)] [SerializeField] private int initialVoices = 12;
        [Min(1)] [SerializeField] private int maxVoices = 32;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float defaultMasterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float defaultSfxVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float defaultUiVolume = 1f;

        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _uiVolume = 1f;

        public static bool HasInstance => _instance != null;
        public static float MasterVolume => EnsureInstance()._masterVolume;
        public static float SfxVolume => EnsureInstance()._sfxVolume;
        public static float UiVolume => EnsureInstance()._uiVolume;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            maxVoices = Mathf.Max(1, maxVoices);
            initialVoices = Mathf.Clamp(initialVoices, 1, maxVoices);

            _masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
            _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
            _uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, defaultUiVolume);

            while (_voices.Count < initialVoices)
                CreateVoice();
        }

        private void Update()
        {
            for (var i = 0; i < _voices.Count; i++)
            {
                var voice = _voices[i];
                if (!voice.Active || voice.Source.isPlaying) continue;
                ReleaseVoice(voice);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        public static bool TryPlay(AudioCue cue, float volumeScale = 1f)
        {
            return TryPlayAtPosition(cue, Vector3.zero, volumeScale, false);
        }

        public static bool TryPlayAtPosition(AudioCue cue, Vector3 position, float volumeScale = 1f)
        {
            return TryPlayAtPosition(cue, position, volumeScale, true);
        }

        /// <summary>
        /// Loads every valid clip referenced by a cue. TryPlay performs the same safety check,
        /// while explicit preloading removes first-play latency for timing-sensitive sounds.
        /// </summary>
        public static bool Preload(AudioCue cue)
        {
            return cue != null && EnsureInstance().PreloadInternal(cue);
        }

        private static bool TryPlayAtPosition(AudioCue cue, Vector3 position, float volumeScale, bool usePosition)
        {
            if (cue == null) return false;
            return EnsureInstance().PlayInternal(cue, position, Mathf.Max(0f, volumeScale), usePosition);
        }

        public static void SetMasterVolume(float value, bool save = true)
        {
            var manager = EnsureInstance();
            manager._masterVolume = Mathf.Clamp01(value);
            if (save) PlayerPrefs.SetFloat(MasterVolumeKey, manager._masterVolume);
            manager.RefreshActiveVolumes();
        }

        public static void SetSfxVolume(float value, bool save = true)
        {
            var manager = EnsureInstance();
            manager._sfxVolume = Mathf.Clamp01(value);
            if (save) PlayerPrefs.SetFloat(SfxVolumeKey, manager._sfxVolume);
            manager.RefreshActiveVolumes();
        }

        public static void SetUiVolume(float value, bool save = true)
        {
            var manager = EnsureInstance();
            manager._uiVolume = Mathf.Clamp01(value);
            if (save) PlayerPrefs.SetFloat(UiVolumeKey, manager._uiVolume);
            manager.RefreshActiveVolumes();
        }

        public static void Stop(AudioCue cue)
        {
            if (_instance == null || cue == null) return;
            for (var i = 0; i < _instance._voices.Count; i++)
            {
                var voice = _instance._voices[i];
                if (voice.Active && voice.Cue == cue)
                    _instance.ReleaseVoice(voice);
            }
        }

        public static void StopAll()
        {
            if (_instance == null) return;
            for (var i = 0; i < _instance._voices.Count; i++)
                _instance.ReleaseVoice(_instance._voices[i]);
        }

        private static AudioManager EnsureInstance()
        {
            if (_instance != null) return _instance;

            _instance = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (_instance != null)
            {
                if (!_instance.gameObject.activeSelf) _instance.gameObject.SetActive(true);
                return _instance;
            }

            var managerObject = new GameObject("[AudioManager]");
            _instance = managerObject.AddComponent<AudioManager>();
            return _instance;
        }

        private bool PlayInternal(AudioCue cue, Vector3 position, float volumeScale, bool usePosition)
        {
            var now = Time.unscaledTime;
            if (_lastPlayedAt.TryGetValue(cue, out var lastPlayed) && now - lastPlayed < cue.Cooldown)
                return false;

            if (!cue.TrySelect(out var clip, out var pitch))
            {
                ReportInvalidCue(cue);
                return false;
            }

            if (!EnsureClipLoaded(clip, cue)) return false;

            var cueVoices = GetActiveVoices(cue);
            if (cueVoices.Count >= cue.MaxSimultaneous)
            {
                if (cue.OverflowMode == AudioOverflowMode.IgnoreNew)
                    return false;
                ReleaseVoice(FindOldest(cueVoices));
            }

            var voice = AcquireVoice(cue.Priority);
            if (voice == null) return false;

            var source = voice.Source;
            source.transform.position = usePosition ? position : transform.position;
            source.clip = clip;
            source.pitch = pitch;
            source.priority = cue.Priority;
            source.outputAudioMixerGroup = cue.Output;
            source.spatialBlend = usePosition ? cue.SpatialBlend : 0f;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.rolloffMode = cue.RolloffMode;
            source.ignoreListenerPause = cue.IgnoreListenerPause;

            voice.Cue = cue;
            voice.Bus = cue.Bus;
            voice.BaseVolume = cue.Volume * volumeScale;
            voice.StartedAt = now;
            voice.Active = true;
            source.volume = GetMixedVolume(voice);
            source.Play();

            _lastPlayedAt[cue] = now;
            return true;
        }

        private bool PreloadInternal(AudioCue cue)
        {
            var foundClip = false;
            var loadedAll = true;

            for (var i = 0; i < cue.Clips.Count; i++)
            {
                var clip = cue.Clips[i];
                if (clip == null) continue;

                foundClip = true;
                if (!EnsureClipLoaded(clip, cue)) loadedAll = false;
            }

            if (!foundClip) ReportInvalidCue(cue);
            return foundClip && loadedAll;
        }

        private bool EnsureClipLoaded(AudioClip clip, AudioCue cue)
        {
            if (clip == null) return false;
            if (clip.loadState == AudioDataLoadState.Loaded ||
                clip.loadState == AudioDataLoadState.Loading)
                return true;

            if (clip.loadState == AudioDataLoadState.Unloaded &&
                clip.LoadAudioData() &&
                clip.loadState != AudioDataLoadState.Failed)
                return true;

            if (_reportedClipLoadFailures.Add(clip))
            {
                Debug.LogWarning(
                    "[AudioManager] Failed to load AudioClip '" + clip.name +
                    "' for AudioCue '" + cue.DisplayName + "'.",
                    cue);
            }

            return false;
        }

        private void ReportInvalidCue(AudioCue cue)
        {
            if (_reportedInvalidCues.Add(cue))
                Debug.LogWarning("[AudioManager] AudioCue has no valid clip: " + cue.name, cue);
        }

        private Voice AcquireVoice(int incomingPriority)
        {
            for (var i = 0; i < _voices.Count; i++)
            {
                if (!_voices[i].Active) return _voices[i];
            }

            if (_voices.Count < maxVoices)
                return CreateVoice();

            Voice candidate = null;
            for (var i = 0; i < _voices.Count; i++)
            {
                var current = _voices[i];
                if (current.Source.priority < incomingPriority) continue;
                if (candidate == null || current.Source.priority > candidate.Source.priority ||
                    current.Source.priority == candidate.Source.priority && current.StartedAt < candidate.StartedAt)
                    candidate = current;
            }

            if (candidate != null) ReleaseVoice(candidate);
            return candidate;
        }

        private Voice CreateVoice()
        {
            var voiceObject = new GameObject("SFX Voice " + (_voices.Count + 1));
            voiceObject.transform.SetParent(transform, false);
            var source = voiceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;

            var voice = new Voice { Source = source };
            _voices.Add(voice);
            return voice;
        }

        private List<Voice> GetActiveVoices(AudioCue cue)
        {
            var result = new List<Voice>();
            for (var i = 0; i < _voices.Count; i++)
            {
                if (_voices[i].Active && _voices[i].Cue == cue)
                    result.Add(_voices[i]);
            }
            return result;
        }

        private static Voice FindOldest(List<Voice> voices)
        {
            var oldest = voices[0];
            for (var i = 1; i < voices.Count; i++)
            {
                if (voices[i].StartedAt < oldest.StartedAt) oldest = voices[i];
            }
            return oldest;
        }

        private void ReleaseVoice(Voice voice)
        {
            if (voice == null) return;
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Cue = null;
            voice.Active = false;
            voice.BaseVolume = 0f;
        }

        private void RefreshActiveVolumes()
        {
            for (var i = 0; i < _voices.Count; i++)
            {
                var voice = _voices[i];
                if (voice.Active) voice.Source.volume = GetMixedVolume(voice);
            }
        }

        private float GetMixedVolume(Voice voice)
        {
            var busVolume = voice.Bus == AudioBus.UI ? _uiVolume : _sfxVolume;
            return Mathf.Clamp01(voice.BaseVolume * _masterVolume * busVolume);
        }

        private void OnValidate()
        {
            maxVoices = Mathf.Max(1, maxVoices);
            initialVoices = Mathf.Clamp(initialVoices, 1, maxVoices);
        }
    }
}
