using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.Audio
{
    /// <summary>
    /// 通用本地交互音效。适合咖啡店材料、杯子、水壶、菜单等对象；所有字段都可选，
    /// 不需要修改对象原有的拖拽或点击脚本。
    /// </summary>
    [AddComponentMenu("KiKs/Audio/Audio Interaction Feedback")]
    public sealed class AudioInteractionFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [Header("Pointer")]
        [SerializeField] private AudioCue pointerEnter;
        [SerializeField] private AudioCue pointerExit;
        [SerializeField] private AudioCue pointerDown;
        [SerializeField] private AudioCue pointerUp;
        [SerializeField] private AudioCue pointerClick;

        [Header("Drag and drop")]
        [SerializeField] private AudioCue dragStarted;
        [Tooltip("松开拖拽物时播放，不代表业务上一定放置成功。")]
        [SerializeField] private AudioCue dragReleased;
        [Tooltip("有对象被拖放到这个对象上时播放。")]
        [SerializeField] private AudioCue receivedDrop;

        public void OnPointerEnter(PointerEventData eventData) => Play(pointerEnter);
        public void OnPointerExit(PointerEventData eventData) => Play(pointerExit);
        public void OnPointerDown(PointerEventData eventData) => Play(pointerDown);
        public void OnPointerUp(PointerEventData eventData) => Play(pointerUp);
        public void OnPointerClick(PointerEventData eventData) => Play(pointerClick);
        public void OnBeginDrag(PointerEventData eventData) => Play(dragStarted);
        public void OnEndDrag(PointerEventData eventData) => Play(dragReleased);
        public void OnDrop(PointerEventData eventData) => Play(receivedDrop);

        private static void Play(AudioCue cue)
        {
            AudioManager.TryPlay(cue);
        }
    }
}
