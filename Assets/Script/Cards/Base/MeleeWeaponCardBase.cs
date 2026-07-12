using System.Collections.Generic;

namespace KiKs.Cards
{
    public abstract class MeleeWeaponCardBase : AttackCardBase
    {
        public abstract float ToughnessDamagePercent { get; }

        protected sealed override void BuildAdditionalCommands(List<CardEffectCommand> commands)
        {
            if (ToughnessDamagePercent > 0f)
                commands.Add(CardEffectCommand.ToughnessDamage(ToughnessDamagePercent));

            BuildMeleeCommands(commands);
        }

        protected virtual void BuildMeleeCommands(List<CardEffectCommand> commands)
        {
        }
    }
}
