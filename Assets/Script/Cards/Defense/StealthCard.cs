using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "StealthCard", menuName = "KiKs/Cards/Defense/Stealth")]
    public sealed class StealthCard : DefenseCardBase
    {
        public override string CardId => "defense_stealth";
        public override string CardName => "隐身";
        public override bool IsSpecial => true;

        protected override void BuildDefenseCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.Stealth, 0, triggerCount: 1));
        }
    }
}
