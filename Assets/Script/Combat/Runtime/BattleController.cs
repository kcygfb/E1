using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Linear Demo Encounters")]
        [Tooltip("Order must be: Dog (Minion), Little Girl (Elite), Big Eye (Boss).")]
        [SerializeField] private List<CombatantDefinition> demoEnemyDefinitions = new List<CombatantDefinition>();

        [Header("Enemy Presentation")]
        [SerializeField] private Image enemyPortraitImage;
        [Tooltip("Optional Big Eye-only overlay. It is hidden automatically for the Dog and Little Girl.")]
        [SerializeField] private GameObject bigEyePortraitOverlay;

        [Header("Deck source")]
        [Tooltip("Used only when no selection screen has filled BattleSession.")]
        [SerializeField] private List<string> debugStartingCardIds = new List<string>();
        [SerializeField] private int randomSeed = 1;
        [SerializeField] private bool shuffleAtBattleStart = true;

        [Header("Lifecycle")]
        [SerializeField] private bool autoStartBattle = true;

        [Header("Hunt rewards")]
        [Min(0)] [SerializeField] private int huntGoldReward = 100;
        [SerializeField] private List<HuntLootReward> huntLootRewards = new List<HuntLootReward>
        {
            new HuntLootReward("CocoaPowder", "Cocoa Powder", 1)
        };
        [Min(0)] [SerializeField] private int huntRewardCardCount = 3;
        [Tooltip("Empty means draw from every card in CardDataV2.")]
        [SerializeField] private List<string> huntRewardCardPool = new List<string>();
        [Min(0f)] [SerializeField] private float huntResultDelay = 0.75f;

        private CombatEngine _engine;
        private string[] _coffeeSlots = new string[2];

        public BattleState State => _engine?.State;
        public bool IsInitialized => _engine != null;
        public int HuntGoldReward => huntGoldReward;
        public IReadOnlyList<HuntLootReward> HuntLootRewards => huntLootRewards;
        public int HuntRewardCardCount => huntRewardCardCount;
        public IReadOnlyList<string> HuntRewardCardPool => huntRewardCardPool;
        public float HuntResultDelay => huntResultDelay;
        internal CardJsonRepository CardRepository => cardDatabase != null ? cardDatabase.Repository : null;
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

                ApplySelectedDemoEncounter();

                if (enemyDefinitions == null || enemyDefinitions.Count == 0)
                    throw new InvalidOperationException("At least one enemy definition is required.");

                ApplyPrimaryEnemyPresentation();

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

                CreateEnemyDecks(state);

                _engine = new CombatEngine(state);
                _engine.EventRaised += ForwardEvent;
                var result = _engine.StartBattle();
                if (!result.Success)
                {
                    Debug.LogError(result.Message, this);
                    DisposeEngine();
                    return false;
                }

                var huntResultPresenter = GetComponent<HuntResultPresenter>();
                if (huntResultPresenter == null)
                    huntResultPresenter = gameObject.AddComponent<HuntResultPresenter>();
                huntResultPresenter.Configure(this);

                if (usesSelectedDeck)
                    BattleSession.ClearSelectedDeck();

                // Coffee slots
                if (BattleSession.HasSelectedCoffees)
                {
                    var coffees = BattleSession.SelectedCoffeeIds;
                    for (int i = 0; i < _coffeeSlots.Length && i < coffees.Count; i++)
                        _coffeeSlots[i] = coffees[i];
                    BattleSession.ClearSelectedCoffees();
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

        public CombatResult SubmitCardAction(CombatActionIntent intent)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));

            var engine = GetEngineOrThrow();
            var source = engine.State.FindCombatant(intent.ActorId);
            var card = intent.CardSource == CombatCardSource.Special
                ? engine.State.GetEnemySpecialCard(intent.ActorId)
                : engine.State.GetDeck(intent.ActorId)?.FindInHand(intent.CardInstanceId);
            var cardName = card != null ? card.Spec.DisplayName : intent.CardInstanceId;
            var result = engine.SubmitCardAction(intent);

            if (result.Success)
            {
                var sourceName = source != null ? source.DisplayName : intent.ActorId;
                var actualDamage = SumDamage(result, intent.ActorId);
                Debug.Log("[Combat] " + sourceName + " played card \"" + cardName +
                          "\" through the shared flow and dealt " + actualDamage + " damage.", this);
            }

            return result;
        }

        public CombatResult PlayCard(string cardInstanceId, string targetId)
        {
            return SubmitCardAction(new CombatActionIntent(
                State.Player.Id,
                cardInstanceId,
                targetId,
                CombatActionOrigin.PlayerInput));
        }

        public CombatResult UpgradeCard(string cardInstanceId, string preferredUltimateTargetId = null)
        {
            return GetEngineOrThrow().UpgradeCard(cardInstanceId, preferredUltimateTargetId);
        }

        public CombatResult PlaySingleShot(string cardInstanceId, string targetId)
        {
            return GetEngineOrThrow().PlaySingleShot(cardInstanceId, targetId);
        }

        public CombatResult PlayRemainingShots(string cardInstanceId, string targetId)
        {
            return GetEngineOrThrow().PlayRemainingShots(cardInstanceId, targetId);
        }

        public bool IsShooting(string cardInstanceId)
        {
            return GetEngineOrThrow().IsShooting(cardInstanceId);
        }

        public CombatResult CancelShooting(string cardInstanceId)
        {
            return GetEngineOrThrow().CancelShooting(cardInstanceId);
        }

        public CombatResult EndPlayerTurn() { return GetEngineOrThrow().EndPlayerTurn(); }

        // ─── Coffee ───

        public string GetCoffeeSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _coffeeSlots.Length) return null;
            return _coffeeSlots[slotIndex];
        }

        public CombatResult UseCoffee(int slotIndex, string targetId)
        {
            if (slotIndex < 0 || slotIndex >= _coffeeSlots.Length)
                return new CombatResult(false, "Invalid coffee slot.", new List<CombatEvent>());

            var coffeeId = _coffeeSlots[slotIndex];
            if (string.IsNullOrEmpty(coffeeId))
                return new CombatResult(false, "Coffee slot is empty.", new List<CombatEvent>());

            var engine = GetEngineOrThrow();
            var result = engine.UseCoffee(coffeeId, targetId);
            if (result.Success)
            {
                _coffeeSlots[slotIndex] = null;
                Debug.Log("[Combat] Used coffee: " + coffeeId + " on " + targetId, this);
            }
            return result;
        }

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
                    if (combatEvent.Type != CombatEventType.EnemyActionSkipped &&
                        combatEvent.Type != CombatEventType.CombatantTurnSkipped &&
                        combatEvent.Type != CombatEventType.ActionNullified) continue;
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

        // ─── Enemy card system forwarding ───

        public DeckDrawResult DrawEnemyCards(string enemyId, int count, int handLimit)
        {
            return GetEngineOrThrow().DrawEnemyCards(enemyId, count, handLimit);
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
            if (card == null) return GetEngineOrThrow().PlayEnemySpecialCard(enemyId);

            return SubmitCardAction(new CombatActionIntent(
                enemyId,
                card.InstanceId,
                State.Player.Id,
                CombatActionOrigin.EnemyAI,
                CombatCardSource.Special));
        }


        public void DiscardEnemyHand(string enemyId)
        {
            GetEngineOrThrow().DiscardEnemyHand(enemyId);
        }

        public CombatantDefinition FindEnemyDefinitionById(string combatantId)
        {
            if (enemyDefinitions == null) return null;
            foreach (var def in enemyDefinitions)
            {
                if (def != null && def.CombatantId == combatantId)
                    return def;
            }
            return null;
        }

        public DeckState GetEnemyDeck(string enemyId)
        {
            return _engine?.State.GetEnemyDeck(enemyId);
        }

        public CombatEngine GetEngineInternal() => _engine;

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

        private void ApplySelectedDemoEncounter()
        {
            if (!BattleSession.HasSelectedDemoStage)
                return;

            var stage = BattleSession.SelectedDemoStage;
            if (!DemoFlowState.IsStageAvailable(stage))
                throw new InvalidOperationException(
                    $"Selected demo stage {stage} does not match current stage {DemoFlowState.CurrentStage}.");

            var index = (int)stage;
            if (demoEnemyDefinitions == null || index < 0 || index >= demoEnemyDefinitions.Count)
                throw new InvalidOperationException(
                    $"Demo enemy slot {index + 1} ({stage}) is not configured on BattleController.");

            var definition = demoEnemyDefinitions[index];
            if (definition == null)
                throw new InvalidOperationException($"Demo enemy slot {index + 1} ({stage}) is null.");

            enemyDefinitions = new List<CombatantDefinition> { definition };
            Debug.Log(
                $"[DemoFlow] BattleController selected {definition.DisplayName} / " +
                $"{definition.EnemyArchetype} / {definition.EnemyRank} for {stage}.",
                this);
        }

        private void ApplyPrimaryEnemyPresentation()
        {
            var definition = enemyDefinitions[0];
            if (definition == null)
                throw new InvalidOperationException("Primary enemy definition is null.");

            if (enemyPortraitImage == null)
            {
                var portraitObject = GameObject.Find("EnemyPortrait");
                if (portraitObject != null)
                    enemyPortraitImage = portraitObject.GetComponent<Image>();
            }

            if (enemyPortraitImage == null)
            {
                Debug.LogWarning("EnemyPortrait Image is not configured; enemy art cannot be updated.", this);
                return;
            }

            if (definition.Portrait == null)
            {
                Debug.LogWarning(definition.name + " has no enemy portrait configured.", definition);
                return;
            }

            enemyPortraitImage.sprite = definition.Portrait;
            enemyPortraitImage.preserveAspect = true;

            var portraitRect = enemyPortraitImage.rectTransform;
            portraitRect.sizeDelta = definition.PortraitSize;
            portraitRect.anchoredPosition = definition.PortraitOffset;
            portraitRect.localScale = Vector3.one * definition.PortraitScale;

            if (bigEyePortraitOverlay != null)
                bigEyePortraitOverlay.SetActive(definition.EnemyArchetype == EnemyArchetype.BigEye);

            var hitFeedback = enemyPortraitImage.GetComponent<EnemyHitFeedbackNew>();
            if (hitFeedback != null)
                hitFeedback.RefreshOrigin();

            Debug.Log($"[Combat] Applied portrait for {definition.DisplayName} at scale {definition.PortraitScale:0.###}.", this);
        }

        private void CreateEnemyDecks(BattleState state)
        {
            for (var i = 0; i < enemyDefinitions.Count; i++)
            {
                var definition = enemyDefinitions[i];
                if (definition == null) continue;

                var enemy = state.Enemies[i];
                var turnRules = state.Rules.GetEnemyTurnRules(enemy.EnemyRank);
                List<CardInstance> enemyCards;
                CardSpec specialCard = null;

                if (definition.EnemyArchetype != EnemyArchetype.None)
                {
                    enemyCards = new List<CardInstance>();
                    foreach (var spec in cardDatabase.Repository.Cards)
                    {
                        if (!string.Equals(
                                spec.Category,
                                definition.EnemyCardCategory,
                                StringComparison.Ordinal))
                            continue;

                        if (spec.IsSpecial)
                        {
                            if (specialCard != null)
                                throw new InvalidOperationException(
                                    definition.name + " has more than one special enemy card.");
                            specialCard = spec;
                            continue;
                        }

                        enemyCards.Add(new CardInstance(
                            enemy.Id + "_" + spec.Id + "#" + (enemyCards.Count + 1).ToString("D2"),
                            spec));
                    }
                }
                else if (definition.HasEnemyDeck)
                {
                    enemyCards = CreateCardInstances(definition.EnemyCardIds, enemy.Id + "_");
                }
                else
                {
                    continue;
                }

                if (enemyCards.Count == 0)
                    throw new InvalidOperationException(
                        definition.name + " resolved to an empty enemy card deck.");

                if (enemyCards.Count != turnRules.DeckSize)
                {
                    Debug.LogWarning(
                        definition.name + " has " + enemyCards.Count +
                        " normal enemy cards; " + enemy.EnemyRank +
                        " rules expect " + turnRules.DeckSize + ".", this);
                }

                var enemySeed = randomSeed + i + 1;
                state.RegisterEnemyDeck(
                    enemy.Id,
                    new DeckState(enemyCards, enemySeed, shuffleAtBattleStart));
                state.RegisterEnemyBaseActionPoints(enemy.Id, turnRules.BaseActionPoints);

                if (specialCard != null)
                {
                    state.RegisterEnemySpecialCard(
                        enemy.Id,
                        new CardInstance(enemy.Id + "_special_" + specialCard.Id, specialCard));
                }
            }
        }

        private List<CardInstance> CreateCardInstances(IReadOnlyList<string> cardIds, string instanceIdPrefix = "")
        {
            var cards = new List<CardInstance>(cardIds.Count);
            for (var i = 0; i < cardIds.Count; i++)
            {
                var spec = cardDatabase.Repository.GetRequiredCard(cardIds[i]);
                cards.Add(new CardInstance(instanceIdPrefix + spec.Id + "#" + (i + 1).ToString("D2"), spec));
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
