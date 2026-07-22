using System;

namespace KiKs.Combat
{
    /// <summary>Immutable runtime snapshot of battle-wide rules.</summary>
    public sealed class CombatRules
    {
        public int BaseActionPoints { get; }
        public int CardsDrawnPerTurn { get; }
        public int HandLimit { get; }
        public int ExpectedInitialDeckSize { get; }
        public int EliteExecutionDamage { get; }
        public int EliteStunTurns { get; }
        public int BossExecutionDamage { get; }
        public int BossStunTurns { get; }
        public ToughnessRestoreMode RestoreMode { get; }
        public int FixedToughnessRestoreAmount { get; }

        public int StartingMana { get; }
        public int MaximumMana { get; }
        public int MaximumManaSpendPerTurn { get; }
        public int CardUpgradeManaCost { get; }
        public int MagicCardsPerTurn { get; }
        public int UltimateManaThreshold { get; }
        public int UltimateDamage { get; }
        public int UltimateStunTurns { get; }

        public CombatRules(
            int baseActionPoints,
            int cardsDrawnPerTurn,
            int handLimit,
            int expectedInitialDeckSize,
            int eliteExecutionDamage,
            int eliteStunTurns,
            int bossExecutionDamage,
            int bossStunTurns,
            ToughnessRestoreMode toughnessRestoreMode,
            int fixedToughnessRestoreAmount,
            int startingMana,
            int maximumMana,
            int maximumManaSpendPerTurn,
            int cardUpgradeManaCost,
            int magicCardsPerTurn,
            int ultimateManaThreshold,
            int ultimateDamage,
            int ultimateStunTurns)
        {
            if (baseActionPoints < 0) throw new ArgumentOutOfRangeException(nameof(baseActionPoints));
            if (cardsDrawnPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(cardsDrawnPerTurn));
            if (handLimit <= 0) throw new ArgumentOutOfRangeException(nameof(handLimit));
            if (expectedInitialDeckSize <= 0) throw new ArgumentOutOfRangeException(nameof(expectedInitialDeckSize));
            if (eliteExecutionDamage < 0) throw new ArgumentOutOfRangeException(nameof(eliteExecutionDamage));
            if (eliteStunTurns < 0) throw new ArgumentOutOfRangeException(nameof(eliteStunTurns));
            if (bossExecutionDamage < 0) throw new ArgumentOutOfRangeException(nameof(bossExecutionDamage));
            if (bossStunTurns < 0) throw new ArgumentOutOfRangeException(nameof(bossStunTurns));
            if (fixedToughnessRestoreAmount < 0) throw new ArgumentOutOfRangeException(nameof(fixedToughnessRestoreAmount));
            if (maximumMana < 0) throw new ArgumentOutOfRangeException(nameof(maximumMana));
            if (startingMana < 0 || startingMana > maximumMana) throw new ArgumentOutOfRangeException(nameof(startingMana));
            if (maximumManaSpendPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(maximumManaSpendPerTurn));
            if (cardUpgradeManaCost < 0) throw new ArgumentOutOfRangeException(nameof(cardUpgradeManaCost));
            if (magicCardsPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(magicCardsPerTurn));
            if (ultimateManaThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(ultimateManaThreshold));
            if (ultimateDamage < 0) throw new ArgumentOutOfRangeException(nameof(ultimateDamage));
            if (ultimateStunTurns < 0) throw new ArgumentOutOfRangeException(nameof(ultimateStunTurns));

            BaseActionPoints = baseActionPoints;
            CardsDrawnPerTurn = cardsDrawnPerTurn;
            HandLimit = handLimit;
            ExpectedInitialDeckSize = expectedInitialDeckSize;
            EliteExecutionDamage = eliteExecutionDamage;
            EliteStunTurns = eliteStunTurns;
            BossExecutionDamage = bossExecutionDamage;
            BossStunTurns = bossStunTurns;
            RestoreMode = toughnessRestoreMode;
            FixedToughnessRestoreAmount = fixedToughnessRestoreAmount;
            StartingMana = startingMana;
            MaximumMana = maximumMana;
            MaximumManaSpendPerTurn = maximumManaSpendPerTurn;
            CardUpgradeManaCost = cardUpgradeManaCost;
            MagicCardsPerTurn = magicCardsPerTurn;
            UltimateManaThreshold = ultimateManaThreshold;
            UltimateDamage = ultimateDamage;
            UltimateStunTurns = ultimateStunTurns;
        }

        public int GetToughnessRestoreAmount(CombatantState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return RestoreMode == ToughnessRestoreMode.Full
                ? target.MaxToughness
                : FixedToughnessRestoreAmount;
        }

        public static CombatRules CreateDefault()
        {
            return new CombatRules(
                3, 4, 10, 5,
                40, 1, 40, 1,
                ToughnessRestoreMode.Full, 0,
                5, 5, 1, 1, 1,
                3, 0, 1);
        }
    }
}
