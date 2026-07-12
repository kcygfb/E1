using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "BleedCard", menuName = "KiKs/Cards/Magic/Bleed")]
    public sealed class BleedCard : MagicCardBase
    {
        public override string CardId => "magic_bleed";
        public override string CardName => "流血";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Bleed, 10, amount: 1));
        }
    }
}
