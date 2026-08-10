using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// Treasure-room flow. NPC and card visuals are scene/prefab-authored; this controller
    /// only loads offer data and connects it to the existing battle card pipeline.
    /// </summary>
    public sealed class TreasureSceneController : MonoBehaviour
    {
        public const string SceneName = "Treasure";
        private const string ReturnSceneName = "PreBattle";
        private const string ChineseFontResourcePath = "Fonts & Materials/站酷文艺体 SDF";

        private static TMP_FontAsset chineseFont;
        private static readonly Dictionary<string, string> RewardDisplayNames = new(System.StringComparer.Ordinal)
        {
            ["ranged_rocket_launcher"] = "火箭筒",
            ["ranged_flamethrower"] = "火焰喷射器",
            ["ranged_grenade_launcher"] = "榴弹发射器",
            ["heavy_maul"] = "大锤",
            ["heavy_greatsword"] = "巨剑",
            ["bleed_shuriken"] = "手里剑",
            ["bleed_reaper"] = "镰刀",
            ["misc_magic_burst"] = "魔法爆破",
            ["BudgetBrew"] = "Budget Brew",
            ["ViscousDream"] = "Viscous Dream",
            ["FinalGaze"] = "Final Gaze",
            ["AfterTaste"] = "AfterTaste",
            ["OneSnakeTwoWays"] = "One Snake, Two Ways",
            ["TentacleLabyrinth"] = "Tentacle Labyrinth",
            ["FreeWom"] = "FreeWom",
            ["Sunset"] = "Sunset",
            ["FlameLatte"] = "Flame Latte",
            ["TheFifthFlavor"] = "The Fifth Flavor",
            ["ESSymphony"] = "E&S Symphony",
            ["snake"] = "蛇干",
            ["claw"] = "爪子",
            ["tentacle"] = "触手",
            ["oil"] = "油脂",
            ["wolffur"] = "狼毫",
            ["eye"] = "眼睛",
            ["fire"] = "紫色火焰"
        };

        private readonly Dictionary<CardView, TreasureOfferDefinition> offersByCard = new();
        private TreasurePurchaseSession session;
        private CardDealAnimator cardDealer;
        private RectTransform canvasRect;
        private RectTransform merchantPortrait;
        private RectTransform rewardTray;
        private RectTransform rewardContent;
        private TMP_Text rewardEmptyText;
        private TMP_Text coinText;
        private Button leaveButton;
        private bool isLeaving;
        private bool createdRewardTray;
        private int revealedRewardCount;
        private readonly List<GameObject> rewardTokens = new();

        private IEnumerator Start()
        {
            session = new TreasurePurchaseSession();

            ResolveSceneReferences();
            ConfigureExistingSceneUI();
            ResolveRewardTray();
            RefreshGold();

            yield return KiKs.UI.TransitionEffect.WaitEntrance();
            DealOffers(LoopProgressionRepository.GetTreasureOffers());
        }

        private void OnDestroy()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(LeaveTreasure);
            if (cardDealer != null && cardDealer.OnCardPlayed == HandleOfferPlayed)
                cardDealer.OnCardPlayed = null;

            foreach (var token in rewardTokens)
            {
                if (token == null) continue;
                DOTween.Kill(token.transform);
                Destroy(token);
            }
            rewardTokens.Clear();

            if (createdRewardTray && rewardTray != null)
                Destroy(rewardTray.gameObject);
        }

        private void ResolveSceneReferences()
        {
            var sceneCanvas = FindSceneCanvas();
            canvasRect = sceneCanvas != null ? sceneCanvas.GetComponent<RectTransform>() : null;
            cardDealer = FindSceneComponent<CardDealAnimator>();
            merchantPortrait = FindSceneObject("MerchantPortrait")?.GetComponent<RectTransform>();
            coinText = FindSceneObject("CoinText")?.GetComponent<TMP_Text>();
            leaveButton = FindSceneObject("Btn_EndTurn")?.GetComponent<Button>() ??
                          FindSceneObject("Btn_LeaveTreasure")?.GetComponent<Button>();

            if (canvasRect == null)
                Debug.LogError("[Treasure] The scene needs the existing Canvas.", this);
            if (cardDealer == null)
                Debug.LogError(
                    "[Treasure] Add and configure CardDealAnimator in the Treasure scene. " +
                    "Use Assets/Prefabs/Card_Battle.prefab as Card Prefab.",
                    this);
            if (merchantPortrait == null)
                Debug.LogWarning(
                    "[Treasure] No MerchantPortrait found. Add a UI Image named MerchantPortrait under Canvas " +
                    "and assign the merchant Sprite in its Source Image field.",
                    this);
        }

        private void ConfigureExistingSceneUI()
        {
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(LeaveTreasure);
                leaveButton.onClick.AddListener(LeaveTreasure);
                leaveButton.gameObject.name = "Btn_LeaveTreasure";

                var tmpLabel = leaveButton.GetComponentInChildren<TMP_Text>(true);
                if (tmpLabel != null)
                {
                    ApplyChineseFont(tmpLabel);
                    tmpLabel.text = "离开";
                }

                var legacyLabel = leaveButton.GetComponentInChildren<Text>(true);
                if (legacyLabel != null)
                    legacyLabel.text = "离开";
            }
            else
            {
                Debug.LogError("[Treasure] The scene needs its existing Btn_EndTurn/Btn_LeaveTreasure button.", this);
            }

            var hpText = FindSceneObject("HpText");
            if (hpText != null)
                hpText.SetActive(false);
            var hpIcon = FindSceneObject("HpIcon");
            if (hpIcon != null)
                hpIcon.SetActive(false);
        }

        private void DealOffers(IReadOnlyList<TreasureOfferDefinition> offers)
        {
            if (cardDealer == null || offers == null)
                return;

            cardDealer.OnCardPlayed = HandleOfferPlayed;
            var count = offers.Count;
            var dealtOffers = new List<KeyValuePair<CardView, TreasureOfferDefinition>>(count);
            for (var index = 0; index < count; index++)
            {
                var offer = offers[index];
                try
                {
                    var card = cardDealer.DrawCard(
                        CreateOfferCardSpec(offer),
                        offer.id,
                        useUnscaledTime: true);
                    if (card == null)
                    {
                        Debug.LogError($"[Treasure] Failed to create the {offer.price}C offer card.", this);
                        continue;
                    }

                    offersByCard[card] = offer;
                    dealtOffers.Add(new KeyValuePair<CardView, TreasureOfferDefinition>(card, offer));
                    HideCombatOnlyCardText(card);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(new System.InvalidOperationException(
                        $"[Treasure] Failed while dealing the {offer?.price ?? 0}C offer; continuing with the remaining tiers.",
                        exception), this);
                }
            }

            foreach (var pair in dealtOffers)
            {
                try
                {
                    if (session.IsFullyOwned(pair.Value))
                        MarkOfferFullyOwned(pair.Key);
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(new System.InvalidOperationException(
                        $"[Treasure] Failed to refresh ownership for the {pair.Value.price}C offer.",
                        exception), this);
                }
            }

            if (offersByCard.Count != count)
                Debug.LogError($"[Treasure] Expected {count} offer cards but dealt {offersByCard.Count}.", this);
        }

        private bool HandleOfferPlayed(CardView card)
        {
            if (card == null || session == null || isLeaving || !offersByCard.TryGetValue(card, out var offer))
                return false;

            var result = session.TryPurchase(offer);
            switch (result.Status)
            {
                case TreasurePurchaseStatus.Success:
                    offersByCard.Remove(card);
                    RefreshGold();
                    RevealReward(offer, result.Reward);
                    Debug.Log(
                        $"[Treasure] Purchased '{offer.id}' for {offer.price}C; " +
                        $"applied its configured reward bundle. Gold: {session.Gold}.",
                        this);
                    // CardDealAnimator receives true and runs its normal DiscardCard path,
                    // including CardView.PlayDiscardAnimation.
                    return true;

                case TreasurePurchaseStatus.InsufficientGold:
                    KiKs.UI.WarningToast.Show($"金币不足：需要 {offer.price}C，当前有 {session.Gold}C。");
                    return false;

                case TreasurePurchaseStatus.AlreadyPurchased:
                    KiKs.UI.WarningToast.Show("这个档位本次已经购买过了。");
                    return false;

                case TreasurePurchaseStatus.AllRewardsOwned:
                    KiKs.UI.WarningToast.Show("该档奖励已全部拥有。");
                    MarkOfferFullyOwned(card);
                    return false;

                default:
                    KiKs.UI.WarningToast.Show("商品配置无效，无法购买。");
                    return false;
            }
        }

        private static CardSpec CreateOfferCardSpec(TreasureOfferDefinition offer)
        {
            var visualOnlyEffect = new CardEffectSpec(
                CardEffectType.BlockDamage,
                UpgradeableNumber.Zero,
                UpgradeableNumber.One,
                ValueUnit.Points,
                0d);

            return new CardSpec(
                id: offer.id,
                displayNameZhCn: string.Empty,
                displayNameEn: string.Empty,
                category: "treasure",
                costResource: CardResourceType.ActionPoint,
                costAmount: 0,
                isSpecial: false,
                targetType: CardTargetType.SingleEnemy,
                effects: new[] { visualOnlyEffect },
                imagePath: offer.imagePath ?? string.Empty);
        }

        private static void HideCombatOnlyCardText(CardView card)
        {
            foreach (var text in card.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "CardNameText" || text.name == "DescriptionText" ||
                    text.name == "DamageText" || text.name == "ToughnessText")
                    text.gameObject.SetActive(false);
            }
        }

        private void ResolveRewardTray()
        {
            rewardTray = FindSceneObject("TreasureRewardTray")?.GetComponent<RectTransform>();
            if (rewardTray == null)
            {
                if (canvasRect == null) return;
                rewardTray = CreateRuntimeArea(
                    "TreasureRewardTray",
                    canvasRect,
                    new Vector2(0f, -170f),
                    new Vector2(960f, 150f));
                createdRewardTray = true;
            }

            rewardTray.SetAsLastSibling();
            rewardTray.localScale = Vector3.one;
            rewardTray.anchorMin = new Vector2(0.5f, 0.5f);
            rewardTray.anchorMax = new Vector2(0.5f, 0.5f);
            rewardTray.pivot = new Vector2(0.5f, 0.5f);
            rewardTray.anchoredPosition = new Vector2(0f, -170f);
            rewardTray.sizeDelta = new Vector2(960f, 150f);

            var background = rewardTray.GetComponent<Image>() ?? rewardTray.gameObject.AddComponent<Image>();
            background.color = new Color32(24, 22, 31, 224);
            background.raycastTarget = false;
            var outline = rewardTray.GetComponent<Outline>() ?? rewardTray.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(198, 157, 88, 170);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var title = rewardTray.Find("RewardTrayTitle")?.GetComponent<TextMeshProUGUI>();
            if (title == null)
                title = CreateText("RewardTrayTitle", rewardTray, "本次收获", 22f, new Color32(244, 211, 145, 255));
            ApplyChineseFont(title);
            title.text = "本次收获";
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            title.rectTransform.sizeDelta = new Vector2(-28f, 30f);
            title.alignment = TextAlignmentOptions.Center;

            rewardContent = rewardTray.Find("RewardContent")?.GetComponent<RectTransform>();
            if (rewardContent == null)
            {
                var contentObject = new GameObject("RewardContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                contentObject.layer = 5;
                contentObject.transform.SetParent(rewardTray, false);
                rewardContent = contentObject.GetComponent<RectTransform>();
            }
            rewardContent.anchorMin = Vector2.zero;
            rewardContent.anchorMax = Vector2.one;
            rewardContent.offsetMin = new Vector2(14f, 12f);
            rewardContent.offsetMax = new Vector2(-14f, -44f);
            rewardContent.localScale = Vector3.one;

            var layout = rewardContent.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 3, 3);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            rewardEmptyText = rewardTray.Find("RewardEmptyText")?.GetComponent<TextMeshProUGUI>();
            if (rewardEmptyText == null)
                rewardEmptyText = CreateText(
                    "RewardEmptyText",
                    rewardTray,
                    "购买后会在这里显示本次获得的内容",
                    17f,
                    new Color32(184, 178, 190, 255));
            rewardEmptyText.rectTransform.anchorMin = new Vector2(0f, 0f);
            rewardEmptyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            rewardEmptyText.rectTransform.offsetMin = new Vector2(20f, 10f);
            rewardEmptyText.rectTransform.offsetMax = new Vector2(-20f, -42f);
            rewardEmptyText.alignment = TextAlignmentOptions.Center;
            rewardEmptyText.raycastTarget = false;
        }

        private void RevealReward(TreasureOfferDefinition offer, RewardGrantResult reward)
        {
            if (offer == null || reward == null || rewardContent == null || rewardTray == null)
                return;

            if (rewardEmptyText != null)
                rewardEmptyText.gameObject.SetActive(false);

            var token = new GameObject(
                $"Reward_{offer.price}C_{revealedRewardCount + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LayoutElement),
                typeof(Outline));
            token.layer = 5;
            token.transform.SetParent(rewardContent, false);
            rewardTokens.Add(token);

            var tokenRect = token.GetComponent<RectTransform>();
            tokenRect.localScale = Vector3.one;
            var layoutElement = token.GetComponent<LayoutElement>();
            layoutElement.minWidth = 150f;
            layoutElement.preferredWidth = 215f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = 76f;
            layoutElement.preferredHeight = 90f;
            layoutElement.flexibleHeight = 1f;

            var tokenImage = token.GetComponent<Image>();
            tokenImage.color = new Color32(69, 73, 94, 245);
            tokenImage.raycastTarget = false;
            var tokenOutline = token.GetComponent<Outline>();
            tokenOutline.effectColor = new Color32(222, 184, 111, 145);
            tokenOutline.effectDistance = new Vector2(1f, -1f);
            tokenOutline.useGraphicAlpha = true;

            var header = CreateText(
                "RewardHeader",
                tokenRect,
                $"{offer.price}C 收获",
                17f,
                new Color32(255, 221, 151, 255));
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -5f);
            header.rectTransform.sizeDelta = new Vector2(-12f, 24f);
            header.alignment = TextAlignmentOptions.Center;

            var body = CreateText("RewardBody", tokenRect, FormatReward(reward), 15f, Color.white);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = new Vector2(7f, 5f);
            body.rectTransform.offsetMax = new Vector2(-7f, -28f);
            body.enableAutoSizing = true;
            body.fontSizeMin = 10f;
            body.fontSizeMax = 15f;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;
            body.alignment = TextAlignmentOptions.Center;

            revealedRewardCount++;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardTray);

            var canvasGroup = token.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            tokenRect.localScale = Vector3.one * 0.82f;
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(1f, 0.18f));
            sequence.Join(tokenRect.DOScale(1f, 0.32f).SetEase(Ease.OutBack));
            sequence.SetLink(token, LinkBehaviour.KillOnDestroy);
        }

        private static string FormatReward(RewardGrantResult reward)
        {
            if (reward == null) return "无奖励";
            var lines = new List<string>();
            if (reward.GoldGranted > 0) lines.Add($"金币 +{reward.GoldGranted}");
            foreach (var resource in reward.ResourcesGranted)
                lines.Add($"{GetRewardDisplayName(resource.ResourceId)} ×{resource.Amount}");
            foreach (var cardId in reward.NewCardIds)
                lines.Add($"卡牌：{GetRewardDisplayName(cardId)}");
            foreach (var recipeId in reward.NewRecipeIds)
                lines.Add($"菜谱：{GetRewardDisplayName(recipeId)}");
            foreach (var cardId in reward.ExistingCardIds)
                lines.Add($"已拥有：{GetRewardDisplayName(cardId)}");
            foreach (var recipeId in reward.ExistingRecipeIds)
                lines.Add($"已拥有：{GetRewardDisplayName(recipeId)}");
            return lines.Count > 0 ? string.Join("\n", lines) : "无新增内容";
        }

        private static string GetRewardDisplayName(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && RewardDisplayNames.TryGetValue(id, out var displayName)
                ? displayName
                : id ?? string.Empty;
        }
        private static void MarkOfferFullyOwned(CardView card)
        {
            if (card == null) return;
            card.enabled = false;
            var draggable = card.GetComponent<KiKs.UI.Draggable>();
            if (draggable != null) draggable.enabled = false;
            var interaction = card.GetComponent<KiKs.UI.CardInteraction>();
            if (interaction != null) interaction.enabled = false;
            var group = card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.55f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var label = CreateText(
                "FullyOwnedLabel",
                card.GetComponent<RectTransform>(),
                "已全部拥有",
                20f,
                new Color32(255, 220, 145, 255));
            label.fontStyle = FontStyles.Bold;
        }
        private void RefreshGold()
        {
            if (coinText != null && session != null)
                coinText.text = $"{session.Gold}C";
        }

        private void LeaveTreasure()
        {
            if (isLeaving) return;

            var completion = RuntimeGameRepository.CompleteSelectedArea(defeated: false);
            var nextSceneName = string.IsNullOrWhiteSpace(completion.NextSceneName)
                ? "PreBattle"
                : completion.NextSceneName;

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"[Treasure] Scene '{nextSceneName}' is not included in the active build profile.", this);
                return;
            }

            isLeaving = true;
            if (leaveButton != null) leaveButton.interactable = false;
            if (KiKs.UI.TransitionEffect.Instance != null)
                KiKs.UI.TransitionEffect.Instance.TransitionTo(nextSceneName);
            else
                SceneManager.LoadScene(nextSceneName);
        }

        private Canvas FindSceneCanvas()
        {
            var targetScene = gameObject.scene;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.gameObject.scene == targetScene && canvas.isRootCanvas)
                    return canvas;
            }
            return null;
        }

        private T FindSceneComponent<T>() where T : Component
        {
            var targetScene = gameObject.scene;
            foreach (var component in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component != null && component.gameObject.scene == targetScene)
                    return component;
            }
            return null;
        }

        private GameObject FindSceneObject(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName)) return null;
            var targetScene = gameObject.scene;
            if (!targetScene.IsValid()) return null;

            foreach (var root in targetScene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                        return child.gameObject;
                }
            }
            return null;
        }
        private static RectTransform CreateRuntimeArea(
            string name,
            RectTransform parent,
            Vector2 position,
            Vector2 size)
        {
            var area = new GameObject(name, typeof(RectTransform));
            area.layer = 5;
            area.transform.SetParent(parent, false);
            var rect = area.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            string content,
            float fontSize,
            Color color)
        {
            var textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = 5;
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            ApplyChineseFont(text);
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyChineseFont(TMP_Text text)
        {
            if (text == null)
                return;

            if (chineseFont == null)
                chineseFont = Resources.Load<TMP_FontAsset>(ChineseFontResourcePath);

            if (chineseFont != null)
                text.font = chineseFont;
            else
                Debug.LogWarning(
                    $"[Treasure] Chinese TMP font not found at Resources/{ChineseFontResourcePath}.");
        }

        private Vector2 GetCanvasPosition(RectTransform target)
        {
            if (target == null || canvasRect == null)
                return Vector2.zero;
            return canvasRect.InverseTransformPoint(target.position);
        }
    }

    internal static class TreasureSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != TreasureSceneController.SceneName ||
                Object.FindFirstObjectByType<TreasureSceneController>() != null)
                return;

            new GameObject(nameof(TreasureSceneController)).AddComponent<TreasureSceneController>();
        }
    }
}
