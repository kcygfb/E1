using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ScalpelCard", menuName = "KiKs/Cards/Cold Weapons/Blades/Scalpel")]
    public sealed class ScalpelCard : BladeCardBase
    {
        public override string CardId => "blade_scalpel";
        public override string CardName => "手术刀";
        public override int AttackPower => 10;
        public override float ToughnessDamagePercent => 0f;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Bleed, 10));
        }
    }
}
