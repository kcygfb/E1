using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "BurnCard", menuName = "KiKs/Cards/Magic/Burn")]
    public sealed class BurnCard : MagicCardBase
    {
        public override string CardId => "magic_burn";
        public override string CardName => "灼烧";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Burn, 2, multiplier: 2f));
        }
    }
}
