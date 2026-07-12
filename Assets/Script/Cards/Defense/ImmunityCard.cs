using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ImmunityCard", menuName = "KiKs/Cards/Defense/Immunity")]
    public sealed class ImmunityCard : DefenseCardBase
    {
        public override string CardId => "defense_immunity";
        public override string CardName => "免疫";

        protected override void BuildDefenseCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.Immunity, 0, triggerCount: 1));
        }
    }
}
