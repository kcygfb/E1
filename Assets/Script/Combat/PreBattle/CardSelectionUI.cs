using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace KiKs.Combat
{
    public class CardSelectionUI : MonoBehaviour
    {
        private const int DEFAULT_DECK_SIZE = 15;
        private const string BATTLE_SCENE_NAME = "Card";

        [Header("Buttons")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Button beginButton;
        [SerializeField] private Button undoButton;

        [Header("Rules")]
        [SerializeField] private CombatRulesConfig rulesConfig;

        [Header("Popups")]
        [SerializeField] private GameObject cardPopup;

        [Header("Card Grid")]
        [SerializeField] private Transform cardGridContent;
        [SerializeField] private GameObject cardItemPrefab;

        [Header("Deck Slots")]
        [SerializeField] private Transform deckGridContent;
        [SerializeField] private Text deckLabel;

        private readonly List<string> selectedCardIds = new();
        private readonly List<CardSpec> allCards = new();
        private bool _isStartingBattle;

        private int RequiredDeckSize =>
            rulesConfig != null ? rulesConfig.ExpectedInitialDeckSize : DEFAULT_DECK_SIZE;

        private void Start()
        {
            ResolveUndoButton();

            if (cardButton != null)
                cardButton.onClick.AddListener(OnCardButtonClicked);
            if (beginButton != null)
                beginButton.onClick.AddListener(OnBeginClicked);
            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoClicked);

            BindCloseButton(cardPopup);
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
            foreach (var card in db.Repository.Cards)
                allCards.Add(card);

            PopulateCardGrid();
            RefreshSelectionUI();
        }

        private void PopulateCardGrid()
        {
            if (cardGridContent == null) return;

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

            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();

            var cardId = card.Id;
            btn.onClick.AddListener(() => OnCardClicked(cardId));
        }

        private GameObject CreateDefaultCardItem(CardSpec card)
        {
            var go = new GameObject(card.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(cardGridContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 190);
            go.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.14f, 1);

            // Category color bar
            var barGO = new GameObject("TypeBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barGO.transform.SetParent(go.transform, false);
            var barRT = barGO.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 1); barRT.anchorMax = new Vector2(1, 1);
            barRT.offsetMin = new Vector2(0, -25); barRT.offsetMax = Vector2.zero;
            barGO.GetComponent<Image>().color = GetCategoryColor(card.Category);

            // Card name
            CreateText("CardName", go.transform, card.DisplayName, 16, new Color(0.9f, 0.85f, 0.7f, 1),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(3, -50), new Vector2(-3, -28));

            // Cost
            var costText = card.CostResource == CardResourceType.ActionPoint ? "AP" : "MP";
            CreateText("CardCost", go.transform, $"{costText}: {card.CostAmount}", 14, new Color(0.8f, 0.6f, 0.3f, 1),
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

            if (selectedCardIds.Count >= RequiredDeckSize)
            {
                Debug.Log("[CardSelectionUI] Deck is full.");
                return;
            }

            selectedCardIds.Add(cardId);
            RefreshSelectionUI();
        }

        private void OnUndoClicked()
        {
            if (_isStartingBattle || selectedCardIds.Count == 0)
                return;

            selectedCardIds.RemoveAt(selectedCardIds.Count - 1);
            RefreshSelectionUI();
        }

        private void RefreshSelectionUI()
        {
            UpdateDeckSlots();
            UpdateDeckLabel();

            if (undoButton != null)
                undoButton.interactable = !_isStartingBattle && selectedCardIds.Count > 0;
            if (beginButton != null)
                beginButton.interactable =
                    !_isStartingBattle && selectedCardIds.Count == RequiredDeckSize;
            if (cardButton != null)
                cardButton.interactable = !_isStartingBattle;
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
                deckLabel.text = $"已选卡牌 ({selectedCardIds.Count}/{RequiredDeckSize})";
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

            var requiredDeckSize = RequiredDeckSize;
            if (selectedCardIds.Count != requiredDeckSize)
            {
                Debug.LogWarning(
                    $"[CardSelectionUI] Select exactly {requiredDeckSize} cards before starting.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(BATTLE_SCENE_NAME))
            {
                Debug.LogError(
                    $"[CardSelectionUI] Scene '{BATTLE_SCENE_NAME}' is not included in the active build profile.");
                return;
            }

            _isStartingBattle = true;
            RefreshSelectionUI();
            BattleSession.SetSelectedDeck(selectedCardIds);
            Debug.Log($"[CardSelectionUI] Starting battle with {selectedCardIds.Count} cards.");
            StartCoroutine(LoadBattleScene());
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
                "blades" => new Color(0.6f, 0.5f, 0.2f, 1),
                "axes" => new Color(0.5f, 0.35f, 0.15f, 1),
                "guns" => new Color(0.2f, 0.4f, 0.6f, 1),
                "flexible_weapons" => new Color(0.3f, 0.5f, 0.3f, 1),
                "hidden_weapons" => new Color(0.4f, 0.3f, 0.5f, 1),
                "defense" => new Color(0.2f, 0.5f, 0.5f, 1),
                "magic" => new Color(0.5f, 0.2f, 0.5f, 1),
                _ => new Color(0.4f, 0.4f, 0.4f, 1),
            };
        }

        private string GetEffectsSummary(CardSpec card)
        {
            var parts = new List<string>();
            foreach (var effect in card.Effects)
            {
                var desc = effect.Type.ToString();
                if (effect.Type == CardEffectType.Damage || effect.Type == CardEffectType.ToughnessDamage)
                    desc += $" x{effect.Amount.BaseValue}";
                parts.Add(desc);
            }
            return string.Join(", ", parts);
        }
    }
}
