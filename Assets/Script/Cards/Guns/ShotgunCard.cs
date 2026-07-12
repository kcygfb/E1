using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ShotgunCard", menuName = "KiKs/Cards/Guns/Shotgun")]
    public sealed class ShotgunCard : GunCardBase
    {
        public override string CardId => "gun_shotgun";
        public override string CardName => "霰弹枪";
        public override int AttackPower => 10;
        public override int AttackCount => 3;

        protected override void BuildGunCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.FlatToughnessDamage(3, AttackCount));
        }
    }
}
