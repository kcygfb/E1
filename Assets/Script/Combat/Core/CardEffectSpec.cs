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
    /// One JSON effect entry. Base/upgraded values are resolved against the battle card instance.
    /// </summary>
    public sealed class CardEffectSpec
    {
        public CardEffectType Type { get; }
        public UpgradeableNumber Amount { get; }
        public UpgradeableNumber Hits { get; }
        public UpgradeableNumber DurationTurns { get; }
        public UpgradeableNumber TriggerCount { get; }
        public UpgradeableNumber DamagePerTurn { get; }
        public DamageType DamageType { get; }
        public ValueUnit Unit { get; }
        public int MinimumDamagePerHit { get; }
        public bool Stackable { get; }
        public double Multiplier { get; }
        public string CompanionId { get; }
        public int NormalTargetPercent { get; }
        public int BossPercent { get; }
        public CardResourceType Resource { get; }
        public string Timing { get; }
        public string Selection { get; }

        public bool HasUpgrade =>
            Amount.HasUpgrade || Hits.HasUpgrade || DurationTurns.HasUpgrade ||
            TriggerCount.HasUpgrade || DamagePerTurn.HasUpgrade;

        public CardEffectSpec(
            CardEffectType type,
            UpgradeableNumber amount,
            UpgradeableNumber hits,
            UpgradeableNumber durationTurns,
            UpgradeableNumber triggerCount,
            UpgradeableNumber damagePerTurn,
            DamageType damageType,
            ValueUnit unit,
            int minimumDamagePerHit,
            bool stackable,
            double multiplier,
            string companionId,
            int normalTargetPercent,
            int bossPercent,
            CardResourceType resource,
            string timing,
            string selection)
        {
            Type = type;
            Amount = amount;
            Hits = hits;
            DurationTurns = durationTurns;
            TriggerCount = triggerCount;
            DamagePerTurn = damagePerTurn;
            DamageType = damageType;
            Unit = unit;
            MinimumDamagePerHit = Math.Max(0, minimumDamagePerHit);
            Stackable = stackable;
            Multiplier = multiplier;
            CompanionId = companionId ?? string.Empty;
            NormalTargetPercent = Math.Max(0, normalTargetPercent);
            BossPercent = Math.Max(0, bossPercent);
            Resource = resource;
            Timing = timing ?? string.Empty;
            Selection = selection ?? string.Empty;
        }
    }
}
