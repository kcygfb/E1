using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "HandAxeCard", menuName = "KiKs/Cards/Cold Weapons/Axes/Hand Axe")]
    public sealed class HandAxeCard : AxeCardBase
    {
        public override string CardId => "axe_hand";
        public override string CardName => "手斧";
        public override int AttackPower => 15;
        public override float ToughnessDamagePercent => 0.10f;
    }
}
