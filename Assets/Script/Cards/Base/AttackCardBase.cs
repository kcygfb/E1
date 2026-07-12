using System.Collections.Generic;

namespace KiKs.Cards
{
    public abstract class AttackCardBase : CardBase
    {
        public sealed override CardCategory Category => CardCategory.Attack;
        public sealed override CardTargetType TargetType => CardTargetType.SingleEnemy;

        public abstract int AttackPower { get; }
        public virtual int AttackCount => 1;
        public virtual DamageType AttackDamageType => DamageType.Normal;

        protected sealed override void BuildEffectCommands(List<CardEffectCommand> commands)
        {
            commands.Add(CardEffectCommand.Damage(AttackPower, AttackCount, AttackDamageType));
            BuildAdditionalCommands(commands);
        }

        protected virtual void BuildAdditionalCommands(List<CardEffectCommand> commands)
        {
        }
    }
}
