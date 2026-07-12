using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "FreezeCard", menuName = "KiKs/Cards/Magic/Freeze")]
    public sealed class FreezeCard : MagicCardBase
    {
        public override string CardId => "magic_freeze";
        public override string CardName => "冻结";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Freeze, 1));
        }
    }
}
