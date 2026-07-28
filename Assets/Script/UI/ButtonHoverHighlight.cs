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
    public class ButtonHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("选中框")]
        [SerializeField] private Image selectionFrame;

        [Header("动画")]
        [SerializeField] private float fadeInDuration = 0.12f;
        [SerializeField] private float fadeOutDuration = 0.08f;
        [SerializeField] private Ease ease = Ease.OutCubic;

        private Tween _currentFade;

        private void Awake()
        {
            if (selectionFrame != null)
            {
                var c = selectionFrame.color;
                c.a = 0f;
                selectionFrame.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!enabled || selectionFrame == null) return;
            _currentFade?.Kill();
            _currentFade = selectionFrame.DOFade(1f, fadeInDuration).SetEase(ease);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!enabled || selectionFrame == null) return;
            _currentFade?.Kill();
            _currentFade = selectionFrame.DOFade(0f, fadeOutDuration).SetEase(ease);
        }

        private void OnDestroy()
        {
            _currentFade?.Kill();
        }
    }
}
