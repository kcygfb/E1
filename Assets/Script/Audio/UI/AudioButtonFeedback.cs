using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.Audio
{
    /// <summary>Drop-in explicit UI sound registration for hover and click.</summary>
    [AddComponentMenu("KiKs/Audio/Audio Button Feedback")]
    public sealed class AudioButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Explicit cue registration")]
        [SerializeField] private AudioCue hover;
        [SerializeField] private AudioCue click;

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioManager.TryPlay(hover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager.TryPlay(click);
        }
    }
}
