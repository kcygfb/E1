using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Scene entry point. It creates runtime instances from JSON card ids and forwards commands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Shared card database")]
        [SerializeField] private CardDatabaseService cardDatabase = null;

        [Header("Rules and participants")]
        [SerializeField] private CombatRulesConfig rulesConfig = null;
        [SerializeField] private CombatantDefinition playerDefinition = null;
        [SerializeField] private List<CombatantDefinition> enemyDefinitions = new List<CombatantDefinition>();

        [Header("Deck source")]
        [Tooltip("Used only when no selection screen has filled BattleSession.")]
        [SerializeField] private List<string> debugStartingCardIds = new List<string>();
        [SerializeField] private int randomSeed = 1;
        [SerializeField] private bool shuffleAtBattleStart = true;

        [Header("Lifecycle")]
        [SerializeField] private bool autoStartBattle = true;

        private CombatEngine _engine;

        public BattleState State => _engine?.State;
        public bool IsInitialized => _engine != null;
        public event Action<CombatEvent> CombatEventRaised;

        private IEnumerator Start()
        {
            if (!autoStartBattle) yield break;
            cardDatabase = ResolveCardDatabase();
            if (cardDatabase == null)
            {
                Debug.LogError("No CardDatabaseService exists in the scene or persistent objects.", this);
                yield break;
            }

            yield return cardDatabase.EnsureLoaded();
            cardDatabase = ResolveCardDatabase();
            if (cardDatabase == null || !cardDatabase.IsLoaded)
            {
                var error = cardDatabase != null
                    ? cardDatabase.LastError
                    : "CardDatabaseService became unavailable during scene loading.";
                Debug.LogError("Battle cannot start because card JSON failed to load: " + error, this);
                yield break;
            }

            InitializeBattle();
        }

        public bool InitializeBattle()
        {
            try
            {
                DisposeEngine();

                cardDatabase = ResolveCardDatabase();
                if (cardDatabase == null || !cardDatabase.IsLoaded)
                    throw new InvalidOperationException("CardDatabaseService must finish loading first.");
                if (rulesConfig == null) throw new InvalidOperationException("CombatRulesConfig is not assigned.");
                if (playerDefinition == null) throw new InvalidOperationException("Player definition is not assigned.");
                if (playerDefinition.Side != CombatantSide.Player)
                    throw new InvalidOperationException("Player definition must use the Player side.");
                if (enemyDefinitions == null || enemyDefinitions.Count == 0)
                    throw new InvalidOperationException("At least one enemy definition is required.");

                var usesSelectedDeck = BattleSession.HasSelectedDeck;
                var selectedIds = usesSelectedDeck
                    ? BattleSession.SelectedCardIds
                    : debugStartingCardIds;
                if (selectedIds == null || selectedIds.Count == 0)
                    throw new InvalidOperationException(
                        "No selected deck exists. Fill BattleSession from the selection screen " +
                        "or add debug card ids on BattleController.");

                var rules = rulesConfig.CreateRuntimeRules();
                var cards = CreateCardInstances(selectedIds);
                if (cards.Count != rules.ExpectedInitialDeckSize)
                {
                    if (usesSelectedDeck)
                        throw new InvalidOperationException(
                            "Selected deck contains " + cards.Count + " cards; rules require exactly " +
                            rules.ExpectedInitialDeckSize + ".");

                    Debug.LogWarning(
                        "Debug deck contains " + cards.Count + " cards; rules expect " +
                        rules.ExpectedInitialDeckSize + ".", this);
                }

                var state = new BattleState(
                    rules,
                    playerDefinition.CreateRuntimeState(),
                    CreateEnemies(),
                    new DeckState(cards, randomSeed, shuffleAtBattleStart));

                _engine = new CombatEngine(state);
                _engine.EventRaised += ForwardEvent;
                var result = _engine.StartBattle();
                if (!result.Success)
                {
                    Debug.LogError(result.Message, this);
                    DisposeEngine();
                    return false;
                }

                if (usesSelectedDeck)
                    BattleSession.ClearSelectedDeck();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Battle initialization failed: " + exception.Message, this);
                DisposeEngine();
                return false;
            }
        }

        public CombatResult PlayCard(string cardInstanceId, string targetId)
        {
            var engine = GetEngineOrThrow();
            var card = engine.State.Deck.FindInHand(cardInstanceId);
            var cardName = card != null ? card.Spec.DisplayName : cardInstanceId;
            var playerId = engine.State.Player.Id;
            var result = engine.PlayCard(cardInstanceId, targetId);

            if (result.Success)
            {
                var actualDamage = SumDamage(result, playerId);
                Debug.Log("[Combat] Player played card \"" + cardName +
                          "\" and dealt " + actualDamage + " damage.", this);
            }

            return result;
        }

        public CombatResult UpgradeCard(string cardInstanceId, string preferredUltimateTargetId = null)
        {
            return GetEngineOrThrow().UpgradeCard(cardInstanceId, preferredUltimateTargetId);
        }

        public CombatResult ConfirmExecution() { return GetEngineOrThrow().ConfirmExecution(); }
        public CombatResult EndPlayerTurn() { return GetEngineOrThrow().EndPlayerTurn(); }

        public CombatResult ResolveEnemyAttack(string enemyId, int damage, int toughnessDamage = 0)
        {
            var engine = GetEngineOrThrow();
            var enemy = engine.State.FindEnemy(enemyId);
            var enemyName = enemy != null ? enemy.DisplayName : enemyId;
            var toughnessBefore = engine.State.Player.CurrentToughness;
            var result = engine.ResolveEnemyAttack(enemyId, damage, toughnessDamage);

            if (result.Success)
            {
                CombatEvent skippedEvent = null;
                foreach (var combatEvent in result.Events)
                {
                    if (combatEvent.Type != CombatEventType.EnemyActionSkipped) continue;
                    skippedEvent = combatEvent;
                    break;
                }

                if (skippedEvent != null)
                {
                    Debug.Log("[Combat] " + enemyName +
                              " skipped \"Basic Attack\": " + skippedEvent.Message, this);
                }
                else
                {
                    var actualDamage = SumDamage(result, enemyId);
                    var actualToughnessDamage =
                        toughnessBefore - engine.State.Player.CurrentToughness;
                    Debug.Log("[Combat] " + enemyName +
                              " used \"Basic Attack\" and dealt " + actualDamage +
                              " damage and " + actualToughnessDamage + " toughness damage.", this);
                }
            }

            return result;
        }

        public CombatResult CompleteEnemyTurn() { return GetEngineOrThrow().CompleteEnemyTurn(); }

        public void SetPlayerActionPointModifier(int modifier)
        {
            GetEngineOrThrow().State.Player.SetActionPointModifier(modifier);
        }

        private static int SumDamage(CombatResult result, string sourceId)
        {
            var total = 0;
            foreach (var combatEvent in result.Events)
            {
                if (combatEvent.Type == CombatEventType.DamageApplied &&
                    string.Equals(combatEvent.SourceId, sourceId, StringComparison.Ordinal))
                {
                    total += Math.Max(0, combatEvent.Amount);
                }
            }

            return total;
        }

        private List<CombatantState> CreateEnemies()
        {
            var enemies = new List<CombatantState>(enemyDefinitions.Count);
            foreach (var definition in enemyDefinitions)
            {
                if (definition == null) throw new InvalidOperationException("Enemy definition list contains null.");
                if (definition.Side != CombatantSide.Enemy)
                    throw new InvalidOperationException(definition.name + " must use the Enemy side.");
                enemies.Add(definition.CreateRuntimeState());
            }

            return enemies;
        }

        private List<CardInstance> CreateCardInstances(IReadOnlyList<string> cardIds)
        {
            var cards = new List<CardInstance>(cardIds.Count);
            for (var i = 0; i < cardIds.Count; i++)
            {
                var spec = cardDatabase.Repository.GetRequiredCard(cardIds[i]);
                cards.Add(new CardInstance(spec.Id + "#" + (i + 1).ToString("D2"), spec));
            }

            return cards;
        }

        private CardDatabaseService ResolveCardDatabase()
        {
            if (CardDatabaseService.Instance != null)
                return CardDatabaseService.Instance;

            if (cardDatabase != null)
                return cardDatabase;

            return FindFirstObjectByType<CardDatabaseService>();
        }

        private CombatEngine GetEngineOrThrow()
        {
            if (_engine == null)
                throw new InvalidOperationException("BattleController has not initialized a battle.");
            return _engine;
        }

        private void ForwardEvent(CombatEvent combatEvent)
        {
            CombatEventRaised?.Invoke(combatEvent);
        }

        private void OnDestroy() { DisposeEngine(); }

        private void DisposeEngine()
        {
            if (_engine != null) _engine.EventRaised -= ForwardEvent;
            _engine = null;
        }
    }
}
