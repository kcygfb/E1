using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "BlindCard", menuName = "KiKs/Cards/Magic/Blind")]
    public sealed class BlindCard : MagicCardBase
    {
        public override string CardId => "magic_blind";
        public override string CardName => "致盲";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.Blind, 3, triggerCount: 1, multiplier: 0f));
        }
    }
}
