using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "HandgunCard", menuName = "KiKs/Cards/Guns/Handgun")]
    public sealed class HandgunCard : GunCardBase
    {
        public override string CardId => "gun_handgun";
        public override string CardName => "手枪";
        public override int AttackPower => 3;
        public override int AttackCount => 8;
    }
}
