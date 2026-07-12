using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "MeteorHammerCard", menuName = "KiKs/Cards/Cold Weapons/Chain/Meteor Hammer")]
    public sealed class MeteorHammerCard : ChainWeaponCardBase
    {
        public override string CardId => "chain_meteor_hammer";
        public override string CardName => "流星锤";
        public override int AttackPower => 30;
        public override float ToughnessDamagePercent => 0.20f;
        public override bool IsSpecial => true;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.ArmorBreak, 1));
        }
    }
}
