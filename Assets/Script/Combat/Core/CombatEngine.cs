using System;
using System.Collections.Generic;
using System.Linq;

namespace KiKs.Combat
{
    /// <summary>Validates commands, mutates BattleState, and emits ordered presentation events.</summary>
    public sealed class CombatEngine
    {
        public BattleState State { get; }
        public event Action<CombatEvent> EventRaised;

        public CombatEngine(BattleState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public CombatResult StartBattle()
        {
            if (State.Phase != CombatPhase.NotStarted) return Reject("Battle has already started.");
            var events = new List<CombatEvent>
            {
                new CombatEvent(CombatEventType.BattleStarted, message: "Battle started.")
            };
            BeginPlayerTurn(events);
            return Complete(true, string.Empty, events);
        }

        /// <summary>Spend mana to upgrade a card in hand without playing or discarding it.</summary>
        public CombatResult UpgradeCard(string cardInstanceId, string preferredUltimateTargetId = null)
        {
            if (State.Phase != CombatPhase.PlayerInput)
                return Reject("Cards can only be upgraded during player input.");

            var card = State.Deck.FindInHand(cardInstanceId);
            if (card == null) return Reject("The selected card is not in hand.");
            if (card.Spec.CostResource != CardResourceType.ActionPoint)
                return Reject("Magic cards cannot be upgraded.");
            if (!card.Spec.CanUpgrade) return Reject("This card has no upgraded values in JSON.");
            if (card.IsUpgraded) return Reject("This card instance is already upgraded.");
            if (!CanSpendMana(State.Rules.CardUpgradeManaCost))
                return Reject("Not enough mana or this turn's mana-spend limit has been reached.");

            var events = new List<CombatEvent>();
            SpendMana(State.Rules.CardUpgradeManaCost, events, "Mana spent to upgrade a card.");
            card.TryUpgrade();
            events.Add(new CombatEvent(
                CombatEventType.CardUpgraded,
                State.Player.Id,
                cardInstanceId: card.InstanceId,
                amount: State.Rules.CardUpgradeManaCost,
                message: "Card upgraded for this battle instance."));

            TryTriggerUltimate(preferredUltimateTargetId, events);
            EvaluateOutcome(events);
            return Complete(true, string.Empty, events);
        }

        public CombatResult PlayCard(string cardInstanceId, string targetId)
        {
            if (State.Phase != CombatPhase.PlayerInput)
                return Reject("Cards cannot be played during phase " + State.Phase + ".");

            var card = State.Deck.FindInHand(cardInstanceId);
            if (card == null) return Reject("The selected card is not in hand.");

            var target = ResolveTarget(card.Spec.TargetType, targetId);
            if (target == null) return Reject("The selected target is invalid.");
            if (target.IsDead) return Reject("The selected target is already dead.");

            if (card.Spec.CostResource == CardResourceType.ActionPoint)
            {
                if (State.Player.CurrentActionPoints < card.Spec.CostAmount)
                    return Reject("Not enough action points.");
            }
            else
            {
                if (State.Mana.MagicCardsPlayedThisTurn >= State.Rules.MagicCardsPerTurn)
                    return Reject("The magic-card limit for this turn has been reached.");
                if (!CanSpendMana(card.Spec.CostAmount))
                    return Reject("Not enough mana or this turn's mana-spend limit has been reached.");
            }

            var events = new List<CombatEvent>();
            SetPhase(CombatPhase.ResolvingCard, events);

            if (card.Spec.CostResource == CardResourceType.ActionPoint)
            {
                State.Player.TrySpendActionPoints(card.Spec.CostAmount);
                events.Add(new CombatEvent(
                    CombatEventType.ActionPointsChanged,
                    State.Player.Id,
                    amount: State.Player.CurrentActionPoints,
                    message: "Action points spent."));
            }
            else
            {
                SpendMana(card.Spec.CostAmount, events, "Mana spent to play a magic card.");
                State.Mana.RegisterMagicCardPlayed();
            }

            events.Add(new CombatEvent(
                CombatEventType.CardPlayed,
                State.Player.Id,
                target.Id,
                card.InstanceId,
                card.Spec.CostAmount,
                "Played " + card.Spec.DisplayName + (card.IsUpgraded ? " (upgraded)." : ".")));

            var toughnessBroken = ResolveEffects(card, target, events);
            card.ConsumeUpgrade();
            State.Deck.DiscardFromHand(card.InstanceId, out _);
            events.Add(new CombatEvent(
                CombatEventType.CardDiscarded,
                State.Player.Id,
                cardInstanceId: card.InstanceId,
                message: "Used card moved to discard pile."));

            if (card.Spec.CostResource == CardResourceType.Mana)
                TryTriggerUltimate(targetId, events);

            if (EvaluateOutcome(events)) return Complete(true, string.Empty, events);

            if (toughnessBroken && !target.IsDead)
            {
                State.PendingExecution = new PendingExecutionState(target.Id, card.InstanceId);
                SetPhase(CombatPhase.AwaitingExecutionConfirmation, events);
                events.Add(new CombatEvent(
                    CombatEventType.ExecutionConfirmationRequired,
                    State.Player.Id,
                    target.Id,
                    card.InstanceId,
                    message: "Execution confirmation is required."));
            }
            else
            {
                SetPhase(CombatPhase.PlayerInput, events);
            }

            return Complete(true, string.Empty, events);
        }

        public CombatResult ConfirmExecution()
        {
            if (State.Phase != CombatPhase.AwaitingExecutionConfirmation || State.PendingExecution == null)
                return Reject("There is no execution waiting for confirmation.");

            var pending = State.PendingExecution;
            var target = State.FindEnemy(pending.TargetId);
            if (target == null || target.IsDead)
            {
                State.PendingExecution = null;
                return Reject("The pending execution target is no longer valid.");
            }

            var events = new List<CombatEvent>();
            var executionDamage = ResolveExecution(target, events);
            events.Add(new CombatEvent(
                CombatEventType.ExecutionResolved,
                State.Player.Id,
                target.Id,
                pending.SourceCardInstanceId,
                executionDamage,
                "Execution damage resolved."));

            State.PendingExecution = null;
            if (target.IsDead)
            {
                events.Add(CreateDeathEvent(target));
            }
            else
            {
                var restored = target.RestoreToughness(State.Rules.GetToughnessRestoreAmount(target));
                events.Add(new CombatEvent(
                    CombatEventType.ToughnessChanged,
                    State.Player.Id,
                    target.Id,
                    amount: target.CurrentToughness,
                    message: "Restored " + restored + " toughness after execution."));
            }

            if (!EvaluateOutcome(events)) SetPhase(CombatPhase.PlayerInput, events);
            return Complete(true, string.Empty, events);
        }

        public CombatResult EndPlayerTurn()
        {
            if (State.Phase != CombatPhase.PlayerInput)
                return Reject("The player turn cannot end during phase " + State.Phase + ".");

            var events = new List<CombatEvent>();
            SetPhase(CombatPhase.PlayerTurnEnd, events);
            foreach (var card in State.Deck.DiscardHand())
            {
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded,
                    State.Player.Id,
                    cardInstanceId: card.InstanceId,
                    message: "Unplayed card discarded at turn end."));
            }

            State.IsCurrentEnemyTurnSkipped = State.Player.TryConsumeSkipEnemyTurn();
            SetPhase(CombatPhase.EnemyTurn, events);
            events.Add(new CombatEvent(CombatEventType.EnemyTurnStarted, message: "Enemy turn started."));
            if (State.IsCurrentEnemyTurnSkipped)
            {
                events.Add(new CombatEvent(
                    CombatEventType.EnemyActionSkipped,
                    State.Player.Id,
                    message: "The current enemy turn is skipped."));
            }

            return Complete(true, string.Empty, events);
        }

        public CombatResult ResolveEnemyAttack(string enemyId, int damage, int toughnessDamage = 0)
        {
            if (State.Phase != CombatPhase.EnemyTurn)
                return Reject("Enemy attacks can only resolve during the enemy turn.");
            if (damage < 0) return Reject("Enemy damage cannot be negative.");
            if (toughnessDamage < 0) return Reject("Enemy toughness damage cannot be negative.");

            var enemy = State.FindEnemy(enemyId);
            if (enemy == null || enemy.IsDead) return Reject("The attacking enemy is invalid.");

            var events = new List<CombatEvent>();
            if (State.IsCurrentEnemyTurnSkipped)
            {
                events.Add(new CombatEvent(
                    CombatEventType.EnemyActionSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: "Enemy turn was already marked as skipped."));
                return Complete(true, string.Empty, events);
            }

            if (enemy.ConsumeOneStunTurn())
            {
                events.Add(new CombatEvent(
                    CombatEventType.EnemyActionSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: "Enemy attack skipped because of stun."));
                return Complete(true, string.Empty, events);
            }

            if (State.Player.TryConsumeNullifyAttack())
            {
                events.Add(new CombatEvent(
                    CombatEventType.EnemyActionSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: "Enemy attack was nullified."));
                return Complete(true, string.Empty, events);
            }

            var reducedDamage = (int)Math.Ceiling(damage * (100 - State.Player.DamageReductionPercent) / 100d);
            var blockedDamage = State.Player.ConsumeBlockPoints(Math.Max(0, reducedDamage));
            var actualDamage = State.Player.ApplyDamage(Math.Max(0, reducedDamage - blockedDamage));
            events.Add(new CombatEvent(
                CombatEventType.DamageApplied,
                enemy.Id,
                State.Player.Id,
                amount: actualDamage,
                message: "Enemy attack resolved."));

            if (blockedDamage > 0)
            {
                events.Add(new CombatEvent(
                    CombatEventType.StatusApplied,
                    State.Player.Id,
                    State.Player.Id,
                    amount: State.Player.BlockPoints,
                    message: "Block absorbed " + blockedDamage + " damage."));
            }

            if (toughnessDamage > 0)
            {
                var actualToughnessDamage = State.Player.ReduceToughness(toughnessDamage);
                events.Add(new CombatEvent(
                    CombatEventType.ToughnessChanged,
                    enemy.Id,
                    State.Player.Id,
                    amount: State.Player.CurrentToughness,
                    message: "Enemy attack reduced player toughness by " +
                             actualToughnessDamage + "."));
            }

            if (State.Player.IsDead) events.Add(CreateDeathEvent(State.Player));

            var reflectedDamage = State.Player.ConsumeReflectDamage();
            if (reflectedDamage > 0 && !enemy.IsDead)
            {
                var actualReflectedDamage = enemy.ApplyDamage(reflectedDamage);
                events.Add(new CombatEvent(
                    CombatEventType.DamageApplied,
                    State.Player.Id,
                    enemy.Id,
                    amount: actualReflectedDamage,
                    message: "Player reflected damage to the attacker."));
                if (enemy.IsDead) events.Add(CreateDeathEvent(enemy));
            }

            EvaluateOutcome(events);
            return Complete(true, string.Empty, events);
        }

        public CombatResult CompleteEnemyTurn()
        {
            if (State.Phase != CombatPhase.EnemyTurn)
                return Reject("The current phase is not the enemy turn.");
            if (State.Outcome != BattleOutcome.None)
                return Reject("The battle has already ended.");

            var events = new List<CombatEvent>();
            BeginPlayerTurn(events);
            return Complete(true, string.Empty, events);
        }

        private bool ResolveEffects(CardInstance card, CombatantState target, List<CombatEvent> events)
        {
            foreach (var effect in card.Spec.Effects)
            {
                switch (effect.Type)
                {
                    case CardEffectType.Damage:
                        ResolveDamage(card, target, effect, events);
                        break;
                    case CardEffectType.ToughnessDamage:
                        if (ResolveToughnessDamage(card, target, effect, events)) return true;
                        break;
                    case CardEffectType.Stun:
                        var stunTurns = effect.DurationTurns.Resolve(card.IsUpgraded);
                        target.AddStun(stunTurns);
                        events.Add(new CombatEvent(
                            CombatEventType.StunApplied, State.Player.Id, target.Id,
                            card.InstanceId, stunTurns, "Stun applied."));
                        break;
                    case CardEffectType.NullifyAttacks:
                        var charges = effect.TriggerCount.Resolve(card.IsUpgraded);
                        State.Player.AddNullifyAttackCharges(charges);
                        AddStatusEvent(card, State.Player, charges, "Attack-nullify charges added.", events);
                        break;
                    case CardEffectType.DamageReduction:
                        var reduction = effect.Amount.Resolve(card.IsUpgraded);
                        var reductionTurns = effect.DurationTurns.Resolve(card.IsUpgraded);
                        State.Player.AddDamageReduction(reduction, reductionTurns);
                        AddStatusEvent(card, State.Player, reduction, "Damage reduction applied.", events);
                        break;
                    case CardEffectType.SkipEnemyTurns:
                        var skippedTurns = effect.DurationTurns.Resolve(card.IsUpgraded);
                        State.Player.AddSkipEnemyTurns(skippedTurns);
                        AddStatusEvent(card, State.Player, skippedTurns, "Enemy-turn skip applied.", events);
                        break;
                    case CardEffectType.DrawCards:
                        DrawCards(effect.Amount.Resolve(card.IsUpgraded), events);
                        break;
                    case CardEffectType.LifeStealMaxHealth:
                        ResolveLifeSteal(card, target, effect, events);
                        break;

                    case CardEffectType.Bleed:
                        var bleedStacks = effect.Amount.Resolve(card.IsUpgraded);
                        if (bleedStacks == 0)
                            bleedStacks = effect.DamagePerTurn.Resolve(card.IsUpgraded);
                        target.AddBleedStacks(bleedStacks);
                        AddStatusEvent(card, target, target.BleedStacks, "Bleed stacks applied.", events);
                        break;
                    case CardEffectType.BleedScaledDamage:
                        ResolveBleedScaledDamage(card, target, effect, events);
                        break;
                    case CardEffectType.LifeSteal:
                        ResolveFixedLifeSteal(card, target, effect, events);
                        break;
                    case CardEffectType.ReflectDamage:
                        var reflectDamage = effect.Amount.Resolve(card.IsUpgraded);
                        State.Player.AddReflectDamage(reflectDamage);
                        AddStatusEvent(card, State.Player, State.Player.PendingReflectDamage,
                            "Reflect damage prepared.", events);
                        break;
                    case CardEffectType.BlockDamage:
                        var blockPoints = effect.Amount.Resolve(card.IsUpgraded);
                        State.Player.AddBlockPoints(blockPoints);
                        AddStatusEvent(card, State.Player, State.Player.BlockPoints,
                            "Block points gained.", events);
                        break;
                    case CardEffectType.GainResource:
                        ResolveGainResource(card, effect, events);
                        break;

                    case CardEffectType.Poison:
                    case CardEffectType.Vulnerability:
                    case CardEffectType.Immunity:
                    case CardEffectType.SummonCompanion:
                    case CardEffectType.PlayCardsFromDiscard:
                        events.Add(new CombatEvent(
                            CombatEventType.EffectNotImplemented,
                            State.Player.Id,
                            target.Id,
                            card.InstanceId,
                            message: effect.Type + " is parsed but awaits its dedicated rule."));
                        break;
                }

                if (target.IsDead) break;
            }

            return false;
        }

        private void ResolveDamage(CardInstance card, CombatantState target, CardEffectSpec effect, List<CombatEvent> events)
        {
            var amount = effect.Amount.Resolve(card.IsUpgraded);
            var hits = effect.Hits.Resolve(card.IsUpgraded);
            for (var hit = 0; hit < hits && !target.IsDead; hit++)
            {
                var actualDamage = target.ApplyDamage(amount);
                events.Add(new CombatEvent(
                    CombatEventType.DamageApplied, State.Player.Id, target.Id,
                    card.InstanceId, actualDamage, "Damage hit " + (hit + 1) + " resolved."));
                if (target.IsDead) events.Add(CreateDeathEvent(target));
            }
        }

        private bool ResolveToughnessDamage(
            CardInstance card, CombatantState target, CardEffectSpec effect, List<CombatEvent> events)
        {
            var rawAmount = effect.Amount.Resolve(card.IsUpgraded);
            var amount = effect.Unit == ValueUnit.Percent
                ? (int)Math.Ceiling(target.MaxToughness * rawAmount / 100d)
                : rawAmount;
            var hits = effect.Hits.Resolve(card.IsUpgraded);

            for (var hit = 0; hit < hits; hit++)
            {
                var hadToughness = target.CurrentToughness > 0;
                var changed = target.ReduceToughness(amount);
                events.Add(new CombatEvent(
                    CombatEventType.ToughnessChanged, State.Player.Id, target.Id,
                    card.InstanceId, target.CurrentToughness,
                    "Toughness hit " + (hit + 1) + " reduced " + changed + "."));

                if (target.Side == CombatantSide.Enemy && hadToughness && target.CurrentToughness == 0)
                {
                    events.Add(new CombatEvent(
                        CombatEventType.ToughnessBroken, State.Player.Id, target.Id,
                        card.InstanceId, message: "Target toughness was broken."));
                    var restoredMana = State.Mana.RestoreToMaximum();
                    if (restoredMana > 0)
                    {
                        events.Add(new CombatEvent(
                            CombatEventType.ManaChanged, State.Player.Id, target.Id,
                            card.InstanceId, State.Mana.Current,
                            "Enemy toughness broken; restored " + restoredMana + " mana."));
                    }
                    return true;
                }
            }

            return false;
        }

        private void ResolveLifeSteal(
            CardInstance card, CombatantState target, CardEffectSpec effect, List<CombatEvent> events)
        {
            var percent = target.EnemyRank == EnemyRank.Boss ? effect.BossPercent : effect.NormalTargetPercent;
            var requested = (int)Math.Ceiling(target.MaxHealth * percent / 100d);
            var damage = target.ApplyDamage(requested);
            var healing = State.Player.Heal(damage);
            events.Add(new CombatEvent(
                CombatEventType.DamageApplied, State.Player.Id, target.Id,
                card.InstanceId, damage, "Life steal dealt damage."));
            events.Add(new CombatEvent(
                CombatEventType.HealingApplied, State.Player.Id, State.Player.Id,
                card.InstanceId, healing, "Life steal healed the player."));
            if (target.IsDead) events.Add(CreateDeathEvent(target));
        }

        private void ResolveBleedScaledDamage(
            CardInstance card, CombatantState target, CardEffectSpec effect, List<CombatEvent> events)
        {
            var requestedDamage = (int)Math.Ceiling(target.BleedStacks * effect.Multiplier);
            var actualDamage = target.ApplyDamage(requestedDamage);
            events.Add(new CombatEvent(
                CombatEventType.DamageApplied, State.Player.Id, target.Id,
                card.InstanceId, actualDamage,
                "Bleed-scaled damage resolved from " + target.BleedStacks + " stacks."));
            if (target.IsDead) events.Add(CreateDeathEvent(target));
        }

        private void ResolveFixedLifeSteal(
            CardInstance card, CombatantState target, CardEffectSpec effect, List<CombatEvent> events)
        {
            var requestedDamage = effect.Amount.Resolve(card.IsUpgraded);
            var actualDamage = target.ApplyDamage(requestedDamage);
            var actualHealing = State.Player.Heal(actualDamage);
            events.Add(new CombatEvent(
                CombatEventType.DamageApplied, State.Player.Id, target.Id,
                card.InstanceId, actualDamage, "Life steal dealt fixed damage."));
            events.Add(new CombatEvent(
                CombatEventType.HealingApplied, State.Player.Id, State.Player.Id,
                card.InstanceId, actualHealing, "Life steal healed fixed health."));
            if (target.IsDead) events.Add(CreateDeathEvent(target));
        }

        private void ResolveGainResource(CardInstance card, CardEffectSpec effect, List<CombatEvent> events)
        {
            var amount = effect.Amount.Resolve(card.IsUpgraded);
            if (effect.Resource == CardResourceType.ActionPoint)
            {
                State.Player.AddActionPoints(amount);
                events.Add(new CombatEvent(
                    CombatEventType.ActionPointsChanged, State.Player.Id,
                    cardInstanceId: card.InstanceId, amount: State.Player.CurrentActionPoints,
                    message: "Action points gained."));
            }
            else
            {
                events.Add(new CombatEvent(
                    CombatEventType.ManaChanged, State.Player.Id,
                    cardInstanceId: card.InstanceId, amount: State.Mana.Current,
                    message: "Mana gain ignored; mana is restored only by breaking enemy toughness."));
            }
        }

        private void DrawCards(int count, List<CombatEvent> events)
        {
            var result = State.Deck.Draw(count, State.Rules.HandLimit);
            for (var i = 0; i < result.ReshuffleCount; i++)
                events.Add(new CombatEvent(CombatEventType.DeckReshuffled, message: "Discard pile reshuffled."));

            foreach (var drawn in result.DrawnCards)
            {
                events.Add(new CombatEvent(
                    CombatEventType.CardDrawn, State.Player.Id,
                    cardInstanceId: drawn.InstanceId, message: "Drew " + drawn.Spec.DisplayName + "."));
            }

            foreach (var overflow in result.OverflowDiscardedCards)
            {
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded, State.Player.Id,
                    cardInstanceId: overflow.InstanceId,
                    message: "Card exceeded the hand limit and was discarded."));
            }
        }

        private static void AddStatusEvent(
            CardInstance card, CombatantState target, int amount, string message, List<CombatEvent> events)
        {
            events.Add(new CombatEvent(
                CombatEventType.StatusApplied,
                cardInstanceId: card.InstanceId,
                targetId: target.Id,
                amount: amount,
                message: message));
        }

        private bool CanSpendMana(int amount)
        {
            return State.Mana.CanSpend(amount, State.Rules.MaximumManaSpendPerTurn);
        }

        private void SpendMana(int amount, List<CombatEvent> events, string message)
        {
            State.Mana.TrySpend(amount, State.Rules.MaximumManaSpendPerTurn);
            events.Add(new CombatEvent(
                CombatEventType.ManaChanged, State.Player.Id,
                amount: State.Mana.Current, message: message));
        }

        private void TryTriggerUltimate(string preferredTargetId, List<CombatEvent> events)
        {
            if (!State.Mana.ConsumeUltimateThreshold(State.Rules.UltimateManaThreshold)) return;

            var target = State.FindEnemy(preferredTargetId);
            if (target == null || target.IsDead) target = State.FindFirstLivingEnemy();

            events.Add(new CombatEvent(
                CombatEventType.UltimateTriggered, State.Player.Id, target?.Id,
                amount: State.Rules.UltimateDamage,
                message: "Mana threshold reached; ultimate triggered automatically."));

            if (target != null)
            {
                var damage = target.ApplyDamage(State.Rules.UltimateDamage);
                events.Add(new CombatEvent(
                    CombatEventType.DamageApplied, State.Player.Id, target.Id,
                    amount: damage, message: "Ultimate damage resolved."));

                if (!target.IsDead && State.Rules.UltimateStunTurns > 0)
                {
                    target.AddStun(State.Rules.UltimateStunTurns);
                    events.Add(new CombatEvent(
                        CombatEventType.StunApplied, State.Player.Id, target.Id,
                        amount: State.Rules.UltimateStunTurns, message: "Ultimate stun applied."));
                }

                if (target.IsDead) events.Add(CreateDeathEvent(target));
            }

        }

        private void BeginPlayerTurn(List<CombatEvent> events)
        {
            State.TurnNumber++;
            State.IsCurrentEnemyTurnSkipped = false;
            State.Mana.BeginTurn();
            State.Player.AdvancePlayerTurnStatuses();

            SetPhase(CombatPhase.PlayerTurnStart, events);
            events.Add(new CombatEvent(
                CombatEventType.TurnStarted, State.Player.Id,
                amount: State.TurnNumber, message: "Player turn " + State.TurnNumber + " started."));
            if (EvaluateOutcome(events)) return;

            State.Player.RestoreActionPoints(State.Rules.BaseActionPoints);
            events.Add(new CombatEvent(
                CombatEventType.ActionPointsChanged, State.Player.Id,
                amount: State.Player.CurrentActionPoints, message: "Action points restored for the turn."));
            events.Add(new CombatEvent(
                CombatEventType.ManaChanged, State.Player.Id,
                amount: State.Mana.Current, message: "Mana spend allowance reset for the turn."));

            DrawCards(State.Rules.CardsDrawnPerTurn, events);
            SetPhase(CombatPhase.PlayerInput, events);
        }

        private CombatantState ResolveTarget(CardTargetType targetType, string targetId)
        {
            return targetType == CardTargetType.Self ? State.Player :
                   targetType == CardTargetType.SingleEnemy ? State.FindEnemy(targetId) : null;
        }

        private int ResolveExecution(CombatantState target, List<CombatEvent> events)
        {
            int actualDamage;
            switch (target.EnemyRank)
            {
                case EnemyRank.Minion:
                    actualDamage = target.Kill();
                    break;
                case EnemyRank.Elite:
                    actualDamage = target.ApplyDamage(State.Rules.EliteExecutionDamage);
                    if (!target.IsDead && State.Rules.EliteStunTurns > 0)
                    {
                        target.AddStun(State.Rules.EliteStunTurns);
                        events.Add(new CombatEvent(
                            CombatEventType.StunApplied, State.Player.Id, target.Id,
                            amount: State.Rules.EliteStunTurns, message: "Elite was stunned by execution."));
                    }
                    break;
                case EnemyRank.Boss:
                    actualDamage = target.ApplyDamage(State.Rules.BossExecutionDamage);
                    break;
                default:
                    actualDamage = 0;
                    break;
            }

            if (actualDamage > 0)
            {
                events.Add(new CombatEvent(
                    CombatEventType.DamageApplied, State.Player.Id, target.Id,
                    amount: actualDamage, message: "Execution dealt an additional damage instance."));
            }

            return actualDamage;
        }

        private bool EvaluateOutcome(List<CombatEvent> events)
        {
            if (State.Player.IsDead)
            {
                State.Outcome = BattleOutcome.Defeat;
                SetPhase(CombatPhase.Defeat, events);
                events.Add(new CombatEvent(
                    CombatEventType.Defeat, targetId: State.Player.Id, message: "Player defeated."));
                return true;
            }

            if (State.Enemies.All(enemy => enemy.IsDead))
            {
                State.Outcome = BattleOutcome.Victory;
                SetPhase(CombatPhase.Victory, events);
                events.Add(new CombatEvent(CombatEventType.Victory, message: "All enemies defeated."));
                return true;
            }

            return false;
        }

        private static CombatEvent CreateDeathEvent(CombatantState target)
        {
            return new CombatEvent(
                CombatEventType.CombatantDied,
                targetId: target.Id,
                message: target.DisplayName + " died.");
        }

        private void SetPhase(CombatPhase phase, List<CombatEvent> events)
        {
            State.Phase = phase;
            events.Add(new CombatEvent(
                CombatEventType.PhaseChanged,
                amount: (int)phase,
                message: "Phase changed to " + phase + "."));
        }

        private CombatResult Reject(string message)
        {
            return Complete(false, message, new List<CombatEvent>
            {
                new CombatEvent(CombatEventType.ActionRejected, message: message)
            });
        }

        private CombatResult Complete(bool success, string message, List<CombatEvent> events)
        {
            var handler = EventRaised;
            if (handler != null)
            {
                foreach (var combatEvent in events) handler.Invoke(combatEvent);
            }

            return new CombatResult(success, message, events);
        }
    }
}
