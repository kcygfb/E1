using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "KatanaCard", menuName = "KiKs/Cards/Cold Weapons/Blades/Katana")]
    public sealed class KatanaCard : BladeCardBase
    {
        public override string CardId => "blade_katana";
        public override string CardName => "太刀";
        public override int AttackPower => 25;
        public override float ToughnessDamagePercent => 0.05f;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.ArmorBreak, 3));
        }
    }
}
