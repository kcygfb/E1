using System;
using System.Collections.Generic;
using System.Linq;

namespace KiKs.Combat
{
    public enum CombatActionOrigin
    {
        PlayerInput,
        EnemyAI,
        System
    }

    public enum CombatCardSource
    {
        Hand,
        Special
    }

    /// <summary>
    /// Side-agnostic request to play one card. Player input and enemy AI create the
    /// same intent; CombatEngine validates it and routes it through CombatFlowController.
    /// </summary>
    public sealed class CombatActionIntent
    {
        public string ActorId { get; }
        public string CardInstanceId { get; }
        public string TargetId { get; }
        public CombatActionOrigin Origin { get; }
        public CombatCardSource CardSource { get; }

        public CombatActionIntent(
            string actorId,
            string cardInstanceId,
            string targetId,
            CombatActionOrigin origin,
            CombatCardSource cardSource = CombatCardSource.Hand)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException("Actor id is required.", nameof(actorId));
            if (string.IsNullOrWhiteSpace(cardInstanceId))
                throw new ArgumentException("Card instance id is required.", nameof(cardInstanceId));

            ActorId = actorId;
            CardInstanceId = cardInstanceId;
            TargetId = targetId;
            Origin = origin;
            CardSource = cardSource;
        }
    }

    /// <summary>Summary returned by one pass through the shared combat flow.</summary>
    public sealed class CombatFlowResult
    {
        public bool WasNullified { get; internal set; }
        public bool ToughnessBroken { get; internal set; }
        public int TotalDamage { get; internal set; }
        public bool SourceDied { get; internal set; }
        public bool TargetDied { get; internal set; }
    }

    /// <summary>
    /// Shared effect pipeline for both sides. Every card effect and direct damage packet
    /// travels source -> flow -> target, so mitigation, block, reflection, death and events
    /// are resolved once instead of being duplicated by player and enemy code paths.
    /// </summary>
    public sealed class CombatFlowController
    {
        private readonly BattleState _state;

        public CombatFlowController(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public CombatFlowResult ResolveCard(
            CombatantState source,
            CombatantState target,
            CardInstance card,
            DeckState sourceDeck,
            int handLimit,
            bool allowExecution,
            List<CombatEvent> events)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (events == null) throw new ArgumentNullException(nameof(events));

            var result = new CombatFlowResult();
            var hostile = source.Side != target.Side && IsHostileCard(card);

            if (hostile && target.TryConsumeNullifyAttack())
            {
                result.WasNullified = true;
                events.Add(new CombatEvent(
                    CombatEventType.ActionNullified,
                    source.Id,
                    target.Id,
                    card.InstanceId,
                    message: source.DisplayName + "'s card was nullified."));
                return result;
            }

            foreach (var effect in card.Spec.Effects)
            {
                ResolveEffect(source, target, card, sourceDeck, handLimit, effect, result, events);
                if (target.IsDead || source.IsDead) break;
            }

            if (result.ToughnessBroken && allowExecution && !target.IsDead)
                ResolveExecution(source, target, card.InstanceId, result, events);

            if (hostile && !result.WasNullified && !source.IsDead)
                ResolveReflection(source, target, card.InstanceId, result, events);

            result.SourceDied = source.IsDead;
            result.TargetDied = target.IsDead;
            return result;
        }

        public CombatFlowResult ResolveDirectAttack(
            CombatantState source,
            CombatantState target,
            int damage,
            int toughnessDamage,
            string sourceActionId,
            List<CombatEvent> events)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (toughnessDamage < 0) throw new ArgumentOutOfRangeException(nameof(toughnessDamage));

            var result = new CombatFlowResult();
            if (source.Side != target.Side && target.TryConsumeNullifyAttack())
            {
                result.WasNullified = true;
                events.Add(new CombatEvent(
                    CombatEventType.ActionNullified,
                    source.Id,
                    target.Id,
                    sourceActionId,
                    message: source.DisplayName + "'s attack was nullified."));
                return result;
            }

            result.TotalDamage += ApplyDamage(
                source,
                target,
                damage,
                sourceActionId,
                "Direct attack damage resolved.",
                true,
                events);

            if (!target.IsDead && toughnessDamage > 0)
            {
                result.ToughnessBroken |= ApplyToughnessDamage(
                    source,
                    target,
                    toughnessDamage,
                    1,
                    ValueUnit.Points,
                    sourceActionId,
                    events);
            }

            if (!source.IsDead)
                ResolveReflection(source, target, sourceActionId, result, events);

            result.SourceDied = source.IsDead;
            result.TargetDied = target.IsDead;
            return result;
        }

        public int ResolveDirectDamage(
            CombatantState source,
            CombatantState target,
            int amount,
            string sourceActionId,
            string message,
            bool applyMitigation,
            List<CombatEvent> events)
        {
            return ApplyDamage(
                source,
                target,
                amount,
                sourceActionId,
                message,
                applyMitigation,
                events);
        }

        public DeckDrawResult DrawCards(
            CombatantState source,
            DeckState deck,
            int count,
            int handLimit,
            List<CombatEvent> events)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (deck == null) return null;
            if (events == null) throw new ArgumentNullException(nameof(events));

            var result = deck.Draw(count, handLimit);
            for (var i = 0; i < result.ReshuffleCount; i++)
            {
                events.Add(new CombatEvent(
                    CombatEventType.DeckReshuffled,
                    source.Id,
                    message: source.DisplayName + "'s discard pile was reshuffled."));
            }

            foreach (var drawn in result.DrawnCards)
            {
                events.Add(new CombatEvent(
                    CombatEventType.CardDrawn,
                    source.Id,
                    cardInstanceId: drawn.InstanceId,
                    message: source.DisplayName + " drew " + drawn.Spec.DisplayName + "."));
            }

            foreach (var overflow in result.OverflowDiscardedCards)
            {
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded,
                    source.Id,
                    cardInstanceId: overflow.InstanceId,
                    message: source.DisplayName + "'s hand limit discarded " +
                             overflow.Spec.DisplayName + "."));
            }

            return result;
        }

        public void ProcessStatusTicks(
            CombatantState target,
            string sourceId,
            List<CombatEvent> events)
        {
            if (target == null || target.IsDead) return;
            if (events == null) throw new ArgumentNullException(nameof(events));

            foreach (var tick in target.ProcessStatusTicks())
            {
                if (tick.DamageDealt <= 0) continue;

                var actualDamage = ApplyDamage(
                    null,
                    target,
                    tick.DamageDealt,
                    null,
                    tick.Type + " dealt " + tick.DamageDealt + " damage (" +
                    tick.RemainingStacks + " stacks remaining).",
                    false,
                    events,
                    sourceId,
                    CombatEventType.StatusTicked);

                if (actualDamage > 0 && target.IsDead) break;
            }
        }

        private void ResolveEffect(
            CombatantState source,
            CombatantState target,
            CardInstance card,
            DeckState sourceDeck,
            int handLimit,
            CardEffectSpec effect,
            CombatFlowResult result,
            List<CombatEvent> events)
        {
            switch (effect.Type)
            {
                case CardEffectType.Damage:
                    result.TotalDamage += ResolveDamageEffect(source, target, card, effect, events);
                    break;

                case CardEffectType.ToughnessDamage:
                    result.ToughnessBroken |= ApplyToughnessDamage(
                        source,
                        target,
                        effect.Amount.Resolve(card.IsUpgraded),
                        effect.Hits.Resolve(card.IsUpgraded),
                        effect.Unit,
                        card.InstanceId,
                        events);
                    break;

                case CardEffectType.Stun:
                    target.AddStun(Math.Max(1, effect.Amount.Resolve(card.IsUpgraded)));
                    events.Add(new CombatEvent(
                        CombatEventType.StunApplied,
                        source.Id,
                        target.Id,
                        card.InstanceId,
                        target.StunTurns,
                        "Stun applied."));
                    break;

                case CardEffectType.Bleed:
                    var bleedStacks = effect.Amount.Resolve(card.IsUpgraded);
                    target.AddBleedStacks(bleedStacks);
                    AddStatusEvent(source, target, card, target.BleedStacks, "Bleed stacks applied.", events);
                    break;

                case CardEffectType.Poison:
                    var poisonStacks = effect.Amount.Resolve(card.IsUpgraded);
                    target.AddPoisonStacks(poisonStacks);
                    AddStatusEvent(source, target, card, target.PoisonStacks, "Poison stacks applied.", events);
                    break;

                case CardEffectType.NullifyAttacks:
                    var nullifyCharges = effect.Amount.Resolve(card.IsUpgraded);
                    source.AddNullifyAttackCharges(nullifyCharges);
                    AddStatusEvent(source, source, card, source.NullifyAttackCharges,
                        "Attack-nullify charges added.", events);
                    break;

                case CardEffectType.DamageReduction:
                    var reduction = effect.Amount.Resolve(card.IsUpgraded);
                    source.AddDamageReduction(reduction, 1);
                    AddStatusEvent(source, source, card, reduction, "Damage reduction applied.", events);
                    break;

                case CardEffectType.SkipEnemyTurns:
                    source.AddSkipEnemyTurns(Math.Max(1, effect.Amount.Resolve(card.IsUpgraded)));
                    AddStatusEvent(source, source, card, source.SkipEnemyTurns,
                        "Opponent-turn skip applied.", events);
                    break;

                case CardEffectType.DrawCards:
                    DrawCards(
                        source,
                        sourceDeck,
                        effect.Amount.Resolve(card.IsUpgraded),
                        handLimit,
                        events);
                    break;

                case CardEffectType.LifeStealMaxHealth:
                    ResolveLifeSteal(source, target, card, effect, true, result, events);
                    break;

                case CardEffectType.BleedScaledDamage:
                    var requested = (int)Math.Ceiling(target.BleedStacks * effect.Multiplier);
                    result.TotalDamage += ApplyDamage(
                        source,
                        target,
                        requested,
                        card.InstanceId,
                        "Bleed-scaled damage resolved from " + target.BleedStacks + " stacks.",
                        true,
                        events,
                        isUpgraded: card.IsUpgraded);
                    target.ClearBleedStacks();
                    break;

                case CardEffectType.LifeSteal:
                    ResolveLifeSteal(source, target, card, effect, false, result, events);
                    break;

                case CardEffectType.ReflectDamage:
                    source.AddReflectDamage(effect.Amount.Resolve(card.IsUpgraded));
                    AddStatusEvent(source, source, card, source.PendingReflectDamage,
                        "Reflect damage prepared.", events);
                    break;

                case CardEffectType.BlockDamage:
                    source.AddBlockPoints(effect.Amount.Resolve(card.IsUpgraded));
                    AddStatusEvent(source, source, card, source.BlockPoints,
                        "Block points gained.", events);
                    break;

                case CardEffectType.GainResource:
                    source.AddActionPoints(effect.Amount.Resolve(card.IsUpgraded));
                    events.Add(new CombatEvent(
                        CombatEventType.ActionPointsChanged,
                        source.Id,
                        cardInstanceId: card.InstanceId,
                        amount: source.CurrentActionPoints,
                        message: "Action points gained."));
                    break;

                case CardEffectType.Vulnerability:
                case CardEffectType.Immunity:
                case CardEffectType.SummonCompanion:
                case CardEffectType.PlayCardsFromDiscard:
                    events.Add(new CombatEvent(
                        CombatEventType.EffectNotImplemented,
                        source.Id,
                        target.Id,
                        card.InstanceId,
                        message: effect.Type + " is parsed but awaits its dedicated rule."));
                    break;
            }
        }

        private int ResolveDamageEffect(
            CombatantState source,
            CombatantState target,
            CardInstance card,
            CardEffectSpec effect,
            List<CombatEvent> events)
        {
            var totalDamage = 0;
            var amount = effect.Amount.Resolve(card.IsUpgraded);
            var hits = effect.Hits.Resolve(card.IsUpgraded);

            for (var hit = 0; hit < hits && !target.IsDead; hit++)
            {
                totalDamage += ApplyDamage(
                    source,
                    target,
                    amount,
                    card.InstanceId,
                    "Damage hit " + (hit + 1) + " resolved.",
                    true,
                    events,
                    isUpgraded: card.IsUpgraded);
            }

            return totalDamage;
        }

        private void ResolveLifeSteal(
            CombatantState source,
            CombatantState target,
            CardInstance card,
            CardEffectSpec effect,
            bool percentOfMaxHealth,
            CombatFlowResult result,
            List<CombatEvent> events)
        {
            var value = effect.Amount.Resolve(card.IsUpgraded);
            var requestedDamage = percentOfMaxHealth
                ? (int)Math.Ceiling(target.MaxHealth * value / 100d)
                : value;
            var actualDamage = ApplyDamage(
                source,
                target,
                requestedDamage,
                card.InstanceId,
                "Life steal dealt damage.",
                true,
                events,
                isUpgraded: card.IsUpgraded);
            result.TotalDamage += actualDamage;

            var healing = source.Heal(actualDamage);
            events.Add(new CombatEvent(
                CombatEventType.HealingApplied,
                source.Id,
                source.Id,
                card.InstanceId,
                healing,
                "Life steal healed " + source.DisplayName + "."));
        }

        private bool ApplyToughnessDamage(
            CombatantState source,
            CombatantState target,
            int rawAmount,
            int hits,
            ValueUnit unit,
            string sourceActionId,
            List<CombatEvent> events)
        {
            var amount = unit == ValueUnit.Percent
                ? (int)Math.Ceiling(target.MaxToughness * rawAmount / 100d)
                : rawAmount;
            var broken = false;

            for (var hit = 0; hit < hits && !target.IsDead; hit++)
            {
                var hadToughness = target.CurrentToughness > 0;
                var changed = target.ReduceToughness(amount);
                events.Add(new CombatEvent(
                    CombatEventType.ToughnessChanged,
                    source?.Id,
                    target.Id,
                    sourceActionId,
                    target.CurrentToughness,
                    "Toughness hit " + (hit + 1) + " reduced " + changed + "."));

                if (!hadToughness || target.CurrentToughness != 0) continue;

                broken = true;
                events.Add(new CombatEvent(
                    CombatEventType.ToughnessBroken,
                    source?.Id,
                    target.Id,
                    sourceActionId,
                    message: target.DisplayName + "'s toughness was broken."));
            }

            return broken;
        }

        private void ResolveExecution(
            CombatantState source,
            CombatantState target,
            string sourceActionId,
            CombatFlowResult result,
            List<CombatEvent> events)
        {
            var actualDamage = ApplyDamage(
                source,
                target,
                _state.Rules.ExecutionDamage,
                sourceActionId,
                "Execution damage resolved.",
                true,
                events);
            result.TotalDamage += actualDamage;

            events.Add(new CombatEvent(
                CombatEventType.ExecutionResolved,
                source.Id,
                target.Id,
                sourceActionId,
                actualDamage,
                "Execution resolved automatically."));

            if (target.IsDead) return;

            var restored = target.RestoreToughness(_state.Rules.GetToughnessRestoreAmount(target));
            events.Add(new CombatEvent(
                CombatEventType.ToughnessChanged,
                source.Id,
                target.Id,
                sourceActionId,
                target.CurrentToughness,
                "Restored " + restored + " toughness after execution."));
        }

        private void ResolveReflection(
            CombatantState source,
            CombatantState target,
            string sourceActionId,
            CombatFlowResult result,
            List<CombatEvent> events)
        {
            if (target == null || source == null || source.Side == target.Side) return;

            var reflectedDamage = target.ConsumeReflectDamage();
            if (reflectedDamage <= 0 || source.IsDead) return;

            ApplyDamage(
                target,
                source,
                reflectedDamage,
                sourceActionId,
                "Reflected damage resolved.",
                false,
                events);
            result.SourceDied = source.IsDead;
        }

        private int ApplyDamage(
            CombatantState source,
            CombatantState target,
            int requestedDamage,
            string sourceActionId,
            string message,
            bool applyMitigation,
            List<CombatEvent> events,
            string sourceIdOverride = null,
            CombatEventType eventType = CombatEventType.DamageApplied,
            bool isUpgraded = false)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (requestedDamage < 0) throw new ArgumentOutOfRangeException(nameof(requestedDamage));

            var damageAfterReduction = requestedDamage;
            var blockedDamage = 0;

            if (applyMitigation)
            {
                damageAfterReduction = (int)Math.Ceiling(
                    requestedDamage * (100 - target.DamageReductionPercent) / 100d);
                blockedDamage = target.ConsumeBlockPoints(Math.Max(0, damageAfterReduction));
            }

            var wasDead = target.IsDead;
            var actualDamage = target.ApplyDamage(Math.Max(0, damageAfterReduction - blockedDamage));
            var sourceId = sourceIdOverride ?? source?.Id;

            events.Add(new CombatEvent(
                eventType,
                sourceId,
                target.Id,
                sourceActionId,
                actualDamage,
                message,
                isUpgraded: isUpgraded));

            if (blockedDamage > 0)
            {
                events.Add(new CombatEvent(
                    CombatEventType.StatusApplied,
                    target.Id,
                    target.Id,
                    sourceActionId,
                    target.BlockPoints,
                    "Block absorbed " + blockedDamage + " damage."));
            }

            if (!wasDead && target.IsDead)
                events.Add(CreateDeathEvent(target));

            return actualDamage;
        }

        private static bool IsHostileCard(CardInstance card)
        {
            return card.Spec.Effects.Any(effect =>
                effect.Type == CardEffectType.Damage ||
                effect.Type == CardEffectType.ToughnessDamage ||
                effect.Type == CardEffectType.Stun ||
                effect.Type == CardEffectType.Vulnerability ||
                effect.Type == CardEffectType.Bleed ||
                effect.Type == CardEffectType.Poison ||
                effect.Type == CardEffectType.BleedScaledDamage ||
                effect.Type == CardEffectType.LifeSteal ||
                effect.Type == CardEffectType.LifeStealMaxHealth);
        }

        private static void AddStatusEvent(
            CombatantState source,
            CombatantState target,
            CardInstance card,
            int amount,
            string message,
            List<CombatEvent> events)
        {
            events.Add(new CombatEvent(
                CombatEventType.StatusApplied,
                source.Id,
                target.Id,
                card.InstanceId,
                amount,
                message));
        }

        private static CombatEvent CreateDeathEvent(CombatantState target)
        {
            return new CombatEvent(
                CombatEventType.CombatantDied,
                targetId: target.Id,
                message: target.DisplayName + " died.");
        }
    }
}
