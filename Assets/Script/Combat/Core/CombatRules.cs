using System;

namespace KiKs.Combat
{
    public sealed class EnemyTurnRules
    {
        public int DeckSize { get; }
        public int BaseActionPoints { get; }
        public int CardsDrawnPerTurn { get; }
        public int CardsPlayedPerTurn { get; }
        public int HandLimit { get; }
        public int RecentCardWindowSize { get; }
        public int MaxTwoCostCardsInWindow { get; }
        public int BerserkTurn { get; }
        public bool UsesExpensiveCardWindow =>
            RecentCardWindowSize > 0 && MaxTwoCostCardsInWindow > 0;

        public EnemyTurnRules(
            int deckSize,
            int baseActionPoints,
            int cardsDrawnPerTurn,
            int cardsPlayedPerTurn,
            int handLimit,
            int recentCardWindowSize = 0,
            int maxTwoCostCardsInWindow = 0,
            int berserkTurn = 0)
        {
            if (deckSize <= 0) throw new ArgumentOutOfRangeException(nameof(deckSize));
            if (baseActionPoints < 0) throw new ArgumentOutOfRangeException(nameof(baseActionPoints));
            if (cardsDrawnPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(cardsDrawnPerTurn));
            if (cardsPlayedPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(cardsPlayedPerTurn));
            if (handLimit <= 0) throw new ArgumentOutOfRangeException(nameof(handLimit));
            if (recentCardWindowSize < 0) throw new ArgumentOutOfRangeException(nameof(recentCardWindowSize));
            if (maxTwoCostCardsInWindow < 0) throw new ArgumentOutOfRangeException(nameof(maxTwoCostCardsInWindow));
            if (berserkTurn < 0) throw new ArgumentOutOfRangeException(nameof(berserkTurn));

            DeckSize = deckSize;
            BaseActionPoints = baseActionPoints;
            CardsDrawnPerTurn = cardsDrawnPerTurn;
            CardsPlayedPerTurn = cardsPlayedPerTurn;
            HandLimit = handLimit;
            RecentCardWindowSize = recentCardWindowSize;
            MaxTwoCostCardsInWindow = maxTwoCostCardsInWindow;
            BerserkTurn = berserkTurn;
        }
    }

    /// <summary>Immutable runtime snapshot of battle-wide rules.</summary>
    public sealed class CombatRules
    {
        public int BaseActionPoints { get; }
        public int CardsDrawnPerTurn { get; }
        public int HandLimit { get; }
        public int ExpectedInitialDeckSize { get; }
        public int ExecutionDamage { get; }
        public ToughnessRestoreMode RestoreMode { get; }
        public int FixedToughnessRestoreAmount { get; }

        public int ManaPerTurn { get; }
        public int CardUpgradeManaCost { get; }
        public int SummonedCompanionResourceBonus { get; }

        public EnemyTurnRules MinionTurnRules { get; }
        public EnemyTurnRules EliteTurnRules { get; }
        public EnemyTurnRules BossTurnRules { get; }

        public CombatRules(
            int baseActionPoints,
            int cardsDrawnPerTurn,
            int handLimit,
            int expectedInitialDeckSize,
            int executionDamage,
            ToughnessRestoreMode toughnessRestoreMode,
            int fixedToughnessRestoreAmount,
            int manaPerTurn,
            int cardUpgradeManaCost,
            EnemyTurnRules minionTurnRules,
            EnemyTurnRules eliteTurnRules,
            EnemyTurnRules bossTurnRules,
            int summonedCompanionResourceBonus = 1)
        {
            if (baseActionPoints < 0) throw new ArgumentOutOfRangeException(nameof(baseActionPoints));
            if (cardsDrawnPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(cardsDrawnPerTurn));
            if (handLimit <= 0) throw new ArgumentOutOfRangeException(nameof(handLimit));
            if (expectedInitialDeckSize <= 0) throw new ArgumentOutOfRangeException(nameof(expectedInitialDeckSize));
            if (executionDamage < 0) throw new ArgumentOutOfRangeException(nameof(executionDamage));
            if (fixedToughnessRestoreAmount < 0) throw new ArgumentOutOfRangeException(nameof(fixedToughnessRestoreAmount));
            if (manaPerTurn <= 0) throw new ArgumentOutOfRangeException(nameof(manaPerTurn));
            if (cardUpgradeManaCost < 0) throw new ArgumentOutOfRangeException(nameof(cardUpgradeManaCost));
            if (summonedCompanionResourceBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(summonedCompanionResourceBonus));

            BaseActionPoints = baseActionPoints;
            CardsDrawnPerTurn = cardsDrawnPerTurn;
            HandLimit = handLimit;
            ExpectedInitialDeckSize = expectedInitialDeckSize;
            ExecutionDamage = executionDamage;
            RestoreMode = toughnessRestoreMode;
            FixedToughnessRestoreAmount = fixedToughnessRestoreAmount;
            ManaPerTurn = manaPerTurn;
            CardUpgradeManaCost = cardUpgradeManaCost;
            SummonedCompanionResourceBonus = summonedCompanionResourceBonus;
            MinionTurnRules = minionTurnRules ?? throw new ArgumentNullException(nameof(minionTurnRules));
            EliteTurnRules = eliteTurnRules ?? throw new ArgumentNullException(nameof(eliteTurnRules));
            BossTurnRules = bossTurnRules ?? throw new ArgumentNullException(nameof(bossTurnRules));
        }

        public int GetToughnessRestoreAmount(CombatantState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return RestoreMode == ToughnessRestoreMode.Full
                ? target.MaxToughness
                : FixedToughnessRestoreAmount;
        }

        public EnemyTurnRules GetEnemyTurnRules(EnemyRank rank)
        {
            switch (rank)
            {
                case EnemyRank.Elite: return EliteTurnRules;
                case EnemyRank.Boss: return BossTurnRules;
                default: return MinionTurnRules;
            }
        }

        public static CombatRules CreateDefault()
        {
            return new CombatRules(
                3, 4, 10, 15,
                60,
                ToughnessRestoreMode.Full, 0,
                1, 1,
                new EnemyTurnRules(3, 3, 1, 1, 5),
                new EnemyTurnRules(4, 4, 2, 2, 5, 5, 2),
                new EnemyTurnRules(4, 5, 2, 2, 5, 5, 2, 12));
        }
    }
}