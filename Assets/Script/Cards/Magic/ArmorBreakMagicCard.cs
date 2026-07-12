using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ArmorBreakMagicCard", menuName = "KiKs/Cards/Magic/Armor Break")]
    public sealed class ArmorBreakMagicCard : MagicCardBase
    {
        public override string CardId => "magic_armor_break";
        public override string CardName => "破甲";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.ArmorBreak, 2, amount: 5));
        }
    }
}
