using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 鼠标悬停时显示一个选中框图片，离开时隐藏。
    /// 挂到按钮上，在 Inspector 里把选中框的 Image 拖到 selectionFrame 字段即可。
    /// </summary>
    public class ButtonHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("选中框")]
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Animator animator;
        [SerializeField] private string playStateName = "选中框";

        [Header("淡入淡出")]
        [SerializeField] private float fadeInDuration = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.08f;
        [SerializeField] private Ease ease = Ease.OutCubic;
        [Tooltip("播放速度倍率，1=正常速度")]
        [SerializeField] private float playSpeed = 1f;

        [Header("缩放")]
        [Tooltip("悬停时的缩放倍率")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float scaleDuration = 0.15f;
        [Tooltip("按下时的额外缩放")]
        [SerializeField] private float pressScale = 0.9f;
        [SerializeField] private float pressDuration = 0.08f;

        private Tween _currentFade;
        private Tween _currentScale;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (selectionFrame != null)
            {
                var c = selectionFrame.color;
                c.a = 0f;
                selectionFrame.color = c;
            }
            if (animator != null)
            {
                animator.Play(playStateName, 0, 0f);
                animator.speed = 0f;
            }
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled) return;
            if (selectionFrame != null)
            {
                _currentFade?.Kill();
                _currentFade = selectionFrame.DOFade(1f, fadeInDuration).SetEase(ease);
            }
            if (animator != null)
            {
                animator.Play(playStateName, 0, 0f);
                animator.speed = playSpeed;
            }
            ScaleTo(_originalScale * hoverScale, scaleDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled) return;
            if (selectionFrame != null)
            {
                _currentFade?.Kill();
                _currentFade = selectionFrame.DOFade(0f, fadeOutDuration).SetEase(ease)
                    .OnComplete(() => { if (animator != null) animator.speed = 0f; });
            }
            ScaleTo(_originalScale, scaleDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled) return;
            ScaleTo(_originalScale * hoverScale * pressScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enabled) return;
            ScaleTo(_originalScale * hoverScale, pressDuration);
        }

        private void ScaleTo(Vector3 target, float duration)
        {
            _currentScale?.Kill();
            _currentScale = transform.DOScale(target, duration).SetEase(ease);
        }

        private void OnDestroy()
        {
            _currentFade?.Kill();
            _currentScale?.Kill();
        }
    }
}
