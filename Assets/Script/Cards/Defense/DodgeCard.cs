using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "DodgeCard", menuName = "KiKs/Cards/Defense/Dodge")]
    public sealed class DodgeCard : DefenseCardBase
    {
        public override string CardId => "defense_dodge";
        public override string CardName => "闪避";

        protected override void BuildDefenseCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.Dodge, 1, triggerCount: 1, percentage: 0.50f));
        }
    }
}
