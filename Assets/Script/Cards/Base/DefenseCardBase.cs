using System.Collections.Generic;

namespace KiKs.Cards
{
    public abstract class DefenseCardBase : CardBase
    {
        public sealed override CardCategory Category => CardCategory.Defense;
        public sealed override CardFamily Family => CardFamily.Defense;
        public sealed override CardTargetType TargetType => CardTargetType.Self;

        protected sealed override void BuildEffectCommands(List<CardEffectCommand> commands)
        {
            BuildDefenseCommands(commands);
        }

        protected abstract void BuildDefenseCommands(List<CardEffectCommand> commands);
    }
}
