using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace KiKs.UI
{
    /// <summary>
    /// UI 拖拽功能，可挂在卡牌、按钮等任何 RectTransform 上。
    /// 与 CardInteraction 互补：CardInteraction 处理悬浮/点击动效，本脚本处理拖拽位移。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("拖拽设置")]
        [SerializeField] private bool returnOnEnd = false;
        [SerializeField] private float returnDuration = 0.2f;

        [Header("轴约束")]
        [SerializeField] private bool constrainX = false;
        [SerializeField] private bool constrainY = false;

        public bool IsDragging { get; private set; }

        private RectTransform _rect;
        private RectTransform _dragParent;
        private Vector2 _originPos;
        private Vector2 _dragOffset;
        private CanvasGroup _group;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _group = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
            _originPos = _rect.anchoredPosition;

            // Kill any running DOTween animations on this rect (e.g. from CardInteraction)
            _rect.DOKill();

            // 使用父级作为拖拽坐标参考空间
            _dragParent = _rect.parent as RectTransform;
            if (_dragParent == null) return;

            // 记录鼠标在父级空间中的起始位置，算出与卡牌锚点的偏移量
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragParent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerLocal
            );
            _dragOffset = _originPos - pointerLocal;

            if (_group != null)
            {
                _group.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging || _dragParent == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragParent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerLocal
            );

            Vector2 target = pointerLocal + _dragOffset;

            // 约束轴：锁定被约束的分量为拖拽起始值
            if (constrainX) target.x = _originPos.x;
            if (constrainY) target.y = _originPos.y;

            _rect.anchoredPosition = target;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;

            if (_group != null)
            {
                _group.blocksRaycasts = true;
            }

            if (returnOnEnd)
            {
                _rect.DOAnchorPos(_originPos, returnDuration).SetEase(Ease.OutQuint);
            }
        }
    }
}
