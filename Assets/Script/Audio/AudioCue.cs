using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace KiKs.Audio
{
    public enum AudioBus
    {
        Sfx,
        UI
    }

    public enum AudioOverflowMode
    {
        IgnoreNew,
        ReplaceOldest
    }

    /// <summary>
    /// A designer-facing sound registration. Create one asset per logical sound and assign it
    /// directly to the component that needs it. No string id or manager-side registration is used.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioCue", menuName = "KiKs/Audio/Audio Cue", order = 10)]
    public sealed class AudioCue : ScriptableObject
    {
        [Header("Registration")]
        [Tooltip("A readable label for search and diagnostics. It does not need to be unique.")]
        [SerializeField] private string displayName = "New Sound";
        [Tooltip("Drop one or more variants here. A non-repeating random variant is chosen per play.")]
        [SerializeField] private List<AudioClip> clips = new List<AudioClip>();

        [Header("Mix")]
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField] private AudioMixerGroup output;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;
        [Tooltip("Small pitch variation (for example 0.95 to 1.05) prevents repeated hits sounding robotic.")]
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [Range(0, 256)] [SerializeField] private int priority = 128;

        [Header("Repeated-play protection")]
        [Min(0f)] [SerializeField] private float cooldown;
        [Min(1)] [SerializeField] private int maxSimultaneous = 4;
        [SerializeField] private AudioOverflowMode overflowMode = AudioOverflowMode.ReplaceOldest;
        [SerializeField] private bool avoidImmediateRepeat = true;

        [Header("Optional 3D sound")]
        [Range(0f, 1f)] [SerializeField] private float spatialBlend;
        [Min(0.01f)] [SerializeField] private float minDistance = 1f;
        [Min(0.01f)] [SerializeField] private float maxDistance = 30f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [SerializeField] private bool ignoreListenerPause;

        [NonSerialized] private int _lastClipIndex = -1;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<AudioClip> Clips => clips;
        public AudioBus Bus => bus;
        public AudioMixerGroup Output => output;
        public float Volume => volume;
        public int Priority => priority;
        public float Cooldown => cooldown;
        public int MaxSimultaneous => maxSimultaneous;
        public AudioOverflowMode OverflowMode => overflowMode;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public AudioRolloffMode RolloffMode => rolloffMode;
        public bool IgnoreListenerPause => ignoreListenerPause;

        internal bool TrySelect(out AudioClip clip, out float pitch)
        {
            clip = null;
            pitch = 1f;
            if (clips == null || clips.Count == 0) return false;

            var validIndices = ListPool<int>.Get();
            try
            {
                for (var i = 0; i < clips.Count; i++)
                {
                    if (clips[i] != null) validIndices.Add(i);
                }

                if (validIndices.Count == 0) return false;

                var selected = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
                if (avoidImmediateRepeat && validIndices.Count > 1 && selected == _lastClipIndex)
                {
                    var current = validIndices.IndexOf(selected);
                    selected = validIndices[(current + UnityEngine.Random.Range(1, validIndices.Count)) % validIndices.Count];
                }

                _lastClipIndex = selected;
                clip = clips[selected];
                pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
                return clip != null;
            }
            finally
            {
                ListPool<int>.Release(validIndices);
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            maxSimultaneous = Mathf.Max(1, maxSimultaneous);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);

            var minPitch = Mathf.Clamp(Mathf.Min(pitchRange.x, pitchRange.y), -3f, 3f);
            var maxPitch = Mathf.Clamp(Mathf.Max(pitchRange.x, pitchRange.y), -3f, 3f);
            if (Mathf.Approximately(minPitch, 0f)) minPitch = 0.01f;
            if (Mathf.Approximately(maxPitch, 0f)) maxPitch = 0.01f;
            pitchRange = new Vector2(minPitch, maxPitch);
        }

        /// <summary>Small allocation-free list pool used only during random variant selection.</summary>
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
