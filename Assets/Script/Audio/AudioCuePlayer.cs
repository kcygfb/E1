using UnityEngine;

namespace KiKs.Audio
{
    /// <summary>
    /// Inspector/UnityEvent/AnimationEvent bridge. Drop it on an object, assign a cue, then call
    /// Play without writing code.
    /// </summary>
    [AddComponentMenu("KiKs/Audio/Audio Cue Player")]
    public sealed class AudioCuePlayer : MonoBehaviour
    {
        [Tooltip("Explicit sound asset played by this component.")]
        [SerializeField] private AudioCue cue;
        [Range(0f, 2f)] [SerializeField] private float volumeScale = 1f;
        [SerializeField] private bool playOnEnable;
        [Tooltip("If enabled, the cue uses this object's world position and the cue's Spatial Blend.")]
        [SerializeField] private bool playAtTransform;

        public AudioCue Cue
        {
            get => cue;
            set => cue = value;
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        public void Play()
        {
            if (playAtTransform)
                AudioManager.TryPlayAtPosition(cue, transform.position, volumeScale);
            else
                AudioManager.TryPlay(cue, volumeScale);
        }

        public void Stop()
        {
            AudioManager.Stop(cue);
        }
    }
}
