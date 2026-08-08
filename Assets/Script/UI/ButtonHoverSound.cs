using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>按钮音效：悬浮 + 点击。点击时中断上一个音效。</summary>
    public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("悬浮音效")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private float hoverVolume = 1f;

        [Header("点击音效")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float clickVolume = 1f;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            // 预加载音频数据，消除首次播放延迟
            if (hoverSound != null) hoverSound.LoadAudioData();
            if (clickSound != null) clickSound.LoadAudioData();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverSound == null || _audioSource == null) return;
            _audioSource.volume = hoverVolume;
            _audioSource.clip = hoverSound;
            _audioSource.Play();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickSound == null || _audioSource == null) return;
            // 中断当前播放，立即播放点击音效
            _audioSource.Stop();
            _audioSource.volume = clickVolume;
            _audioSource.clip = clickSound;
            _audioSource.Play();
        }
    }
}