using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "LongAxeCard", menuName = "KiKs/Cards/Cold Weapons/Axes/Long Axe")]
    public sealed class LongAxeCard : AxeCardBase
    {
        public override string CardId => "axe_long";
        public override string CardName => "长斧";
        public override int AttackPower => 10;
        public override float ToughnessDamagePercent => 0.20f;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Stun, 1));
        }
    }
}
