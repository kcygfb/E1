namespace KiKs.Combat
{
    public enum CombatPhase
    {
        NotStarted,
        PlayerTurnStart,
        PlayerInput,
        ResolvingCard,
        AwaitingExecutionConfirmation,
        PlayerTurnEnd,
        EnemyTurn,
        Victory,
        Defeat
    }

    public enum BattleOutcome { None, Victory, Defeat }
    public enum CombatantSide { Player, Enemy }
    public enum EnemyRank { None, Minion, Elite, Boss }
    public enum CardTargetType { Self, SingleEnemy }
    public enum ToughnessRestoreMode { Full, FixedAmount }
    public enum CardResourceType { ActionPoint, Mana }
    public enum DamageType { Normal, True }
    public enum ValueUnit { Points, Percent }

    public enum CardEffectType
    {
        Damage,
        ToughnessDamage,
        Stun,
        Bleed,
        Poison,
        Vulnerability,
        NullifyAttacks,
        DamageReduction,
        SkipEnemyTurns,
        DrawCards,
        Immunity,
        SummonCompanion,
        LifeStealMaxHealth,
        GainResource,
        PlayCardsFromDiscard
    }

    public enum CombatEventType
    {
        BattleStarted,
        PhaseChanged,
        TurnStarted,
        ActionPointsChanged,
        ManaChanged,
        DeckReshuffled,
        CardDrawn,
        CardDiscarded,
        CardPlayed,
        CardUpgraded,
        DamageApplied,
        HealingApplied,
        ToughnessChanged,
        ToughnessBroken,
        StatusApplied,
        ExecutionConfirmationRequired,
        ExecutionResolved,
        StunApplied,
        EnemyActionSkipped,
        UltimateTriggered,
        EffectNotImplemented,
        CombatantDied,
        EnemyTurnStarted,
        Victory,
        Defeat,
        ActionRejected
    }
}
