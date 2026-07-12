using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "PoisonCard", menuName = "KiKs/Cards/Magic/Poison")]
    public sealed class PoisonCard : MagicCardBase
    {
        public override string CardId => "magic_poison";
        public override string CardName => "中毒";

        protected override void BuildMagicCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(
                CardEffectType.Poison, 3, amount: 2, damageType: DamageType.True));
        }
    }
}
