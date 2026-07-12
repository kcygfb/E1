using System.Collections.Generic;

namespace KiKs.Cards
{
    public abstract class MagicCardBase : CardBase
    {
        public sealed override CardCategory Category => CardCategory.Magic;
        public sealed override CardFamily Family => CardFamily.Magic;
        public sealed override CardTargetType TargetType => CardTargetType.SingleEnemy;
        public sealed override int ActionPointCost => 0;
        public override int ManaCost => 1;
        public sealed override bool IsUnique => true;

        protected sealed override void BuildEffectCommands(List<CardEffectCommand> commands)
        {
            BuildMagicCommands(commands);
        }

        protected abstract void BuildMagicCommands(List<CardEffectCommand> commands);
    }
}
