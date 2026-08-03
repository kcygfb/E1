using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>Validates commands, mutates BattleState, and emits ordered presentation events.</summary>
    public sealed class CombatEngine
    {
        private readonly Dictionary<string, int> _enemyCardsPlayedThisTurn = new();
        private readonly Dictionary<string, Queue<int>> _enemyRecentCardCosts = new();
        private readonly HashSet<string> _usedEnemySpecialCards = new();
        private readonly HashSet<string> _enemyActorsAuthorizedThisTurn = new();
        private readonly HashSet<string> _enemyActorsBlockedThisTurn = new();
        private readonly Dictionary<string, int> _gunCardShotsRemaining = new();
        private readonly CombatFlowController _flow;

        public BattleState State { get; }
        public event Action<CombatEvent> EventRaised;

        public CombatEngine(BattleState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _flow = new CombatFlowController(State);
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
            return SubmitCardAction(new CombatActionIntent(
                State.Player.Id,
                cardInstanceId,
                targetId,
                CombatActionOrigin.PlayerInput));
        }

        /// <summary>
        /// Unified card entry point. Player input and enemy AI submit the same intent;
        /// only the actor, target direction and resource policy differ.
        /// </summary>
        public CombatResult SubmitCardAction(CombatActionIntent intent)
        {
            if (intent == null) return Reject("Combat action intent is required.");

            var source = State.FindCombatant(intent.ActorId);
            if (source == null || source.IsDead)
                return Reject("The acting combatant is invalid or dead.");

            if (source.Side == CombatantSide.Player)
            {
                if (intent.Origin == CombatActionOrigin.EnemyAI)
                    return Reject("Enemy AI cannot control the player.");
                if (State.Phase != CombatPhase.PlayerInput)
                    return Reject("Player cards cannot be played during phase " + State.Phase + ".");
                if (intent.CardSource == CombatCardSource.Special)
                    return Reject("Player special-card source is not configured.");
            }
            else
            {
                if (intent.Origin == CombatActionOrigin.PlayerInput)
                    return Reject("Player input cannot control an enemy.");
                if (State.Phase != CombatPhase.EnemyTurn)
                    return Reject("Enemy cards can only be played during the enemy turn.");
                if (!TryAuthorizeEnemyTurn(source, out var skippedEvent))
                {
                    var skippedEvents = new List<CombatEvent>();
                    if (skippedEvent != null) skippedEvents.Add(skippedEvent);
                    return Complete(false, skippedEvent?.Message ?? "Enemy cannot act.", skippedEvents);
                }
            }

            var sourceDeck = State.GetDeck(source.Id);
            CardInstance card;
            if (intent.CardSource == CombatCardSource.Special)
            {
                card = State.GetEnemySpecialCard(source.Id);
                if (card == null || card.InstanceId != intent.CardInstanceId)
                    return Reject("The selected special card is unavailable.");
                if (_usedEnemySpecialCards.Contains(card.InstanceId))
                    return Reject("The special card was already used.");

                var specialRules = State.Rules.GetEnemyTurnRules(source.EnemyRank);
                if (specialRules.BerserkTurn <= 0 || State.TurnNumber != specialRules.BerserkTurn)
                    return Reject("The special card is not available on this turn.");
            }
            else
            {
                if (sourceDeck == null)
                    return Reject(source.DisplayName + " has no deck.");
                card = sourceDeck.FindInHand(intent.CardInstanceId);
                if (card == null)
                    return Reject("The selected card is not in " + source.DisplayName + "'s hand.");
            }

            if (source.Side == CombatantSide.Enemy &&
                intent.CardSource == CombatCardSource.Hand &&
                !CanPlayEnemyCard(source.Id, card, out var enemyRejection))
            {
                return Reject(enemyRejection);
            }

            var target = ResolveTarget(source, card.Spec.TargetType, intent.TargetId);
            if (target == null) return Reject("The selected target is invalid.");
            if (target.IsDead) return Reject("The selected target is already dead.");

            if (card.Spec.CostResource == CardResourceType.ActionPoint)
            {
                if (source.CurrentActionPoints < card.Spec.CostAmount)
                    return Reject(source.DisplayName + " does not have enough action points.");
            }
            else
            {
                if (source.Side != CombatantSide.Player)
                    return Reject("Enemy mana resources are not configured.");
                if (State.Mana.MagicCardsPlayedThisTurn >= State.Rules.MagicCardsPerTurn)
                    return Reject("The magic-card limit for this turn has been reached.");
                if (!CanSpendMana(card.Spec.CostAmount))
                    return Reject("Not enough mana or this turn's mana-spend limit has been reached.");
            }

            var events = new List<CombatEvent>();
            SetPhase(CombatPhase.ResolvingCard, events);

            if (card.Spec.CostResource == CardResourceType.ActionPoint)
            {
                source.TrySpendActionPoints(card.Spec.CostAmount);
                events.Add(new CombatEvent(
                    CombatEventType.ActionPointsChanged,
                    source.Id,
                    amount: source.CurrentActionPoints,
                    message: source.DisplayName + " spent " + card.Spec.CostAmount + " action points."));
            }
            else
            {
                SpendMana(card.Spec.CostAmount, events, "Mana spent to play a magic card.");
                State.Mana.RegisterMagicCardPlayed();
            }

            events.Add(new CombatEvent(
                CombatEventType.CardPlayed,
                source.Id,
                target.Id,
                card.InstanceId,
                card.Spec.CostAmount,
                source.DisplayName + " played " + card.Spec.DisplayName +
                (card.IsUpgraded ? " (upgraded)." : ".")));

            var handLimit = source.Side == CombatantSide.Player
                ? State.Rules.HandLimit
                : State.Rules.GetEnemyTurnRules(source.EnemyRank).HandLimit;
            _flow.ResolveCard(
                source,
                target,
                card,
                sourceDeck,
                handLimit,
                source.Side == CombatantSide.Player && target.Side == CombatantSide.Enemy,
                events);

            if (source.Side == CombatantSide.Enemy && intent.CardSource == CombatCardSource.Hand)
                RecordEnemyCardPlayed(source.Id, card);

            if (intent.CardSource == CombatCardSource.Hand)
            {
                card.ConsumeUpgrade();
                sourceDeck.DiscardFromHand(card.InstanceId, out _);
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded,
                    source.Id,
                    cardInstanceId: card.InstanceId,
                    message: "Used card moved to " + source.DisplayName + "'s discard pile."));
            }
            else
            {
                _usedEnemySpecialCards.Add(card.InstanceId);
            }

            if (source.Side == CombatantSide.Player &&
                card.Spec.CostResource == CardResourceType.Mana)
            {
                TryTriggerUltimate(target.Id, events);
            }

            if (!EvaluateOutcome(events))
            {
                SetPhase(
                    source.Side == CombatantSide.Player
                        ? CombatPhase.PlayerInput
                        : CombatPhase.EnemyTurn,
                    events);
            }

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

            BeginEnemyTurn(events);
            return Complete(true, string.Empty, events);
        }

        private void BeginEnemyTurn(List<CombatEvent> events)
        {
            State.IsCurrentEnemyTurnSkipped = State.Player.TryConsumeSkipEnemyTurn();
            _enemyActorsAuthorizedThisTurn.Clear();
            _enemyActorsBlockedThisTurn.Clear();

            SetPhase(CombatPhase.EnemyTurn, events);
            events.Add(new CombatEvent(
                CombatEventType.EnemyTurnStarted,
                message: "Enemy turn started."));

            foreach (var enemy in State.Enemies)
            {
                if (enemy.IsDead) continue;

                enemy.AdvanceTurnStatuses();
                _enemyCardsPlayedThisTurn[enemy.Id] = 0;
                var baseActionPoints = State.GetEnemyBaseActionPoints(enemy.Id);
                if (baseActionPoints <= 0) continue;

                enemy.RestoreActionPoints(baseActionPoints);
                events.Add(new CombatEvent(
                    CombatEventType.ActionPointsChanged,
                    enemy.Id,
                    amount: enemy.CurrentActionPoints,
                    message: enemy.DisplayName + " restored " + baseActionPoints +
                             " action points."));
            }

            if (State.IsCurrentEnemyTurnSkipped)
            {
                foreach (var enemy in State.Enemies)
                {
                    if (!enemy.IsDead) _enemyActorsBlockedThisTurn.Add(enemy.Id);
                }

                events.Add(new CombatEvent(
                    CombatEventType.CombatantTurnSkipped,
                    State.Player.Id,
                    message: "The current enemy turn is skipped."));
            }
        }

        public CombatResult ResolveEnemyAttack(string enemyId, int damage, int toughnessDamage = 0)
        {
            if (State.Phase != CombatPhase.EnemyTurn)
                return Reject("Enemy attacks can only resolve during the enemy turn.");
            if (damage < 0) return Reject("Enemy damage cannot be negative.");
            if (toughnessDamage < 0) return Reject("Enemy toughness damage cannot be negative.");

            var enemy = State.FindEnemy(enemyId);
            if (enemy == null || enemy.IsDead)
                return Reject("The attacking enemy is invalid.");

            var events = new List<CombatEvent>();
            if (!TryAuthorizeEnemyTurn(enemy, out var skippedEvent))
            {
                if (skippedEvent != null) events.Add(skippedEvent);
                return Complete(true, string.Empty, events);
            }

            _flow.ResolveDirectAttack(
                enemy,
                State.Player,
                damage,
                toughnessDamage,
                "fixed-attack:" + enemy.Id,
                events);

            EvaluateOutcome(events);
            return Complete(true, string.Empty, events);
        }

        private bool TryAuthorizeEnemyTurn(
            CombatantState enemy,
            out CombatEvent skippedEvent)
        {
            skippedEvent = null;
            if (enemy == null || enemy.IsDead) return false;
            if (_enemyActorsAuthorizedThisTurn.Contains(enemy.Id)) return true;

            if (_enemyActorsBlockedThisTurn.Contains(enemy.Id))
            {
                skippedEvent = new CombatEvent(
                    CombatEventType.CombatantTurnSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: enemy.DisplayName + " cannot act again during this turn.");
                return false;
            }

            if (State.IsCurrentEnemyTurnSkipped)
            {
                _enemyActorsBlockedThisTurn.Add(enemy.Id);
                skippedEvent = new CombatEvent(
                    CombatEventType.CombatantTurnSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: enemy.DisplayName + "'s turn was skipped.");
                return false;
            }

            if (enemy.ConsumeOneStunTurn())
            {
                _enemyActorsBlockedThisTurn.Add(enemy.Id);
                skippedEvent = new CombatEvent(
                    CombatEventType.CombatantTurnSkipped,
                    enemy.Id,
                    State.Player.Id,
                    message: enemy.DisplayName + "'s turn was skipped because of stun.");
                return false;
            }

            _enemyActorsAuthorizedThisTurn.Add(enemy.Id);
            return true;
        }

        public bool CanEnemyTakeCardTurn(string enemyId)
        {
            if (State.Phase != CombatPhase.EnemyTurn) return false;
            var enemy = State.FindEnemy(enemyId);
            if (enemy == null || enemy.IsDead) return false;
            if (TryAuthorizeEnemyTurn(enemy, out var skippedEvent)) return true;

            if (skippedEvent != null)
                ForwardEvents(new List<CombatEvent> { skippedEvent });
            return false;
        }

        public bool CanPlayEnemyCard(string enemyId, CardInstance card, out string reason)
        {
            var enemy = State.FindEnemy(enemyId);
            if (enemy == null || enemy.IsDead)
            {
                reason = "The acting enemy is invalid or dead.";
                return false;
            }

            if (card == null)
            {
                reason = "The selected enemy card is missing.";
                return false;
            }

            if (card.Spec.CostResource == CardResourceType.ActionPoint &&
                enemy.CurrentActionPoints < card.Spec.CostAmount)
            {
                reason = "Enemy does not have enough action points.";
                return false;
            }

            var rules = State.Rules.GetEnemyTurnRules(enemy.EnemyRank);
            _enemyCardsPlayedThisTurn.TryGetValue(enemyId, out var cardsPlayed);
            if (cardsPlayed >= rules.CardsPlayedPerTurn)
            {
                reason = "Enemy reached its card-play limit for this turn.";
                return false;
            }

            if (rules.UsesExpensiveCardWindow &&
                card.Spec.CostResource == CardResourceType.ActionPoint &&
                card.Spec.CostAmount == 2 &&
                _enemyRecentCardCosts.TryGetValue(enemyId, out var recentCosts))
            {
                var twoCostCards = 0;
                foreach (var cost in recentCosts)
                {
                    if (cost == 2) twoCostCards++;
                }

                if (twoCostCards >= rules.MaxTwoCostCardsInWindow)
                {
                    reason = "Enemy already used the maximum number of 2-AP cards in its recent-card window.";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Draw cards for an enemy through the same deck-event flow used by the player.</summary>
        public DeckDrawResult DrawEnemyCards(string enemyId, int count, int handLimit)
        {
            if (State.Phase != CombatPhase.EnemyTurn)
            {
                Debug.LogWarning("[CombatEngine] DrawEnemyCards called outside EnemyTurn phase.");
                return null;
            }

            var enemy = State.FindEnemy(enemyId);
            var deck = State.GetDeck(enemyId);
            if (enemy == null || deck == null) return null;

            var events = new List<CombatEvent>();
            var result = _flow.DrawCards(enemy, deck, count, handLimit, events);
            ForwardEvents(events);
            return result;
        }

        public CombatResult PlayEnemyCard(string enemyId, string cardInstanceId)
        {
            return SubmitCardAction(new CombatActionIntent(
                enemyId,
                cardInstanceId,
                State.Player.Id,
                CombatActionOrigin.EnemyAI));
        }

        public CombatResult PlayEnemySpecialCard(string enemyId)
        {
            var card = State.GetEnemySpecialCard(enemyId);
            if (card == null) return Reject("Enemy has no configured special card.");

            return SubmitCardAction(new CombatActionIntent(
                enemyId,
                card.InstanceId,
                State.Player.Id,
                CombatActionOrigin.EnemyAI,
                CombatCardSource.Special));
        }


        /// <summary>Enemy discards its entire hand (called at end of enemy turn).</summary>
        public void DiscardEnemyHand(string enemyId)
        {
            var deck = State.GetEnemyDeck(enemyId);
            if (deck == null) return;

            var discarded = deck.DiscardHand();
            var events = new List<CombatEvent>();
            foreach (var card in discarded)
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded,
                    sourceId: enemyId,
                    cardInstanceId: card.InstanceId,
                    message: "Enemy discarded " + card.Spec.DisplayName + " at turn end."));

            ForwardEvents(events);
        }

        private void RecordEnemyCardPlayed(string enemyId, CardInstance card)
        {
            _enemyCardsPlayedThisTurn.TryGetValue(enemyId, out var played);
            _enemyCardsPlayedThisTurn[enemyId] = played + 1;

            var rules = State.Rules.GetEnemyTurnRules(
                State.FindEnemy(enemyId)?.EnemyRank ?? EnemyRank.Minion);
            if (!rules.UsesExpensiveCardWindow) return;

            if (!_enemyRecentCardCosts.TryGetValue(enemyId, out var recentCosts))
            {
                recentCosts = new Queue<int>();
                _enemyRecentCardCosts[enemyId] = recentCosts;
            }

            var cost = card.Spec.CostResource == CardResourceType.ActionPoint
                ? card.Spec.CostAmount
                : -1;
            recentCosts.Enqueue(cost);
            while (recentCosts.Count > rules.RecentCardWindowSize)
                recentCosts.Dequeue();
        }


        private void ForwardEvents(List<CombatEvent> events)
        {
            var handler = EventRaised;
            if (handler != null)
                foreach (var e in events) handler.Invoke(e);
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

        private void DrawCards(int count, List<CombatEvent> events)
        {
            _flow.DrawCards(
                State.Player,
                State.Deck,
                count,
                State.Rules.HandLimit,
                events);
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
                CombatEventType.UltimateTriggered,
                State.Player.Id,
                target?.Id,
                amount: State.Rules.UltimateDamage,
                message: "Mana threshold reached; ultimate triggered automatically."));

            if (target != null)
            {
                _flow.ResolveDirectDamage(
                    State.Player,
                    target,
                    State.Rules.UltimateDamage,
                    "ultimate",
                    "Ultimate damage resolved.",
                    true,
                    events);
            }

            var restoredMana = State.Mana.RestoreToMaximum();
            events.Add(new CombatEvent(
                CombatEventType.ManaChanged,
                State.Player.Id,
                amount: State.Mana.Current,
                message: "Ultimate restored " + restoredMana + " mana."));
        }

        private void BeginPlayerTurn(List<CombatEvent> events)
        {
            State.TurnNumber++;
            State.IsCurrentEnemyTurnSkipped = false;
            State.Mana.BeginTurn();
            State.Player.AdvanceTurnStatuses();

            SetPhase(CombatPhase.PlayerTurnStart, events);
            events.Add(new CombatEvent(
                CombatEventType.TurnStarted, State.Player.Id,
                amount: State.TurnNumber, message: "Player turn " + State.TurnNumber + " started."));
            if (EvaluateOutcome(events)) return;

            // Status effects on the player and enemies tick at the start of the player turn.
            _flow.ProcessStatusTicks(State.Player, State.FindFirstLivingEnemy()?.Id, events);
            if (EvaluateOutcome(events)) return;

            foreach (var enemy in State.Enemies)
            {
                _flow.ProcessStatusTicks(enemy, State.Player.Id, events);
                if (EvaluateOutcome(events)) return;
            }

            CombatantState skipSource = null;
            var playerStunned = State.Player.ConsumeOneStunTurn();
            if (!playerStunned)
            {
                foreach (var enemy in State.Enemies)
                {
                    if (enemy.IsDead || !enemy.TryConsumeSkipEnemyTurn()) continue;
                    skipSource = enemy;
                    break;
                }
            }

            if (playerStunned || skipSource != null)
            {
                events.Add(new CombatEvent(
                    CombatEventType.CombatantTurnSkipped,
                    skipSource?.Id,
                    State.Player.Id,
                    message: playerStunned
                        ? "Player turn was skipped because of stun."
                        : "Player turn was skipped by " + skipSource.DisplayName + "."));
                BeginEnemyTurn(events);
                return;
            }

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

        private CombatantState ResolveTarget(
            CombatantState source,
            CardTargetType targetType,
            string targetId)
        {
            if (source == null) return null;
            if (targetType == CardTargetType.Self) return source;
            if (targetType != CardTargetType.SingleEnemy) return null;

            var target = State.FindCombatant(targetId) ?? State.FindFirstLivingOpponent(source);
            if (target == null || target.Side == source.Side) return null;
            return target;
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

        /// <summary>对枪械卡进行单发射击。首次射击扣费，每发造成1次伤害，最后一发弃牌。</summary>
        public CombatResult PlaySingleShot(string cardInstanceId, string targetId)
        {
            if (State.Phase != CombatPhase.PlayerInput && State.Phase != CombatPhase.ResolvingCard)
                return Reject("Cannot shoot during phase " + State.Phase + ".");

            var card = State.Deck.FindInHand(cardInstanceId);
            if (card == null) return Reject("The selected card is not in hand.");

            var target = ResolveTarget(State.Player, card.Spec.TargetType, targetId);
            if (target == null) return Reject("The selected target is invalid.");
            if (target.IsDead) return Reject("The selected target is already dead.");

            var events = new List<CombatEvent>();

            // 首次射击：扣费 + 初始化子弹计数
            if (!_gunCardShotsRemaining.ContainsKey(cardInstanceId))
            {
                var totalShots = GetGunCardTotalShots(card);
                if (totalShots <= 1)
                    return PlayCard(cardInstanceId, targetId);

                _gunCardShotsRemaining[cardInstanceId] = totalShots;
                SetPhase(CombatPhase.ResolvingCard, events);

                if (card.Spec.CostResource == CardResourceType.ActionPoint)
                {
                    if (!State.Player.TrySpendActionPoints(card.Spec.CostAmount))
                    {
                        _gunCardShotsRemaining.Remove(cardInstanceId);
                        SetPhase(CombatPhase.PlayerInput, events);
                        return Reject("Not enough action points.");
                    }
                    events.Add(new CombatEvent(
                        CombatEventType.ActionPointsChanged,
                        State.Player.Id,
                        amount: State.Player.CurrentActionPoints,
                        message: "Action points spent for gun card."));
                }

                events.Add(new CombatEvent(
                    CombatEventType.CardPlayed,
                    State.Player.Id,
                    target.Id,
                    card.InstanceId,
                    card.Spec.CostAmount,
                    "Started shooting " + card.Spec.DisplayName + "."));
            }

            // 造成1发伤害
            var damagePerHit = GetGunCardDamagePerHit(card);
            _flow.ResolveDirectDamage(
                State.Player,
                target,
                damagePerHit,
                card.InstanceId,
                "Gun shot resolved.",
                true,
                events);

            _gunCardShotsRemaining[cardInstanceId]--;

            // 最后一发：弃牌 + 回到 PlayerInput
            if (_gunCardShotsRemaining[cardInstanceId] <= 0)
            {
                _gunCardShotsRemaining.Remove(cardInstanceId);
                card.ConsumeUpgrade();
                State.Deck.DiscardFromHand(cardInstanceId, out _);
                events.Add(new CombatEvent(
                    CombatEventType.CardDiscarded,
                    State.Player.Id,
                    cardInstanceId: card.InstanceId,
                    message: "Gun card emptied and discarded."));

                if (EvaluateOutcome(events)) return Complete(true, string.Empty, events);
                SetPhase(CombatPhase.PlayerInput, events);
            }

            return Complete(true, string.Empty, events);
        }

        /// <summary>取消正在进行的射击（如回合结束时强制结束）。</summary>
        public CombatResult CancelShooting(string cardInstanceId)
        {
            if (!_gunCardShotsRemaining.ContainsKey(cardInstanceId)) return Reject("Card is not being shot.");
            _gunCardShotsRemaining.Remove(cardInstanceId);

            var card = State.Deck.FindInHand(cardInstanceId);
            if (card != null)
            {
                State.Deck.DiscardFromHand(cardInstanceId, out _);
                var events = new List<CombatEvent>
                {
                    new CombatEvent(CombatEventType.CardDiscarded,
                        State.Player.Id,
                        cardInstanceId: card.InstanceId,
                        message: "Gun card cancelled and discarded.")
                };
                SetPhase(CombatPhase.PlayerInput, events);
                return Complete(true, string.Empty, events);
            }
            return Reject("Card not found in hand.");
        }

        /// <summary>一次性打完剩余子弹（拖拽中途清弹）。</summary>
        public CombatResult PlayRemainingShots(string cardInstanceId, string targetId)
        {
            if (!_gunCardShotsRemaining.ContainsKey(cardInstanceId))
                return PlayCard(cardInstanceId, targetId);

            var remaining = _gunCardShotsRemaining[cardInstanceId];
            var card = State.Deck.FindInHand(cardInstanceId);
            if (card == null) return Reject("Card not in hand.");

            var target = ResolveTarget(State.Player, card.Spec.TargetType, targetId);
            if (target == null || target.IsDead) return Reject("Invalid target.");

            var events = new List<CombatEvent>();
            var damagePerHit = GetGunCardDamagePerHit(card);

            for (int i = 0; i < remaining && !target.IsDead; i++)
            {
                _flow.ResolveDirectDamage(
                    State.Player,
                    target,
                    damagePerHit,
                    card.InstanceId,
                    "Gun shot (burst) resolved.",
                    true,
                    events);
            }


            _gunCardShotsRemaining.Remove(cardInstanceId);
            card.ConsumeUpgrade();
            State.Deck.DiscardFromHand(cardInstanceId, out _);
            events.Add(new CombatEvent(
                CombatEventType.CardDiscarded,
                State.Player.Id,
                cardInstanceId: card.InstanceId,
                message: "Gun card burst-fired and discarded."));

            if (EvaluateOutcome(events)) return Complete(true, string.Empty, events);
            SetPhase(CombatPhase.PlayerInput, events);
            return Complete(true, string.Empty, events);
        }

        private static int GetGunCardTotalShots(CardInstance card)
        {
            foreach (var effect in card.Spec.Effects)
            {
                if (effect.Type == CardEffectType.Damage)
                    return effect.Hits.Resolve(card.IsUpgraded);
            }
            return 1;
        }

        private static int GetGunCardDamagePerHit(CardInstance card)
        {
            foreach (var effect in card.Spec.Effects)
            {
                if (effect.Type == CardEffectType.Damage)
                    return effect.Amount.Resolve(card.IsUpgraded);
            }
            return 0;
        }

        public bool IsShooting(string cardInstanceId)
        {
            return _gunCardShotsRemaining.ContainsKey(cardInstanceId);
        }
    }
}
