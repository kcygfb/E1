using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "BlockCard", menuName = "KiKs/Cards/Defense/Block")]
    public sealed class BlockCard : DefenseCardBase
    {
        public override string CardId => "defense_block";
        public override string CardName => "格挡";

        protected override void BuildDefenseCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.DamageReduction, 1, percentage: 0.50f));
        }
    }
}
