using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "BattleAxeCard", menuName = "KiKs/Cards/Cold Weapons/Axes/Battle Axe")]
    public sealed class BattleAxeCard : AxeCardBase
    {
        public override string CardId => "axe_battle";
        public override string CardName => "战斧";
        public override int AttackPower => 35;
        public override float ToughnessDamagePercent => 0.35f;
        public override bool IsSpecial => true;
    }
}
