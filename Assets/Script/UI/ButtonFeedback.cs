using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// 通用按钮动效：悬停缩放 + 按压反馈。
    /// 挂到任意带 RectTransform 的对象上即可生效，零配置。
    /// 可选：选中框帧动画（拖入 selectionFrame + animator 即可启用）。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("悬停缩放")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.15f;

        [Header("按压缩放")]
        [SerializeField] private float pressScale = 0.9f;
        [SerializeField] private float pressDuration = 0.08f;

        [Header("缓动")]
        [SerializeField] private Ease ease = Ease.OutCubic;

        [Header("选中框（可选）")]
        [Tooltip("不填则只走缩放动效")]
        [SerializeField] private Image selectionFrame;
        [SerializeField] private Animator animator;
        [SerializeField] private string playStateName = "选中框";
        [Tooltip("帧动画播放速度倍率")]
        [SerializeField] private float playSpeed = 1f;
        [SerializeField] private float fadeInDuration = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.08f;

        private Tween _scaleTween;
        private Tween _fadeTween;
        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;

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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled) return;
            ScaleTo(_originalScale * hoverScale, hoverDuration);

            if (selectionFrame != null)
            {
                _fadeTween?.Kill();
                _fadeTween = selectionFrame.DOFade(1f, fadeInDuration).SetEase(ease);
            }
            if (animator != null)
            {
                animator.Play(playStateName, 0, 0f);
                animator.speed = playSpeed;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled) return;
            ScaleTo(_originalScale, hoverDuration);

            if (selectionFrame != null)
            {
                _fadeTween?.Kill();
                _fadeTween = selectionFrame.DOFade(0f, fadeOutDuration).SetEase(ease)
                    .OnComplete(() => { if (animator != null) animator.speed = 0f; });
            }
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
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(target, duration).SetEase(ease);
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _fadeTween?.Kill();
        }
    }
}
