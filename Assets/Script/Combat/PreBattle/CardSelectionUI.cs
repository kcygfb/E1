using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using KiKs.UI;

namespace KiKs.Combat
{
    public class CardSelectionUI : MonoBehaviour
    {
        private const int DEFAULT_DECK_SIZE = 15;
        private const int MaxSpecialCardsInDeck = 2;
        private const string BATTLE_SCENE_NAME = "Card";
        private const string TREASURE_SCENE_NAME = "Treasure";

        [Header("Buttons")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Button beginButton;
        [SerializeField] private Button undoButton;

        [Header("Rules")]
        [SerializeField] private CombatRulesConfig rulesConfig;

        [Header("Daily Area Map")]
        [Tooltip("Five map points. Their positions are randomized between three battles, one event, and one treasure.")]
        [SerializeField] private List<GameObject> demoMapPoints = new();
        [SerializeField] private Sprite battlePointSprite;
        [SerializeField] private Sprite eventPointSprite;
        [SerializeField] private Sprite treasurePointSprite;
        [Tooltip("Decorative UI drawn above the map. Its non-button Graphics must not intercept clicks.")]
        [SerializeField] private Transform decorativeOverlay;
        [SerializeField] private GameObject demoCompletePanel;
        [SerializeField] private Text demoCompleteLabel;
        [SerializeField] private string demoCompleteText = "试玩结束 / Demo Complete";
        [SerializeField] private bool openCardSelectionAfterMapClick = true;

        [Header("Popups")]
        [SerializeField] private GameObject cardPopup;

        [Header("Card Grid")]
        [SerializeField] private Transform cardGridContent;
        [SerializeField] private GameObject cardItemPrefab;

        [Header("Tutorial")]
        [SerializeField] private TutorialController tutorialController;

        [Header("Demo Encounters")]
        [Tooltip("三个敌人定义，槽位 0=Dog, 1=Little Girl, 2=Big Eye（与 BattleController.demoEnemyDefinitions 一致）。战斗点教学框按此生成 Boss 描述。")]
        [SerializeField] private List<CombatantDefinition> demoEnemyDefinitions = new();

        [Header("Deck Slots")]
        [SerializeField] private Transform deckGridContent;
        [SerializeField] private Text deckLabel;

        [Header("Card Art")]
        [SerializeField] private Vector2 cardArtSize = new Vector2(120, 100);

        private readonly List<string> selectedCardIds = new();
        private readonly List<CardSpec> allCards = new();
        private readonly Dictionary<Button, UnityAction> demoMapPointListeners = new();
        private bool _isStartingBattle;

        private int RequiredDeckSize =>
            rulesConfig != null ? rulesConfig.ExpectedInitialDeckSize : DEFAULT_DECK_SIZE;
        private bool IsDeckComplete => selectedCardIds.Count == RequiredDeckSize;

        private IEnumerator Start()
        {
            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();

            DisableDecorativeOverlayRaycasts();
            BindDemoMapPoints();
            DailyAreaMapState.EnsureGenerated();
            RefreshMapPoints();

            yield return TransitionEffect.WaitEntrance();

            ResolveUndoButton();
            if (cardButton != null)
                cardButton.onClick.AddListener(OnCardButtonClicked);
            if (beginButton != null)
                beginButton.onClick.AddListener(OnBeginClicked);
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);

            BindCloseButton(cardPopup);
            RestoreSelectedDeckFromSession();
            RefreshSelectionUI();

            StartCoroutine(LoadCardsAndPopulate());
        }

        private void OnDestroy()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveListener(OnCardButtonClicked);
            if (beginButton != null)
                beginButton.onClick.RemoveListener(OnBeginClicked);
            if (undoButton != null)
                undoButton.onClick.RemoveListener(OnUndoClicked);

            foreach (var pair in demoMapPointListeners)
                if (pair.Key != null) pair.Key.onClick.RemoveListener(pair.Value);
            demoMapPointListeners.Clear();
            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);
        }

        private IEnumerator LoadCardsAndPopulate()
        {
            // Ensure CardDatabaseService is loaded
            var db = CardDatabaseService.Instance;
            if (db == null)
                db = FindFirstObjectByType<CardDatabaseService>();
            if (db == null)
            {
                Debug.LogError("[CardSelectionUI] No CardDatabaseService found.");
                yield break;
            }

            yield return db.EnsureLoaded();
            if (!db.IsLoaded)
            {
                Debug.LogError("[CardSelectionUI] Card database failed to load: " + db.LastError);
                yield break;
            }

            allCards.Clear();
            foreach (var card in StaticGameRepository.PlayerCards)
            {
                // 当前版本只使用有卡面的卡牌；无卡面（ImagePath 为空）的卡不进入选牌界面
                if (!string.IsNullOrEmpty(card.ImagePath))
                    allCards.Add(card);
            }

            PopulateCardGrid();
            RefreshSelectionUI();
        }

        private void PopulateCardGrid()
        {
            if (cardGridContent == null) return;

            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);

            for (int i = cardGridContent.childCount - 1; i >= 0; i--)
                Destroy(cardGridContent.GetChild(i).gameObject);

            foreach (var card in allCards)
                CreateCardItem(card);
        }

        private void CreateCardItem(CardSpec card)
        {
            GameObject item;
            if (cardItemPrefab != null)
                item = Instantiate(cardItemPrefab, cardGridContent);
            else
                item = CreateDefaultCardItem(card);

            item.name = card.Id;

            // Add card art if using a prefab that doesn't have it yet
            if (cardItemPrefab != null && !string.IsNullOrEmpty(card.ImagePath))
            {
                var existingArt = item.transform.Find("CardArt");
                if (existingArt == null)
                    AddCardArtChild(item, card);
            }

            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();

            var cardId = card.Id;
            btn.onClick.AddListener(() => OnCardClicked(cardId));

            if (tutorialController != null)
                tutorialController.RegisterJsonCallout(this, item.GetComponent<RectTransform>(), card.Tutorial);
        }

        private void AddCardArtChild(GameObject parent, CardSpec card)
        {
            var artGO = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artGO.transform.SetParent(parent.transform, false);
            artGO.transform.SetAsFirstSibling();

            var artRT = artGO.GetComponent<RectTransform>();
            artRT.anchorMin = Vector2.zero;
            artRT.anchorMax = Vector2.one;
            artRT.offsetMin = Vector2.zero;
            artRT.offsetMax = Vector2.zero;

            var artImage = artGO.GetComponent<Image>();
            artImage.preserveAspect = true;
            CardImageLoader.ApplyToImage(artImage, card.ImagePath);
        }

        private GameObject CreateDefaultCardItem(CardSpec card)
        {
            var go = new GameObject(card.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(cardGridContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 190);
            go.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.14f, 1);

            // Card art image
            if (!string.IsNullOrEmpty(card.ImagePath))
                AddCardArtChild(go, card);

            // Card name
            CreateText("CardName", go.transform, card.DisplayName, 16, new Color(0.9f, 0.85f, 0.7f, 1),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(3, -50), new Vector2(-3, -28));

            // Cost
            var costText = card.CostResource == CardResourceType.ActionPoint ? "AP" : "MP";
            var copiesLabel = card.IsSpecial ? "x1（特殊）" : "x2";
            CreateText("CardCost", go.transform, $"{costText}: {card.CostAmount}  {copiesLabel}", 14, new Color(0.8f, 0.6f, 0.3f, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(3, 3), new Vector2(-3, 20));

            // Effects summary
            var effectsText = GetEffectsSummary(card);
            CreateText("CardDesc", go.transform, effectsText, 12, new Color(0.65f, 0.65f, 0.65f, 1),
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(3, 22), new Vector2(-3, -55));

            return go;
        }

        private void OnCardClicked(string cardId)
        {
            if (_isStartingBattle) return;

            var card = allCards.Find(candidate => candidate.Id == cardId);
            if (card == null) return;

            var selectedCopies = 0;
            foreach (var selectedId in selectedCardIds)
            {
                if (selectedId == cardId) selectedCopies++;
            }
            if (selectedCopies >= GetMaxCopies(card))
            {
                Debug.Log("[CardSelectionUI] No more copies are available for " + card.DisplayName + ".");
                WarningToast.Show(card.IsSpecial
                    ? "该特殊卡牌最多选择1张"
                    : "普通卡牌最多选择2张");
                return;
            }

            if (card.IsSpecial && CountSelectedSpecialCards() >= MaxSpecialCardsInDeck)
            {
                Debug.Log("[CardSelectionUI] Special card quota reached.");
                WarningToast.Show(string.Format(
                    "特殊卡牌总共最多选择{0}张（且必须为不同的两张）", MaxSpecialCardsInDeck));
                return;
            }

            if (selectedCardIds.Count >= RequiredDeckSize)
            {
                Debug.Log("[CardSelectionUI] Deck is full.");
                WarningToast.Show(string.Format("卡组已满：最多{0}张", RequiredDeckSize));
                return;
            }

            selectedCardIds.Add(cardId);
            RefreshSelectionUI();
        }

        /// <summary>每张卡牌在卡组中允许选入的数量：特殊卡 1 张，普通卡 2 张。</summary>
        private static int GetMaxCopies(CardSpec card)
        {
            return card.IsSpecial ? 1 : 2;
        }

        /// <summary>统计已选卡组中特殊卡牌的张数（每张特殊卡最多 1 张，因此张数即特殊牌种类数）。</summary>
        private int CountSelectedSpecialCards()
        {
            var count = 0;
            foreach (var selectedId in selectedCardIds)
            {
                var selected = allCards.Find(candidate => candidate.Id == selectedId);
                if (selected != null && selected.IsSpecial) count++;
            }

            return count;
        }
        private void OnUndoClicked()
        {
            if (_isStartingBattle || selectedCardIds.Count == 0)
                return;

            selectedCardIds.RemoveAt(selectedCardIds.Count - 1);
            RuntimeGameRepository.ClearSelectedDemoStage();
            DailyAreaMapState.CancelSelectedPoint();
            RefreshMapPoints();
            RefreshSelectionUI();
        }

        public void RefreshSelectionUI()
        {
            UpdateDeckSlots();
            UpdateDeckLabel();

            if (undoButton != null)
                undoButton.interactable = !_isStartingBattle && selectedCardIds.Count > 0;
            if (beginButton != null)
            {
                var coffeeUI = FindFirstObjectByType<CoffeeSelectionUI>();
                var coffeeReady = coffeeUI == null || coffeeUI.IsSelectionComplete;
                beginButton.interactable =
                    !_isStartingBattle &&
                    DailyAreaMapState.HasSelectedPoint &&
                    RuntimeGameRepository.HasSelectedDemoStage &&
                    RuntimeGameRepository.SelectedDemoStage == DemoFlowState.CurrentStage &&
                    IsDeckComplete &&
                    coffeeReady;
            }
            if (cardButton != null)
                cardButton.interactable = !_isStartingBattle && !DemoFlowState.IsCompleted;
        }

        private void DisableDecorativeOverlayRaycasts()
        {
            if (decorativeOverlay == null)
                decorativeOverlay = transform.Find("Frame");

            if (decorativeOverlay == null)
            {
                Debug.LogWarning(
                    "[DemoFlow] Decorative overlay is not assigned; a transparent Graphic may block map clicks.",
                    this);
                return;
            }

            var disabledCount = 0;
            foreach (var graphic in decorativeOverlay.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget || graphic.GetComponent<Selectable>() != null)
                    continue;

                graphic.raycastTarget = false;
                disabledCount++;
            }

            Debug.Log(
                $"[DemoFlow] Disabled raycast interception on {disabledCount} decorative Graphics.",
                decorativeOverlay);
        }

        private void BindDemoMapPoints()
        {
            ResolveDemoMapPointsByName();
            ResolveMapPointSprites();

            if (demoMapPoints.Count < DailyAreaMapState.PointCount)
            {
                Debug.LogError(
                    $"[DemoFlow] CardSelectionUI needs {DemoFlowState.BattleCount} map points in Dog/Girl/Eye order; " +
                    $"only {demoMapPoints.Count} are configured.", this);
            }

            var count = Mathf.Min(demoMapPoints.Count, DailyAreaMapState.PointCount);
            for (var i = 0; i < count; i++)
            {
                var mapPoint = demoMapPoints[i];
                if (mapPoint == null)
                {
                    Debug.LogError($"[DemoFlow] Map point slot {i + 1} is not assigned.", this);
                    continue;
                }

                var button = mapPoint.GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogError($"[DemoFlow] {mapPoint.name} has no Button component.", mapPoint);
                    continue;
                }

                var pointIndex = i;
                UnityAction listener = () => OnDemoMapPointClicked(pointIndex);
                button.onClick.AddListener(listener);
                demoMapPointListeners[button] = listener;
            }
        }

        private void ResolveDemoMapPointsByName()
        {
            var resolvedPoints = new List<GameObject>();
            for (var i = 1; i <= DailyAreaMapState.PointCount; i++)
            {
                var mapPoint = GameObject.Find($"MapPoint_{i}");
                if (mapPoint != null) resolvedPoints.Add(mapPoint);
            }

            if (resolvedPoints.Count != DailyAreaMapState.PointCount)
                return;

            demoMapPoints.Clear();
            demoMapPoints.AddRange(resolvedPoints);
        }

        private void ResolveMapPointSprites()
        {
            if (battlePointSprite == null && demoMapPoints.Count > 0)
                battlePointSprite = demoMapPoints[0].GetComponent<Image>()?.sprite;
            if (eventPointSprite == null && demoMapPoints.Count > 3)
                eventPointSprite = demoMapPoints[3].GetComponent<Image>()?.sprite;
            if (treasurePointSprite == null && demoMapPoints.Count > 4)
                treasurePointSprite = demoMapPoints[4].GetComponent<Image>()?.sprite;

            if (battlePointSprite == null || eventPointSprite == null || treasurePointSprite == null)
            {
                Debug.LogError("[AreaMap] Battle, event, and treasure sprites must be assigned.", this);
            }
        }

        private void OnDemoMapPointClicked(int pointIndex)
        {
            if (_isStartingBattle)
                return;

            if (!DailyAreaMapState.TryGetPoint(pointIndex, out var point))
            {
                Debug.LogError($"[AreaMap] Map point index {pointIndex} is invalid.", this);
                return;
            }

            if (point.Type == AreaPointType.Event)
            {
                WarningToast.Show("Event areas are not available yet.");
                return;
            }

            if (point.Type == AreaPointType.Treasure)
            {
                StartTreasureArea(pointIndex);
                return;
            }

            if (!IsDeckComplete)
            {
                if (openCardSelectionAfterMapClick && cardPopup != null)
                    cardPopup.SetActive(true);

                WarningToast.Show(string.Format("还需选择{0}张卡牌", RequiredDeckSize));
                RefreshSelectionUI();
                return;
            }

            if (!DailyAreaMapState.TrySelectPoint(pointIndex, out var failureReason))
            {
                WarningToast.Show(failureReason);
                RefreshMapPoints();
                return;
            }

            RuntimeGameRepository.SetSelectedDemoStage(DemoFlowState.CurrentStage);
            RuntimeGameRepository.SetSelectedEncounterIndex(point.EncounterIndex);
            Debug.Log(
                $"[AreaMap] Selected map point {pointIndex + 1}: {point.Type}; " +
                $"encounter slot {point.EncounterIndex}; " +
                $"battle stage is {DemoFlowState.CurrentStage}.",
                this);

            if (cardPopup != null)
                cardPopup.SetActive(false);

            RefreshMapPoints();
            RefreshSelectionUI();
        }

        private void StartTreasureArea(int pointIndex)
        {
            if (!Application.CanStreamedLevelBeLoaded(TREASURE_SCENE_NAME))
            {
                Debug.LogError(
                    $"[AreaMap] Scene '{TREASURE_SCENE_NAME}' is not included in the active build profile.",
                    this);
                WarningToast.Show("The treasure scene is unavailable.");
                return;
            }

            if (!DailyAreaMapState.TrySelectPoint(pointIndex, out var failureReason))
            {
                WarningToast.Show(failureReason);
                RefreshMapPoints();
                return;
            }

            // Treasure does not need a combat deck or encounter slot.
            RuntimeGameRepository.ClearSelectedDemoStage();
            RuntimeGameRepository.ClearSelectedEncounterIndex();
            _isStartingBattle = true;
            RefreshMapPoints();
            RefreshSelectionUI();

            Debug.Log($"[AreaMap] Entering treasure point {pointIndex + 1}.", this);
            if (TransitionEffect.Instance != null)
                TransitionEffect.Instance.TransitionTo(TREASURE_SCENE_NAME);
            else
                StartCoroutine(LoadAreaScene(TREASURE_SCENE_NAME));
        }

        /// <summary>
        /// 为每个战斗点程序化注册教学框：显示"里面的 Boss 名字 / 血量 / 韧性"。
        /// 描述按该点随机分配到的敌人槽位生成，每局重开（重新生成地图）后自动更新。
        /// </summary>
        private void RegisterMapPointCallouts()
        {
            if (tutorialController == null)
                return;

            var count = Mathf.Min(demoMapPoints.Count, DailyAreaMapState.PointCount);
            for (var i = 0; i < count; i++)
            {
                var mapPoint = demoMapPoints[i];
                if (mapPoint == null)
                    continue;

                var owner = mapPoint.GetComponent<RectTransform>();
                if (owner == null)
                    continue;

                tutorialController.UnregisterJsonCallouts(owner);

                if (!DailyAreaMapState.TryGetPoint(i, out var point) || point.Type != AreaPointType.Battle)
                    continue;

                var definition = GetEncounterDefinition(point.EncounterIndex);
                if (definition == null)
                    continue;

                tutorialController.RegisterJsonCallout(
                    owner,
                    owner,
                    new TutorialHintJson { description = BuildEncounterDescription(definition) });
            }
        }

        private CombatantDefinition GetEncounterDefinition(int encounterIndex)
        {
            if (demoEnemyDefinitions == null || encounterIndex < 0 || encounterIndex >= demoEnemyDefinitions.Count)
                return null;

            return demoEnemyDefinitions[encounterIndex];
        }

        private static string BuildEncounterDescription(CombatantDefinition definition)
        {
            var enemyName = definition.EnemyArchetype switch
            {
                EnemyArchetype.Dog => "Dog",
                EnemyArchetype.LittleGirl => "Little Girl",
                EnemyArchetype.BigEye => "Big Eye",
                _ => definition.DisplayName
            };

            // 敌人等级写在 CombatantDefinition.EnemyRank（Minion/Elite/Boss）。
            var rankLabel = definition.EnemyRank switch
            {
                EnemyRank.Minion => "Minion",
                EnemyRank.Elite => "Elite",
                EnemyRank.Boss => "Boss",
                _ => "Enemy"
            };

            return $"{rankLabel}: {enemyName} | HP {definition.MaxHealth} | Toughness {definition.MaxToughness}";
        }
        public void RefreshMapPoints()
        {
            DailyAreaMapState.EnsureGenerated();

            var count = Mathf.Min(demoMapPoints.Count, DailyAreaMapState.PointCount);
            for (var i = 0; i < count; i++)
            {
                var mapPoint = demoMapPoints[i];
                if (mapPoint == null || !DailyAreaMapState.TryGetPoint(i, out var point))
                    continue;

                var image = mapPoint.GetComponent<Image>();
                if (image != null)
                    image.sprite = GetMapPointSprite(point.Type);

                var isVisible = !DemoFlowState.IsCompleted && !point.IsCompleted;
                mapPoint.SetActive(isVisible);

                var button = mapPoint.GetComponent<Button>();
                if (button != null)
                    button.interactable = isVisible && !point.IsSelected;
            }

            if (demoCompletePanel != null)
            {
                demoCompletePanel.SetActive(DemoFlowState.IsCompleted);
                if (DemoFlowState.IsCompleted)
                {
                    if (demoCompleteLabel != null)
                        demoCompleteLabel.text = demoCompleteText;

                    var closeButton = demoCompletePanel.transform.Find("CloseBtn");
                    if (closeButton != null) closeButton.gameObject.SetActive(false);
                }
            }
            else if (DemoFlowState.IsCompleted)
            {
                Debug.Log("[DemoFlow] Demo Complete. No completion panel is configured.", this);
            }

            RegisterMapPointCallouts();
            RefreshSelectionUI();
        }

        private Sprite GetMapPointSprite(AreaPointType type)
        {
            return type switch
            {
                AreaPointType.Battle => battlePointSprite,
                AreaPointType.Event => eventPointSprite,
                AreaPointType.Treasure => treasurePointSprite,
                _ => null
            };
        }

        [ContextMenu("Reset Demo Progress")]
        public void ResetDemoProgress()
        {
            DemoFlowState.ResetDemoProgress();
            RuntimeGameRepository.ClearSelectedDemoStage();
            RuntimeGameRepository.ClearSelectedDeck();
            selectedCardIds.Clear();

            if (demoCompletePanel != null)
            {
                var closeButton = demoCompletePanel.transform.Find("CloseBtn");
                if (closeButton != null) closeButton.gameObject.SetActive(true);
            }

            RefreshMapPoints();
        }


        private void UpdateDeckSlots()
        {
            if (deckGridContent == null) return;

            for (int i = 0; i < deckGridContent.childCount; i++)
            {
                var slot = deckGridContent.GetChild(i);
                var isRequiredSlot = i < RequiredDeckSize;
                slot.gameObject.SetActive(isRequiredSlot);
                if (!isRequiredSlot)
                    continue;

                var placeholder = slot.Find("Placeholder");

                if (i < selectedCardIds.Count)
                {
                    var card = allCards.Find(c => c.Id == selectedCardIds[i]);
                    var name = card != null ? card.DisplayName : selectedCardIds[i];
                    if (placeholder != null)
                    {
                        var text = placeholder.GetComponent<Text>();
                        if (text != null)
                        {
                            text.text = name;
                            text.fontSize = 12;
                            text.color = new Color(0.9f, 0.85f, 0.7f, 1);
                        }
                    }
                    var img = slot.GetComponent<Image>();
                    if (img != null)
                        img.color = new Color(0.2f, 0.18f, 0.14f, 1);
                }
                else
                {
                    if (placeholder != null)
                    {
                        var text = placeholder.GetComponent<Text>();
                        if (text != null)
                        {
                            text.text = "+";
                            text.fontSize = 24;
                            text.color = new Color(0.3f, 0.3f, 0.35f, 1);
                        }
                    }
                    var img = slot.GetComponent<Image>();
                    if (img != null)
                        img.color = new Color(0.12f, 0.12f, 0.15f, 1);
                }
            }
        }

        private void UpdateDeckLabel()
        {
            if (deckLabel != null)
                deckLabel.text = $"已选卡�?({selectedCardIds.Count}/{RequiredDeckSize})";
        }

        private void ResolveUndoButton()
        {
            if (undoButton != null)
                return;

            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button == cardButton || button == beginButton)
                    continue;

                var objectName = button.gameObject.name;
                var label = button.GetComponentInChildren<Text>(true);
                var labelText = label != null ? label.text : string.Empty;

                if (objectName == "UndoButton" || objectName == "UndoBtn" ||
                    objectName == "RevokeButton" || objectName == "RevokeBtn" ||
                    labelText.Contains("\u64A4\u9500") || labelText.Contains("\u9000\u9009") ||
                    labelText.Contains("\u53D6\u6D88\u9009\u62E9"))
                {
                    undoButton = button;
                    return;
                }
            }

            Debug.LogWarning("[CardSelectionUI] Undo button is not assigned. Assign it in the Inspector or name it UndoButton.");
        }

        private void OnCardButtonClicked()
        {
            if (cardPopup != null) cardPopup.SetActive(true);
        }

        private void OnBeginClicked()
        {
            if (_isStartingBattle)
                return;

            if (!RuntimeGameRepository.HasSelectedDemoStage ||
                RuntimeGameRepository.SelectedDemoStage != DemoFlowState.CurrentStage ||
                !DailyAreaMapState.HasSelectedPoint)
            {
                Debug.LogWarning(
                    "[AreaMap] Click an available battle point before starting battle.", this);
                WarningToast.Show("请先选择一个战斗区域");
                return;
            }

            var requiredDeckSize = RequiredDeckSize;
            if (!IsDeckComplete)
            {
                Debug.LogWarning(
                    $"[CardSelectionUI] Select exactly {requiredDeckSize} cards before starting.");
                WarningToast.Show(string.Format("请先选择{0}张卡牌再开始", requiredDeckSize));
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(BATTLE_SCENE_NAME))
            {
                Debug.LogError(
                    $"[CardSelectionUI] Scene '{BATTLE_SCENE_NAME}' is not included in the active build profile.");
                WarningToast.Show("The battle scene is unavailable.");
                return;
            }

            _isStartingBattle = true;
            RefreshSelectionUI();
            RuntimeGameRepository.SetSelectedDeck(selectedCardIds);
            Debug.Log(
                $"[CardSelectionUI] Starting {RuntimeGameRepository.SelectedDemoStage} with " +
                $"{selectedCardIds.Count} cards.");

            var coffeeUI = UnityEngine.Object.FindFirstObjectByType<CoffeeSelectionUI>();
            if (coffeeUI != null)
                coffeeUI.ConfirmSelection();

            Debug.Log($"[CardSelectionUI] Starting battle with {selectedCardIds.Count} cards.");

            if (TransitionEffect.Instance != null)
            {
                TransitionEffect.Instance.TransitionTo(BATTLE_SCENE_NAME);
            }
            else
            {
                StartCoroutine(LoadBattleScene());
            }
        }
        private IEnumerator LoadBattleScene()
        {
            var operation = SceneManager.LoadSceneAsync(BATTLE_SCENE_NAME, LoadSceneMode.Single);
            if (operation == null)
            {
                _isStartingBattle = false;
                RefreshSelectionUI();
                Debug.LogError($"[CardSelectionUI] Failed to start loading scene '{BATTLE_SCENE_NAME}'.");
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        private IEnumerator LoadAreaScene(string sceneName)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                _isStartingBattle = false;
                DailyAreaMapState.CancelSelectedPoint();
                RefreshMapPoints();
                Debug.LogError($"[AreaMap] Failed to start loading scene '{sceneName}'.", this);
                yield break;
            }

            while (!operation.isDone)
                yield return null;
        }

        private void RestoreSelectedDeckFromSession()
        {
            if (!RuntimeGameRepository.HasSelectedDeck)
                return;

            selectedCardIds.Clear();
            selectedCardIds.AddRange(RuntimeGameRepository.SelectedCardIds);
        }

        private void BindCloseButton(GameObject popup)
        {
            if (popup == null) return;
            var closeBtn = popup.transform.Find("CloseBtn");
            if (closeBtn != null)
            {
                var btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => popup.SetActive(false));
            }
        }

        private void CreateText(string name, Transform parent, string text, int fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var t = go.GetComponent<Text>();
            t.text = text; t.fontSize = fontSize; t.color = color;
            t.alignment = TextAnchor.UpperLeft;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private Color GetCategoryColor(string category)
        {
            return category switch
            {
                "melee" => new Color(0.6f, 0.35f, 0.2f, 1),
                "heavy" => new Color(0.65f, 0.3f, 0.18f, 1),
                "bleed" => new Color(0.65f, 0.18f, 0.2f, 1),
                "flexible" => new Color(0.3f, 0.55f, 0.25f, 1),
                "hidden" => new Color(0.28f, 0.28f, 0.38f, 1),
                "ranged" => new Color(0.2f, 0.4f, 0.6f, 1),
                "magic" => new Color(0.5f, 0.2f, 0.5f, 1),
                "misc" => new Color(0.45f, 0.4f, 0.3f, 1),
                _ => new Color(0.4f, 0.4f, 0.4f, 1),
            };
        }

        private string GetEffectsSummary(CardSpec card)
        {
            var parts = new List<string>();
            foreach (var effect in card.Effects)
            {
                var desc = effect.Type.ToString();
                if (effect.Type == CardEffectType.Damage ||
                    effect.Type == CardEffectType.ToughnessDamage ||
                    effect.Type == CardEffectType.Bleed ||
                    effect.Type == CardEffectType.LifeSteal ||
                    effect.Type == CardEffectType.ReflectDamage ||
                    effect.Type == CardEffectType.BlockDamage)
                    desc += $" x{effect.Amount.BaseValue}";
                else if (effect.Type == CardEffectType.BleedScaledDamage)
                    desc += $" x{effect.Multiplier:0.#}";
                parts.Add(desc);
            }
            return string.Join(", ", parts);
        }
    }
}
