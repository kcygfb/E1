using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 咖啡店 BGM 播放器：点击主按钮展开/收起两个子按钮（播放/暂停、下一首）。
    /// BGM 文件放在 Assets/Audio/Cafe/BGM/ 下，通过 Inspector 配置。
    /// </summary>
    public class CafeBGMPlayer : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("拖入 Assets/Audio/Cafe/BGM/ 下的音频文件")]
        [SerializeField] private List<AudioClip> bgmClips = new();

        [Header("UI References")]
        [SerializeField] private Button mainButton;
        [SerializeField] private RectTransform panel;          // 展开面板（含两个子按钮）
        [SerializeField] private Button playPauseButton;       // 播放/暂停
        [SerializeField] private Button nextButton;            // 下一首
        [SerializeField] private Image playIcon;               // 播放图标
        [SerializeField] private Image pauseIcon;              // 暂停图标
        [SerializeField] private Text trackNameText;           // 歌曲名（可选）

        [Header("Animation")]
        [SerializeField] private float expandDuration = 0.25f;
        [SerializeField] private Ease expandEase = Ease.OutBack;

        private bool _isExpanded;
        private bool _isPlaying;
        private int _currentTrack;
        private Vector2 _panelTargetPos;
        private Vector2 _panelHiddenPos;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;

            _isExpanded = false;
            _isPlaying = false;
            _currentTrack = 0;

            // panel 隐藏位置 = 当前位置向左偏移自身宽度
            if (panel != null)
            {
                _panelTargetPos = panel.anchoredPosition;
                _panelHiddenPos = _panelTargetPos + new Vector2(-panel.rect.width, 0);
                panel.anchoredPosition = _panelHiddenPos;
                panel.gameObject.SetActive(false);
            }

            // 图标初始状态
            UpdatePlayPauseIcon();

            // 歌曲名
            UpdateTrackName();
        }

        private void Start()
        {
            if (mainButton != null)
                mainButton.onClick.AddListener(ToggleExpand);
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(TogglePlayPause);
            if (nextButton != null)
                nextButton.onClick.AddListener(NextTrack);

            // 自动播放第一首
            if (bgmClips.Count > 0)
                PlayTrack(0);
        }

        private void Update()
        {
            // 播完自动切下一首
            if (_isPlaying && !audioSource.isPlaying)
            {
                NextTrack();
            }
        }

        private void ToggleExpand()
        {
            _isExpanded = !_isExpanded;

            if (panel == null) return;

            if (_isExpanded)
            {
                panel.gameObject.SetActive(true);
                panel.DOAnchorPos(_panelTargetPos, expandDuration).SetEase(expandEase);
            }
            else
            {
                panel.DOAnchorPos(_panelHiddenPos, expandDuration).SetEase(Ease.InBack)
                    .OnComplete(() => panel.gameObject.SetActive(false));
            }
        }

        private void TogglePlayPause()
        {
            if (bgmClips.Count == 0)
            {
                Debug.LogWarning("[CafeBGMPlayer] No BGM clips assigned.");
                return;
            }

            if (_isPlaying)
            {
                audioSource.Pause();
                _isPlaying = false;
            }
            else
            {
                if (!audioSource.isPlaying && audioSource.clip == null)
                    PlayTrack(_currentTrack);

                audioSource.UnPause();
                _isPlaying = true;
            }

            UpdatePlayPauseIcon();
        }

        private void NextTrack()
        {
            if (bgmClips.Count == 0) return;

            _currentTrack = (_currentTrack + 1) % bgmClips.Count;
            PlayTrack(_currentTrack);
        }

        private void PlayTrack(int index)
        {
            if (index < 0 || index >= bgmClips.Count) return;

            audioSource.clip = bgmClips[index];
            audioSource.Play();
            _isPlaying = true;
            _currentTrack = index;
            UpdatePlayPauseIcon();
            UpdateTrackName();
        }

        private void UpdatePlayPauseIcon()
        {
            if (playIcon != null) playIcon.gameObject.SetActive(!_isPlaying);
            if (pauseIcon != null) pauseIcon.gameObject.SetActive(_isPlaying);
        }

        private void UpdateTrackName()
        {
            if (trackNameText != null && _currentTrack < bgmClips.Count && bgmClips[_currentTrack] != null)
                trackNameText.text = bgmClips[_currentTrack].name;
        }
    }
}
