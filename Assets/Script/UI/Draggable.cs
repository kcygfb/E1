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

        /// <summary>全局：当前是否有任意 Draggable 正在拖拽（用于过滤误触发的 Click）</summary>
        private static int s_activeDragCount;
        public static bool AnyDragging => s_activeDragCount > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticDragState()
        {
            s_activeDragCount = 0;
        }

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
            s_activeDragCount++;
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
            EndDragState();

            if (returnOnEnd)
            {
                _rect.DOAnchorPos(_originPos, returnDuration).SetEase(Ease.OutQuint);
            }
        }

        private void EndDragState()
        {
            if (IsDragging)
            {
                IsDragging = false;
                s_activeDragCount = Mathf.Max(0, s_activeDragCount - 1);
            }

            if (_group != null)
            {
                _group.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 拖拽中途对象被销毁（如出牌动画中禁用/回收卡牌）时，EndDrag 不再触发，
        /// 这里兜底释放全局拖拽计数，避免 AnyDragging 永久为 true 拦截后续点击。
        /// </summary>
        private void OnDisable()
        {
            EndDragState();
        }

        private void OnDestroy()
        {
            EndDragState();
        }
    }
}
