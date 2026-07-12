using UnityEngine;

namespace KiKs.Combat
{
    public enum CardType { Click, Drag }

    /// <summary>
    /// 卡牌属性：类型、攻击力、削韧值。
    /// </summary>
    public class CardData : MonoBehaviour
    {
        [Header("卡牌类型")]
        [SerializeField] private CardType cardType = CardType.Click;

        [Header("属性数值")]
        [SerializeField] private int attack = 10;
        [SerializeField] private int toughnessReduction = 5;

        public CardType Type => cardType;
        public int Attack => attack;
        public int ToughnessReduction => toughnessReduction;
    }
}
