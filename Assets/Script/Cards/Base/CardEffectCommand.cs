namespace KiKs.Cards
{
    /// <summary>
    /// Describes an effect requested by a card. Future battle controllers execute these commands.
    /// </summary>
    public sealed class CardEffectCommand
    {
        public CardEffectType EffectType { get; }
        public int Amount { get; }
        public int HitCount { get; }
        public int DurationTurns { get; }
        public int TriggerCount { get; }
        public float Percentage { get; }
        public float Multiplier { get; }
        public DamageType DamageType { get; }

        private CardEffectCommand(
            CardEffectType effectType,
            int amount,
            int hitCount,
            int durationTurns,
            int triggerCount,
            float percentage,
            float multiplier,
            DamageType damageType)
        {
            EffectType = effectType;
            Amount = amount;
            HitCount = hitCount;
            DurationTurns = durationTurns;
            TriggerCount = triggerCount;
            Percentage = percentage;
            Multiplier = multiplier;
            DamageType = damageType;
        }

        public static CardEffectCommand Damage(int amount, int hitCount, DamageType damageType)
        {
            return new CardEffectCommand(
                CardEffectType.Damage, amount, hitCount, 0, 0, 0f, 1f, damageType);
        }

        public static CardEffectCommand ToughnessDamage(float percentage, int hitCount = 1)
        {
            return new CardEffectCommand(
                CardEffectType.ToughnessDamagePercent, 0, hitCount, 0, 0,
                percentage, 1f, DamageType.Normal);
        }

        public static CardEffectCommand FlatToughnessDamage(int amount, int hitCount = 1)
        {
            return new CardEffectCommand(
                CardEffectType.ToughnessDamageFlat, amount, hitCount, 0, 0,
                0f, 1f, DamageType.Normal);
        }

        public static CardEffectCommand Status(
            CardEffectType effectType,
            int durationTurns,
            int amount = 0,
            int triggerCount = 0,
            float percentage = 0f,
            float multiplier = 1f,
            DamageType damageType = DamageType.Normal)
        {
            return new CardEffectCommand(
                effectType, amount, 1, durationTurns, triggerCount,
                percentage, multiplier, damageType);
        }
    }
}
