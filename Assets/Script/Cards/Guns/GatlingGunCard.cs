using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "GatlingGunCard", menuName = "KiKs/Cards/Guns/Gatling Gun")]
    public sealed class GatlingGunCard : GunCardBase
    {
        public override string CardId => "gun_gatling";
        public override string CardName => "加特林";
        public override int AttackPower => 2;
        public override int AttackCount => 100;
    }
}
