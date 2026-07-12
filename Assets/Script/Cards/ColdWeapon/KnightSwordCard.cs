using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "KnightSwordCard", menuName = "KiKs/Cards/Cold Weapons/Blades/Knight Sword")]
    public sealed class KnightSwordCard : BladeCardBase
    {
        public override string CardId => "blade_knight_sword";
        public override string CardName => "骑士剑";
        public override int AttackPower => 15;
        public override float ToughnessDamagePercent => 0.25f;
    }
}
