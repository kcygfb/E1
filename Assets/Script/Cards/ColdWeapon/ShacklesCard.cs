using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Cards
{
    [CreateAssetMenu(fileName = "ShacklesCard", menuName = "KiKs/Cards/Cold Weapons/Chain/Shackles")]
    public sealed class ShacklesCard : ChainWeaponCardBase
    {
        public override string CardId => "chain_shackles";
        public override string CardName => "绊脚锁";
        public override int AttackPower => 1;
        public override float ToughnessDamagePercent => 0.10f;

        protected override void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Status(CardEffectType.Stun, 1));
        }
    }
}
