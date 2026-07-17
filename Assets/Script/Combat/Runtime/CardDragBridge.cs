using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.Combat
{
    /// <summary>
    /// 桥接 Draggable 和 CardView。
    /// Draggable 处理拖拽位移，本脚本把拖拽事件转发给 CardView 区分点击/拖拽。
    /// 挂在卡牌上，不需要手动配置。
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public class CardDragBridge : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardView _cardView;

        private void Awake()
        {
            _cardView = GetComponent<CardView>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _cardView.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _cardView.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _cardView.OnEndDrag(eventData);
        }
    }
}
