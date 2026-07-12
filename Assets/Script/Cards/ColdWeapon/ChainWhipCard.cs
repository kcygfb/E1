using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ChainWhipCard", menuName = "KiKs/Cards/Cold Weapons/Chain/Chain Whip")]
    public sealed class ChainWhipCard : ChainWeaponCardBase
    {
        public override string CardId => "chain_whip";
        public override string CardName => "锁镰";
        public override int AttackPower => 15;
        public override float ToughnessDamagePercent => 0.10f;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Bleed, 6));
        }
    }
}
