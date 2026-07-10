using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.Combat
{
    /// <summary>
    /// 拖拽攻击卡：拖到敌人身上松手时造成伤害。
    /// 需配合 Draggable 组件使用。
    /// </summary>
    [RequireComponent(typeof(CardData))]
    public class DragAttackCard : MonoBehaviour, IEndDragHandler
    {
        private CardData _cardData;

        private void Awake()
        {
            _cardData = GetComponent<CardData>();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // 用 RaycastAll 遍历鼠标下所有物体，跳过卡牌自身，找到敌人
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == gameObject) continue;

                var enemy = result.gameObject.GetComponentInParent<EnemyStats>();
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TakeDamage(_cardData.Attack, _cardData.ToughnessReduction);
                    return;
                }
            }
        }
    }
}
