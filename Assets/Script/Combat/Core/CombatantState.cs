using System;

namespace KiKs.Combat
{
    /// <summary>Mutable values for one participant in one battle.</summary>
    public sealed class CombatantState
    {
        public string Id { get; }
        public string DisplayName { get; }
        public CombatantSide Side { get; }
        public EnemyRank EnemyRank { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public int MaxToughness { get; }
        public int CurrentToughness { get; private set; }
        public int CurrentActionPoints { get; private set; }
        public int ActionPointModifier { get; private set; }
        public int StunTurns { get; private set; }
        public int NullifyAttackCharges { get; private set; }
        public int DamageReductionPercent { get; private set; }
        public int DamageReductionTurns { get; private set; }
        public int SkipEnemyTurns { get; private set; }
        public int BleedStacks { get; private set; }
        public int BlockPoints { get; private set; }
        public int PendingReflectDamage { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public CombatantState(
            string id,
            string displayName,
            CombatantSide side,
            EnemyRank enemyRank,
            int maxHealth,
            int maxToughness)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Combatant id is required.", nameof(id));
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (maxToughness < 0) throw new ArgumentOutOfRangeException(nameof(maxToughness));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Side = side;
            EnemyRank = enemyRank;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            MaxToughness = maxToughness;
            CurrentToughness = maxToughness;
        }

        public int ApplyDamage(int requestedDamage)
        {
            if (requestedDamage < 0) throw new ArgumentOutOfRangeException(nameof(requestedDamage));
            if (IsDead || requestedDamage == 0) return 0;
            var actualDamage = Math.Min(CurrentHealth, requestedDamage);
            CurrentHealth -= actualDamage;
            return actualDamage;
        }

        public int Heal(int requestedHealing)
        {
            if (requestedHealing < 0) throw new ArgumentOutOfRangeException(nameof(requestedHealing));
            if (IsDead || requestedHealing == 0) return 0;
            var previous = CurrentHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + requestedHealing);
            return CurrentHealth - previous;
        }

        public int Kill()
        {
            if (IsDead) return 0;
            var removedHealth = CurrentHealth;
            CurrentHealth = 0;
            return removedHealth;
        }

        public int ReduceToughness(int requestedReduction)
        {
            if (requestedReduction < 0) throw new ArgumentOutOfRangeException(nameof(requestedReduction));
            if (CurrentToughness <= 0 || requestedReduction == 0) return 0;
            var actualReduction = Math.Min(CurrentToughness, requestedReduction);
            CurrentToughness -= actualReduction;
            return actualReduction;
        }

        public int RestoreToughness(int requestedRestore)
        {
            if (requestedRestore < 0) throw new ArgumentOutOfRangeException(nameof(requestedRestore));
            if (MaxToughness == 0 || requestedRestore == 0) return 0;
            var previous = CurrentToughness;
            CurrentToughness = Math.Min(MaxToughness, CurrentToughness + requestedRestore);
            return CurrentToughness - previous;
        }

        public int GetEffectiveActionPointMaximum(int baseActionPoints)
        {
            if (baseActionPoints < 0) throw new ArgumentOutOfRangeException(nameof(baseActionPoints));
            return Math.Max(0, baseActionPoints + ActionPointModifier);
        }

        public void RestoreActionPoints(int baseActionPoints)
        {
            CurrentActionPoints = GetEffectiveActionPointMaximum(baseActionPoints);
        }

        public bool TrySpendActionPoints(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (CurrentActionPoints < amount) return false;
            CurrentActionPoints -= amount;
            return true;
        }

        public void AddActionPoints(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            CurrentActionPoints += amount;
        }

        public void SetActionPointModifier(int modifier) { ActionPointModifier = modifier; }

        public void AddStun(int turns)
        {
            if (turns < 0) throw new ArgumentOutOfRangeException(nameof(turns));
            StunTurns += turns;
        }

        public bool ConsumeOneStunTurn()
        {
            if (StunTurns <= 0) return false;
            StunTurns--;
            return true;
        }

        public void AddNullifyAttackCharges(int charges)
        {
            if (charges < 0) throw new ArgumentOutOfRangeException(nameof(charges));
            NullifyAttackCharges += charges;
        }

        public bool TryConsumeNullifyAttack()
        {
            if (NullifyAttackCharges <= 0) return false;
            NullifyAttackCharges--;
            return true;
        }

        public void AddDamageReduction(int percent, int turns)
        {
            if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent));
            if (turns < 0) throw new ArgumentOutOfRangeException(nameof(turns));
            DamageReductionPercent = Math.Max(DamageReductionPercent, Math.Min(100, percent));
            DamageReductionTurns = Math.Max(DamageReductionTurns, turns);
        }

        public void AdvancePlayerTurnStatuses()
        {
            if (DamageReductionTurns <= 0) return;
            DamageReductionTurns--;
            if (DamageReductionTurns == 0) DamageReductionPercent = 0;
        }

        public void AddSkipEnemyTurns(int turns)
        {
            if (turns < 0) throw new ArgumentOutOfRangeException(nameof(turns));
            SkipEnemyTurns += turns;
        }

        public bool TryConsumeSkipEnemyTurn()
        {
            if (SkipEnemyTurns <= 0) return false;
            SkipEnemyTurns--;
            return true;
        }

        public void AddBleedStacks(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            BleedStacks += amount;
        }

        /// <summary>
        /// Processes all ticking status effects (bleed, poison, etc.) and returns
        /// a list of tick results. Call once per combatant per turn start.
        /// </summary>
        public System.Collections.Generic.List<StatusTickResult> ProcessStatusTicks()
        {
            var results = new System.Collections.Generic.List<StatusTickResult>();

            // --- Bleed: damage = stacks, stacks-- ---
            if (BleedStacks > 0)
            {
                results.Add(new StatusTickResult(StatusEffectType.Bleed, BleedStacks, BleedStacks - 1));
                BleedStacks--;
            }

            // --- Poison hook (future): flat damage per stack, duration-- ---
            // if (PoisonStacks > 0) { ... }

            return results;
        }

        public void AddBlockPoints(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            BlockPoints += amount;
        }

        public int ConsumeBlockPoints(int incomingDamage)
        {
            if (incomingDamage < 0) throw new ArgumentOutOfRangeException(nameof(incomingDamage));
            var blocked = Math.Min(BlockPoints, incomingDamage);
            BlockPoints -= blocked;
            return blocked;
        }

        public void AddReflectDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            PendingReflectDamage += amount;
        }

        public int ConsumeReflectDamage()
        {
            var amount = PendingReflectDamage;
            PendingReflectDamage = 0;
            return amount;
        }
    }

    /// <summary>
    /// Result of one status effect tick, produced by <see cref="CombatantState.ProcessStatusTicks"/>.
    /// </summary>
    public readonly struct StatusTickResult
    {
        public StatusEffectType Type { get; }
        public int DamageDealt { get; }
        public int RemainingStacks { get; }

        public StatusTickResult(StatusEffectType type, int damageDealt, int remainingStacks)
        {
            Type = type;
            DamageDealt = damageDealt;
            RemainingStacks = remainingStacks;
        }
    }
}
