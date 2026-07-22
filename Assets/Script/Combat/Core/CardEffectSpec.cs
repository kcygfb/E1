using System;

namespace KiKs.Combat
{
    public readonly struct UpgradeableNumber
    {
        public static readonly UpgradeableNumber Zero = new UpgradeableNumber(0, null);
        public static readonly UpgradeableNumber One = new UpgradeableNumber(1, null);

        public int BaseValue { get; }
        public int? UpgradedValue { get; }
        public bool HasUpgrade => UpgradedValue.HasValue && UpgradedValue.Value != BaseValue;

        public UpgradeableNumber(int baseValue, int? upgradedValue)
        {
            if (baseValue < 0) throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (upgradedValue < 0) throw new ArgumentOutOfRangeException(nameof(upgradedValue));
            BaseValue = baseValue;
            UpgradedValue = upgradedValue;
        }

        public int Resolve(bool upgraded)
        {
            return upgraded && UpgradedValue.HasValue ? UpgradedValue.Value : BaseValue;
        }
    }

    /// <summary>
    /// One JSON effect entry matching the V2 card-data schema.
    /// Base/upgraded values are resolved against the battle card instance.
    /// </summary>
    public sealed class CardEffectSpec
    {
        public CardEffectType Type { get; }
        public UpgradeableNumber Amount { get; }
        public UpgradeableNumber Hits { get; }
        public ValueUnit Unit { get; }
        public double Multiplier { get; }

        public bool HasUpgrade => Amount.HasUpgrade || Hits.HasUpgrade;

        public CardEffectSpec(
            CardEffectType type,
            UpgradeableNumber amount,
            UpgradeableNumber hits,
            ValueUnit unit,
            double multiplier)
        {
            Type = type;
            Amount = amount;
            Hits = hits;
            Unit = unit;
            Multiplier = multiplier;
        }
    }
}
