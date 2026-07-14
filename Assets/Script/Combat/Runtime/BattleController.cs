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
            if (cardDatabase == null)
                cardDatabase = CardDatabaseService.Instance != null
                    ? CardDatabaseService.Instance
                    : FindFirstObjectByType<CardDatabaseService>();
            if (cardDatabase == null)
            {
                Debug.LogError("No CardDatabaseService exists in the scene or persistent objects.", this);
                yield break;
            }

            yield return cardDatabase.EnsureLoaded();
            if (!cardDatabase.IsLoaded)
            {
                Debug.LogError("Battle cannot start because card JSON failed to load: " + cardDatabase.LastError, this);
                yield break;
            }

            InitializeBattle();
        }

        public bool InitializeBattle()
        {
            try
            {
                DisposeEngine();

                if (cardDatabase == null || !cardDatabase.IsLoaded)
                    throw new InvalidOperationException("CardDatabaseService must finish loading first.");
                if (rulesConfig == null) throw new InvalidOperationException("CombatRulesConfig is not assigned.");
                if (playerDefinition == null) throw new InvalidOperationException("Player definition is not assigned.");
                if (playerDefinition.Side != CombatantSide.Player)
                    throw new InvalidOperationException("Player definition must use the Player side.");
                if (enemyDefinitions == null || enemyDefinitions.Count == 0)
                    throw new InvalidOperationException("At least one enemy definition is required.");

                var selectedIds = BattleSession.HasSelectedDeck
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
                    Debug.LogWarning(
                        "Starting deck contains " + cards.Count + " cards; rules expect " +
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
            return GetEngineOrThrow().PlayCard(cardInstanceId, targetId);
        }

        public CombatResult UpgradeCard(string cardInstanceId, string preferredUltimateTargetId = null)
        {
            return GetEngineOrThrow().UpgradeCard(cardInstanceId, preferredUltimateTargetId);
        }

        public CombatResult ConfirmExecution() { return GetEngineOrThrow().ConfirmExecution(); }
        public CombatResult EndPlayerTurn() { return GetEngineOrThrow().EndPlayerTurn(); }

        public CombatResult ResolveEnemyAttack(string enemyId, int damage)
        {
            return GetEngineOrThrow().ResolveEnemyAttack(enemyId, damage);
        }

        public CombatResult CompleteEnemyTurn() { return GetEngineOrThrow().CompleteEnemyTurn(); }

        public void SetPlayerActionPointModifier(int modifier)
        {
            GetEngineOrThrow().State.Player.SetActionPointModifier(modifier);
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
