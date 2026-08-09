using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KiKs.UI;

namespace KiKs.Combat
{
    [Serializable]
    public sealed class HuntLootReward
    {
        [SerializeField] private string resourceId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [Min(1)] [SerializeField] private int amount = 1;

        public string ResourceId => resourceId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? resourceId : displayName;
        public int Amount => amount;

        public HuntLootReward(string resourceId, string displayName, int amount)
        {
            this.resourceId = resourceId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.amount = Mathf.Max(1, amount);
        }
    }


    /// <summary>
    /// Listens for the rules-layer Victory event, grants the configured rewards
    /// exactly once, and creates an art-independent hunt result popup at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HuntResultPresenter : MonoBehaviour
    {
        private readonly List<CardSpec> rewardCards = new();

        private BattleController battleController;
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Image dimmer;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform cardArea;
        [SerializeField] private TMP_Text lootText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text extraCardsText;
        [SerializeField] private Button confirmButton;

        private bool victoryQueued;
        private bool rewardsGranted;
        private bool isTransitioning;

        private static readonly Color DimColor = new Color(0.01f, 0.015f, 0.025f, 0.84f);
        private static readonly Color PanelColor = new Color(0.055f, 0.085f, 0.105f, 0.985f);
        private static readonly Color AccentColor = new Color(0.12f, 0.83f, 0.78f, 1f);
        private static readonly Color HotColor = new Color(1f, 0.13f, 0.38f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.97f, 0.97f, 1f);
        private static readonly Color MutedColor = new Color(0.61f, 0.72f, 0.75f, 1f);

        public void Configure(BattleController controller)
        {
            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();

            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;

            battleController = controller;
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;

            if (popupRoot == null) BuildPlaceholderUI();
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Type != CombatEventType.Victory || victoryQueued) return;
            victoryQueued = true;
            StartCoroutine(ShowAfterVictoryRoutine());
        }

        private IEnumerator ShowAfterVictoryRoutine()
        {
            float delay = battleController != null ? battleController.HuntResultDelay : 0.75f;
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            GrantRewardsOnce();
            RefreshRewardUI();
            if (popupRoot == null) BuildPlaceholderUI();
            if (popupRoot == null) yield break;

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            if (rootCanvasGroup != null)
                yield return ShowRoutine();
            else if (confirmButton != null)
                confirmButton.interactable = true;
        }

        private void GrantRewardsOnce()
        {
            if (rewardsGranted || battleController == null) return;
            rewardsGranted = true;
            RuntimeGameRepository.BeginBattleRewards();

            if (battleController.HuntGoldReward > 0)
                RuntimeGameRepository.AddGold(battleController.HuntGoldReward);

            IReadOnlyList<HuntLootReward> loot = battleController.HuntLootRewards;
            if (loot != null)
            {
                foreach (HuntLootReward item in loot)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.ResourceId) || item.Amount <= 0)
                        continue;
                    RuntimeGameRepository.AddResource(item.ResourceId, item.Amount);
                }
            }

            SelectRewardCards();
            foreach (CardSpec card in rewardCards)
                RuntimeGameRepository.AddBattleRewardCard(card.Id);
        }

        private void SelectRewardCards()
        {
            rewardCards.Clear();
            if (!StaticGameRepository.HasCards || battleController.HuntRewardCardCount <= 0) return;

            var candidates = new List<CardSpec>();
            IReadOnlyList<string> configuredPool = battleController.HuntRewardCardPool;
            if (configuredPool != null && configuredPool.Count > 0)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (string cardId in configuredPool)
                {
                    if (!seen.Add(cardId)) continue;
                    if (StaticGameRepository.TryGetCard(cardId, out CardSpec card))
                    {
                        if (!card.IsEnemyCard) candidates.Add(card);
                    }
                    else
                    {
                        Debug.LogWarning($"[HuntResult] Unknown reward card id: {cardId}", this);
                    }
                }
            }
            else
            {
                foreach (var card in StaticGameRepository.PlayerCards)
                {
                    // 当前版本只发放有卡面的卡牌
                    if (string.IsNullOrEmpty(card.ImagePath)) continue;
                    candidates.Add(card);
                }
            }

            if (candidates.Count == 0) return;
            int seed = unchecked(Environment.TickCount ^ battleController.State.TurnNumber ^ GetInstanceID());
            var random = new System.Random(seed);
            int count = Mathf.Min(battleController.HuntRewardCardCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int index = random.Next(candidates.Count);
                rewardCards.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
        }

        private void RefreshRewardUI()
        {
            if (lootText == null) return;

            var builder = new System.Text.StringBuilder();
            IReadOnlyList<HuntLootReward> loot = battleController.HuntLootRewards;
            if (loot != null)
            {
                foreach (HuntLootReward item in loot)
                {
                    if (item == null || item.Amount <= 0) continue;
                    if (builder.Length > 0) builder.Append("    ");
                    builder.Append(item.DisplayName)
                        .Append("  <color=#28D8CC>x")
                        .Append(item.Amount)
                        .Append("</color>");
                }
            }
            if (builder.Length == 0)
                builder.Append("<color=#82949A>No material drops</color>");

            lootText.text = builder.ToString();
            goldText.text = $"GOLD    <color=#FFD75A>+{battleController.HuntGoldReward} G</color>";
            BuildCardRewards();
        }

        private void BuildCardRewards()
        {
            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);

            for (int i = cardArea.childCount - 1; i >= 0; i--)
                Destroy(cardArea.GetChild(i).gameObject);

            int visibleCount = Mathf.Min(3, rewardCards.Count);
            const float spacing = 236f;
            float startX = -((visibleCount - 1) * spacing) * 0.5f;
            for (int i = 0; i < visibleCount; i++)
                CreateRewardCard(rewardCards[i], new Vector2(startX + i * spacing, 0f));

            extraCardsText.text = rewardCards.Count > visibleCount
                ? $"+ {rewardCards.Count - visibleCount} MORE"
                : rewardCards.Count == 0 ? "NO CARD DROP" : string.Empty;
        }

        private void CreateRewardCard(CardSpec card, Vector2 position)
        {
            GameObject slotObject = CreateUIObject(
                "RewardCard_" + card.Id,
                cardArea,
                typeof(Image),
                typeof(Outline));
            RectTransform slot = slotObject.GetComponent<RectTransform>();
            slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = position;
            slot.sizeDelta = new Vector2(202f, 278f);
            slotObject.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.19f, 1f);
            Outline outline = slotObject.GetComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(3f, -3f);

            GameObject artObject = CreateUIObject("Art", slot, typeof(Image));
            RectTransform artRect = artObject.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.05f, 0.17f);
            artRect.anchorMax = new Vector2(0.95f, 0.96f);
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            Image art = artObject.GetComponent<Image>();
            art.sprite = CardImageLoader.LoadSprite(card.ImagePath);
            art.preserveAspect = true;
            art.color = art.sprite != null ? Color.white : new Color(0.2f, 0.24f, 0.29f, 1f);

            CreateText(
                slot,
                "Name",
                card.DisplayName.ToUpperInvariant(),
                20f,
                FontStyles.Bold,
                TextColor,
                TextAlignmentOptions.Center,
                new Vector2(0.04f, 0.02f),
                new Vector2(0.96f, 0.17f));

            if (tutorialController != null)
                tutorialController.RegisterJsonCallout(this, slot, card.Tutorial);
        }

        private IEnumerator ShowRoutine()
        {
            confirmButton.interactable = false;
            rootCanvasGroup.alpha = 0f;
            panelCanvasGroup.alpha = 1f;
            panel.localScale = new Vector3(0.88f, 0.88f, 1f);
            dimmer.color = new Color(DimColor.r, DimColor.g, DimColor.b, 0f);

            const float duration = 0.32f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rootCanvasGroup.alpha = eased;
                panel.localScale = Vector3.LerpUnclamped(
                    new Vector3(0.88f, 0.88f, 1f),
                    Vector3.one,
                    eased);
                dimmer.color = new Color(DimColor.r, DimColor.g, DimColor.b, DimColor.a * eased);
                yield return null;
            }

            rootCanvasGroup.alpha = 1f;
            panel.localScale = Vector3.one;
            dimmer.color = DimColor;
            confirmButton.interactable = true;
        }

        private void OnConfirmClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            confirmButton.interactable = false;
            StartCoroutine(TransitionAfterVictoryRoutine());
        }

        private IEnumerator TransitionAfterVictoryRoutine()
        {
            const float duration = 0.4f;
            float elapsed = 0f;
            Color startDim = dimmer.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                panelCanvasGroup.alpha = 1f - Mathf.Clamp01((t - 0.2f) / 0.8f);
                panel.localScale = Vector3.LerpUnclamped(
                    Vector3.one,
                    new Vector3(0.94f, 0.94f, 1f),
                    eased);
                dimmer.color = Color.Lerp(startDim, Color.black, eased);
                yield return null;
            }

            if (!RuntimeGameRepository.HasSelectedDemoStage)
            {
                Debug.LogWarning(
                    "[DemoFlow] Victory came from a direct/debug battle. Keeping the existing Cafe return flow.",
                    this);
                EndNightAndReturnToCafe();
                yield break;
            }

            var completedStage = RuntimeGameRepository.SelectedDemoStage;
            var advanced = DemoFlowState.CompleteCurrentBattle(completedStage);
            RuntimeGameRepository.ClearSelectedDemoStage();

            if (advanced)
                DailyAreaMapState.CompleteSelectedPoint();
            else
                DailyAreaMapState.CancelSelectedPoint();


            if (!advanced)
                Debug.LogError(
                    "[DemoFlow] Battle result did not advance progress; returning to PreBattle safely.", this);

            ReturnToPreBattle();
        }

        private static void ReturnToPreBattle()
        {
            const string sceneName = "PreBattle";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[DemoFlow] Scene '{sceneName}' is not included in the active build profile.");
                return;
            }

            if (TransitionEffect.Instance != null)
                TransitionEffect.Instance.TransitionTo(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }


        private void BuildPlaceholderUI()
        {
            if (popupRoot != null) return;

            Canvas canvas = FindSceneCanvas();
            if (canvas == null)
            {
                Debug.LogError("[HuntResult] Cannot create result UI because no Canvas exists.", this);
                return;
            }

            popupRoot = CreateUIObject("HuntResult_Placeholder", canvas.transform, typeof(CanvasGroup));
            Stretch(popupRoot.GetComponent<RectTransform>());
            rootCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;

            GameObject dimObject = CreateUIObject("Dimmer", popupRoot.transform, typeof(Image));
            Stretch(dimObject.GetComponent<RectTransform>());
            dimmer = dimObject.GetComponent<Image>();
            dimmer.color = DimColor;

            GameObject panelObject = CreateUIObject(
                "HuntPanel_Placeholder",
                popupRoot.transform,
                typeof(Image),
                typeof(Outline),
                typeof(CanvasGroup));
            panel = panelObject.GetComponent<RectTransform>();
            panelCanvasGroup = panelObject.GetComponent<CanvasGroup>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1180f, 830f);
            panel.anchoredPosition = Vector2.zero;
            panelObject.GetComponent<Image>().color = PanelColor;
            Outline panelOutline = panelObject.GetComponent<Outline>();
            panelOutline.effectColor = HotColor;
            panelOutline.effectDistance = new Vector2(4f, -4f);

            CreateBar(panel, "TopAccent", new Vector2(0f, 402f), new Vector2(1110f, 7f), HotColor);
            CreateText(
                panel, "Title", "HUNT COMPLETE", 60f, FontStyles.Bold,
                AccentColor, TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f));

            CreateText(
                panel, "LootLabel", "LOOT ACQUIRED", 22f, FontStyles.Bold,
                MutedColor, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.81f));
            lootText = CreateText(
                panel, "Loot", string.Empty, 27f, FontStyles.Normal,
                TextColor, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.75f));
            goldText = CreateText(
                panel, "Gold", string.Empty, 29f, FontStyles.Bold,
                TextColor, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.66f));

            CreateText(
                panel, "CardsLabel", "CARDS ACQUIRED", 22f, FontStyles.Bold,
                MutedColor, TextAlignmentOptions.Left,
                new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.56f));

            GameObject cardAreaObject = CreateUIObject("CardArea", panel);
            cardArea = cardAreaObject.GetComponent<RectTransform>();
            cardArea.anchorMin = new Vector2(0.08f, 0.12f);
            cardArea.anchorMax = new Vector2(0.78f, 0.49f);
            cardArea.offsetMin = cardArea.offsetMax = Vector2.zero;

            extraCardsText = CreateText(
                panel, "ExtraCards", string.Empty, 22f, FontStyles.Bold,
                AccentColor, TextAlignmentOptions.Center,
                new Vector2(0.79f, 0.23f), new Vector2(0.94f, 0.36f));

            GameObject buttonObject = CreateUIObject(
                "ConfirmButton_Placeholder",
                panel,
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.79f, 0.08f);
            buttonRect.anchorMax = new Vector2(0.94f, 0.2f);
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = AccentColor;
            confirmButton = buttonObject.GetComponent<Button>();
            confirmButton.targetGraphic = buttonImage;
            ColorBlock colors = confirmButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 1f, 0.98f, 1f);
            colors.pressedColor = new Color(0.55f, 0.78f, 0.76f, 1f);
            colors.disabledColor = new Color(0.4f, 0.48f, 0.5f, 0.7f);
            confirmButton.colors = colors;
            confirmButton.onClick.AddListener(OnConfirmClicked);
            Outline buttonOutline = buttonObject.GetComponent<Outline>();
            buttonOutline.effectColor = HotColor;
            buttonOutline.effectDistance = new Vector2(3f, -3f);

            CreateText(
                buttonRect, "Label", "OK", 36f, FontStyles.Bold,
                new Color(0.02f, 0.13f, 0.15f, 1f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);

            popupRoot.SetActive(false);
        }

        private Canvas FindSceneCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.gameObject.scene == gameObject.scene && candidate.isRootCanvas)
                    return candidate;
            }
            return FindFirstObjectByType<Canvas>();
        }

        private static void EndNightAndReturnToCafe()
        {
            Type timeSystemType = Type.GetType("TimeSystem, Assembly-CSharp");
            MethodInfo endNightMethod = timeSystemType?.GetMethod(
                "EndNightPhaseStatic",
                BindingFlags.Public | BindingFlags.Static);
            if (endNightMethod != null)
            {
                endNightMethod.Invoke(null, null);
                return;
            }

            Debug.LogWarning("[HuntResult] TimeSystem was not found; loading Cafe without advancing day.");
            SceneManager.LoadScene("Cafe");
        }

        private static GameObject CreateUIObject(
            string name,
            Transform parent,
            params Type[] extraComponents)
        {
            var components = new List<Type>
            {
                typeof(RectTransform),
                typeof(CanvasRenderer)
            };
            components.AddRange(extraComponents);
            var result = new GameObject(name, components.ToArray());
            result.layer = 5;
            result.transform.SetParent(parent, false);
            return result;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateBar(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject barObject = CreateUIObject(name, parent, typeof(Image));
            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            barObject.GetComponent<Image>().color = color;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
