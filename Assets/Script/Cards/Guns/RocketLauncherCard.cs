using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "RocketLauncherCard", menuName = "KiKs/Cards/Guns/Rocket Launcher")]
    public sealed class RocketLauncherCard : GunCardBase
    {
        public override string CardId => "gun_rocket_launcher";
        public override string CardName => "火箭筒";
        public override int AttackPower => 30;
        public override int AttackCount => 1;
        public override bool IsSpecial => true;
    }
}
