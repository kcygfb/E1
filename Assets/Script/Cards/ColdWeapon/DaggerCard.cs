using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "DaggerCard", menuName = "KiKs/Cards/Cold Weapons/Blades/Dagger")]
    public sealed class DaggerCard : BladeCardBase
    {
        public override string CardId => "blade_dagger";
        public override string CardName => "匕首";
        public override int AttackPower => 10;
        public override DamageType AttackDamageType => DamageType.True;
        public override float ToughnessDamagePercent => 0f;
    }
}
