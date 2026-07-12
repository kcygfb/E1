using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "SniperRifleCard", menuName = "KiKs/Cards/Guns/Sniper Rifle")]
    public sealed class SniperRifleCard : GunCardBase
    {
        public override string CardId => "gun_sniper_rifle";
        public override string CardName => "狙击枪";
        public override int AttackPower => 10;
        public override int AttackCount => 1;
        public override DamageType AttackDamageType => DamageType.True;
    }
}
