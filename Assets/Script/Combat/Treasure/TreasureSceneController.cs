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

        private readonly Dictionary<CardView, TreasureOfferDefinition> offersByCard = new();
        private TreasurePurchaseSession session;
        private CardDealAnimator cardDealer;
        private RectTransform canvasRect;
        private RectTransform merchantPortrait;
        private RectTransform rewardTray;
        private TMP_Text coinText;
        private Button leaveButton;
        private bool isLeaving;
        private int revealedRewardCount;

        private IEnumerator Start()
        {
            var definition = TreasureJsonRepository.Load();
            session = new TreasurePurchaseSession(definition.testStartingGold);

            ResolveSceneReferences();
            ConfigureExistingSceneUI();
            ResolveRewardTray();
            RefreshGold();

            yield return KiKs.UI.TransitionEffect.WaitEntrance();
            DealOffers(definition.offers);
        }

        private void OnDestroy()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(LeaveTreasure);
            if (cardDealer != null && cardDealer.OnCardPlayed == HandleOfferPlayed)
                cardDealer.OnCardPlayed = null;
        }

        private void ResolveSceneReferences()
        {
            var canvasObject = GameObject.Find("Canvas");
            canvasRect = canvasObject != null ? canvasObject.GetComponent<RectTransform>() : null;
            cardDealer = FindFirstObjectByType<CardDealAnimator>();
            merchantPortrait = GameObject.Find("MerchantPortrait")?.GetComponent<RectTransform>();
            coinText = GameObject.Find("CoinText")?.GetComponent<TMP_Text>();
            leaveButton = GameObject.Find("Btn_EndTurn")?.GetComponent<Button>() ??
                          GameObject.Find("Btn_LeaveTreasure")?.GetComponent<Button>();

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

            var hpText = GameObject.Find("HpText");
            if (hpText != null)
                hpText.SetActive(false);
            var hpIcon = GameObject.Find("HpIcon");
            if (hpIcon != null)
                hpIcon.SetActive(false);
        }

        private void DealOffers(IReadOnlyList<TreasureOfferDefinition> offers)
        {
            if (cardDealer == null || offers == null)
                return;

            cardDealer.OnCardPlayed = HandleOfferPlayed;
            var count = Mathf.Min(TreasureJsonRepository.RequiredOfferCount, offers.Count);
            for (var index = 0; index < count; index++)
            {
                var offer = offers[index];
                var card = cardDealer.DrawCard(CreateOfferCardSpec(offer), offer.id);
                if (card == null)
                    continue;

                offersByCard[card] = offer;
                HideCombatOnlyCardText(card);
            }
        }

        private bool HandleOfferPlayed(CardView card)
        {
            if (card == null || session == null || isLeaving || !offersByCard.TryGetValue(card, out var offer))
                return false;

            var result = session.TryPurchase(offer, () => Random.value);
            switch (result.Status)
            {
                case TreasurePurchaseStatus.Success:
                    offersByCard.Remove(card);
                    RefreshGold();
                    RevealReward(result.Reward);
                    Debug.Log(
                        $"[Treasure] Purchased '{offer.id}' for {offer.price}C; " +
                        $"revealed {result.Reward.GetDisplayText()}. Test gold: {session.Gold}.",
                        this);
                    // CardDealAnimator receives true and runs its normal DiscardCard path,
                    // including CardView.PlayDiscardAnimation.
                    return true;

                case TreasurePurchaseStatus.InsufficientGold:
                    KiKs.UI.WarningToast.Show($"金币不足：需要 {offer.price}C，当前有 {session.Gold}C。");
                    return false;

                case TreasurePurchaseStatus.AlreadyPurchased:
                    KiKs.UI.WarningToast.Show("这张购买牌已经使用过了。");
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
            rewardTray = GameObject.Find("TreasureRewardTray")?.GetComponent<RectTransform>();
            if (rewardTray != null || canvasRect == null)
                return;

            // Temporary reward readout until the authored acquisition UI is ready.
            rewardTray = CreateRuntimeArea(
                "TreasureRewardTray",
                canvasRect,
                new Vector2(0f, -155f),
                new Vector2(760f, 110f));
            var background = rewardTray.gameObject.AddComponent<Image>();
            background.color = new Color32(32, 28, 39, 185);
            background.raycastTarget = false;

            var title = CreateText(
                "RewardTrayTitle",
                rewardTray,
                "已获得",
                19f,
                new Color32(210, 188, 142, 255));
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(12f, -6f);
            title.rectTransform.sizeDelta = new Vector2(120f, 30f);
            title.alignment = TextAlignmentOptions.Left;
        }

        private void RevealReward(TreasureRewardDefinition reward)
        {
            if (reward == null || canvasRect == null || rewardTray == null)
                return;

            var token = new GameObject(
                $"Reward_{reward.id}_{revealedRewardCount + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            token.layer = 5;
            token.transform.SetParent(canvasRect, false);
            var tokenRect = token.GetComponent<RectTransform>();
            tokenRect.anchorMin = new Vector2(0.5f, 0.5f);
            tokenRect.anchorMax = new Vector2(0.5f, 0.5f);
            tokenRect.sizeDelta = new Vector2(165f, 58f);
            tokenRect.anchoredPosition = merchantPortrait != null
                ? GetCanvasPosition(merchantPortrait)
                : new Vector2(300f, 165f);

            var tokenImage = token.GetComponent<Image>();
            tokenImage.color = string.Equals(reward.type, "card", System.StringComparison.OrdinalIgnoreCase)
                ? new Color32(87, 102, 142, 255)
                : new Color32(116, 82, 54, 255);
            tokenImage.raycastTarget = false;
            CreateText("RewardName", tokenRect, reward.GetDisplayText(), 22f, Color.white);

            var targetX = -270f + revealedRewardCount * 180f;
            var targetCanvasPosition = GetCanvasPosition(rewardTray) + new Vector2(targetX, -12f);
            revealedRewardCount++;

            var canvasGroup = token.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            tokenRect.localScale = Vector3.one * 0.45f;
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(1f, 0.14f));
            sequence.Join(tokenRect.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            sequence.Join(tokenRect.DOAnchorPos(targetCanvasPosition, 0.52f).SetEase(Ease.OutBounce));
        }

        private void RefreshGold()
        {
            if (coinText != null && session != null)
                coinText.text = $"{session.Gold}C";
        }

        private void LeaveTreasure()
        {
            if (isLeaving)
                return;

            isLeaving = true;
            if (leaveButton != null)
                leaveButton.interactable = false;

            if (DailyAreaMapState.HasSelectedPoint &&
                DailyAreaMapState.TryGetPoint(DailyAreaMapState.SelectedPointIndex, out var point) &&
                point.Type == AreaPointType.Treasure)
                DailyAreaMapState.CompleteSelectedPointWithoutCountingExploration();

            if (!Application.CanStreamedLevelBeLoaded(ReturnSceneName))
            {
                Debug.LogError($"[Treasure] Scene '{ReturnSceneName}' is not included in the active build profile.", this);
                isLeaving = false;
                if (leaveButton != null)
                    leaveButton.interactable = true;
                return;
            }

            if (KiKs.UI.TransitionEffect.Instance != null)
                KiKs.UI.TransitionEffect.Instance.TransitionTo(ReturnSceneName);
            else
                SceneManager.LoadScene(ReturnSceneName);
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
