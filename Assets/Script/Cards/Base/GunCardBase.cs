using System.Collections.Generic;

namespace KiKs.Cards
{
    public abstract class GunCardBase : AttackCardBase
    {
        public sealed override CardFamily Family => CardFamily.Gun;

        protected sealed override void BuildAdditionalCommands(List<CardEffectCommand> commands)
        {
            BuildGunCommands(commands);
        }

        protected virtual void BuildGunCommands(List<CardEffectCommand> commands)
        {
        }
    }
}
