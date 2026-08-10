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
    public enum EnemyArchetype
    {
        None,
        Dog,
        LittleGirl,
        BigEye,
        Ghost,
        Cat,
        Nightmare,
        Fatty,
        Thief,
        Merchant,
        Butcher,
        SecondBoss
    }
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
        Bleed,
        Poison,
        NullifyAttacks,
        DamageReduction,
        DrawCards,
        SummonCompanion,
        BleedScaledDamage,
        LifeSteal,
        ReflectDamage,
        BlockDamage,
        BlockScaledDamage,
        Heal,
        PoisonScaledNextAttack,
        PoisonDamageBonus,
        /// <summary>Parry: deal toughness damage and reflect it per incoming enemy attack count.</summary>
        ParryCounter,
        /// <summary>Ambush: skip the enemy's turn, then deal damage.</summary>
        InvisibleAttack,
        /// <summary>Magic burst: deal damage per mana card spent this turn.</summary>
        ManaCardBurst,
        /// <summary>Hydraulic breaker: double an execution triggered by the current card.</summary>
        ExecutionDouble
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
        CombatantDied,
        EnemyTurnStarted,
        Victory,
        Defeat,
        ActionRejected,
        CardActivated,
        CardDestroyed
    }
}
