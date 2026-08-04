namespace KiKs.Combat
{
    public enum CombatPhase
    {
        NotStarted,
        PlayerTurnStart,
        PlayerInput,
        ResolvingCard,
        PlayerTurnEnd,
        EnemyTurn,
        Victory,
        Defeat
    }

    public enum BattleOutcome { None, Victory, Defeat }
    public enum CombatantSide { Player, Enemy }
    public enum EnemyRank { None, Minion, Elite, Boss }
    public enum EnemyArchetype { None, Dog, LittleGirl, BigEye }
    public enum CardTargetType { Self, SingleEnemy }
    public enum ToughnessRestoreMode { Full, FixedAmount }
    public enum CardResourceType { ActionPoint, Mana }
    public enum DamageType { Normal, True }
    public enum ValueUnit { Points, Percent }

    /// <summary>
    /// Status effects that tick every turn (e.g. bleed, poison).
    /// New status types should be added here and handled in <see cref="CombatantState.ProcessStatusTicks"/>.
    /// </summary>
    public enum StatusEffectType { Bleed, Poison }

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
        BleedScaledDamage,
        LifeSteal,
        ReflectDamage,
        BlockDamage,
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
        ExecutionResolved,
        StunApplied,
        ActionNullified,
        CombatantTurnSkipped,
        EnemyActionSkipped,
        StatusTicked,
        EffectNotImplemented,
        CombatantDied,
        EnemyTurnStarted,
        Victory,
        Defeat,
        ActionRejected
    }
}
