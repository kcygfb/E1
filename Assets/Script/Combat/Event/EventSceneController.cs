using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace KiKs.Combat
{
    /// <summary>
    /// 事件区内本地对话 DTO（与全局 DialogueLineJson 字段一致，
    /// 读取同样的 StreamingAssets/Dialogue/*.json，避免跨 asmdef 依赖 Assembly-CSharp）。
    /// </summary>
    [System.Serializable]
    internal sealed class EventDialogueLine
    {
        public string speaker;
        public string text;
        public string expression;
    }

    [System.Serializable]
    internal sealed class EventDialogueData
    {
        public string dialogueId;
        public List<EventDialogueLine> lines = new();
    }

    [System.Serializable]
    internal sealed class EventDialogueFile
    {
        public List<EventDialogueData> dialogues = new();
    }

    /// <summary>
    /// 事件区内的轻量对话加载器，镜像 DialogueRepository 行为：
    /// 读取 StreamingAssets/Dialogue/*.json 多对话格式，按 dialogueId 查询。
    /// </summary>
    internal static class EventDialogueLoader
    {
        private static readonly Dictionary<string, EventDialogueData> _dialogues = new(System.StringComparer.Ordinal);
        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var dir = Path.Combine(Application.streamingAssetsPath, "Dialogue");
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[EventDialogue] Directory not found: {dir}");
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                string json;
                try { json = File.ReadAllText(file); }
                catch (Exception e) { Debug.LogWarning($"[EventDialogue] Cannot read {Path.GetFileName(file)}: {e.Message}"); continue; }

                try
                {
                    var fileData = JsonUtility.FromJson<EventDialogueFile>(json);
                    if (fileData != null && fileData.dialogues != null && fileData.dialogues.Count > 0)
                    {
                        foreach (var d in fileData.dialogues)
                            if (d != null && !string.IsNullOrEmpty(d.dialogueId))
                                _dialogues[d.dialogueId] = d;
                    }
                    else
                    {
                        var single = JsonUtility.FromJson<EventDialogueData>(json);
                        if (single != null && !string.IsNullOrEmpty(single.dialogueId))
                            _dialogues[single.dialogueId] = single;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EventDialogue] Cannot parse {Path.GetFileName(file)}: {e.Message}");
                }
            }

            Debug.Log($"[EventDialogue] Loaded {_dialogues.Count} dialogues.");
        }

        public static EventDialogueData Get(string dialogueId)
        {
            EnsureLoaded();
            _dialogues.TryGetValue(dialogueId, out var d);
            return d;
        }
    }
    /// <summary>
    /// Event-area flow: intro dialogue → deal 4 cards → player picks one → resolve effect/kill/end → leave.
    /// Lightweight dialogue playback built in (no dependency on CustomerController).
    /// Reward display reused from treasure area pattern.
    /// </summary>
    public sealed class EventSceneController : MonoBehaviour
    {
        public const string SceneName = "Event";
        private const string ReturnSceneName = "PreBattle";
        private const string ChineseFontResourcePath = "Fonts & Materials/站酷文艺体 SDF";
        private const string PlayerName = "艾薇儿";
        private const float CharsPerSecond = 30f;

        private static TMP_FontAsset chineseFont;

        private readonly Dictionary<CardView, EventCardDefinition> cardsByView = new();

        private CardDealAnimator cardDealer;
        private RectTransform canvasRect;
        private Image npcPortrait;
        private RectTransform rewardTray;
        private TMP_Text coinText;
        private TMP_Text _hpText;
        private Button leaveButton;
        private bool isLeaving;
        private int revealedRewardCount;

        // --- 对话 UI ---
        private RectTransform dialoguePanel;
        private Text speakerText;
        private Text lineText;
        private Button nextButton;
        private GameObject nextWordIcon;
        private Animator _nextWordAnimator;

        // --- 对话播放状态 ---
        private bool _dialogueRunning;
        private bool _isTyping;
        private bool _typingDone;
        private bool _waitingNext;
        private Coroutine _typingRoutine;
        private string _currentFullText;
        private readonly Color _playerColor = new(0.4f, 0.8f, 1f, 1f);
        private readonly Color _npcColor = Color.white;

        // --- 离开按钮文字 ---
        private static readonly Color NpcPortraitDefaultColor = new(1f, 1f, 1f, 1f);

        private void Awake()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
        }

        private void OnDestroy()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(LeaveEvent);
            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnNextClicked);
            if (cardDealer != null && cardDealer.OnCardPlayed == HandleEventCardPlayed)
                cardDealer.OnCardPlayed = null;
        }

        private void Update()
        {
            if (!_dialogueRunning) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb[UnityEngine.InputSystem.Key.F8].wasPressedThisFrame)
            {
                Debug.Log("[EventScene] F8 skip dialogue");
                EndDialogueEarly();
            }
        }

        // ==================== 阶段 2: Start ====================

        private IEnumerator Start()
        {
            Debug.Log("[EventScene] Start() begin.");

            ResolveSceneReferences();
            ConfigureSceneUI();
            ResolveRewardTray();
            RefreshGoldDisplay();

            // 加载对话 JSON（本地加载器，不依赖 Cafe 场景的 DialogueRepository）
            EventDialogueLoader.EnsureLoaded();
            Debug.Log($"[EventScene] DialogueLoader loaded. EventDialogueLoader has {EventDialogueLoader.Get("evt_001_intro")?.lines?.Count ?? -1} lines for evt_001_intro.");

            yield return KiKs.UI.TransitionEffect.WaitEntrance();
            Debug.Log("[EventScene] Entrance complete.");

            var evt = EventSelectionState.CurrentEvent;
            if (evt == null)
            {
                Debug.LogWarning("[EventScene] No current event set; leaving.");
                LeaveEvent();
                yield break;
            }

            Debug.Log($"[EventScene] Current event: id={evt.id}, npcId={evt.npcId}, introDialogueId={evt.introDialogueId}, cards={evt.cards?.Length}");

            // 设置 NPC 立绘默认表情
            SetNpcDefaultPortrait(evt.npcId);

            // 阶段 3: 播放初始对话
            yield return PlayDialogue(evt.introDialogueId);

            // 阶段 4: 发 4 张事件卡
            DealCards(evt.cards);
            Debug.Log("[EventScene] Cards dealt. Waiting for player selection.");
        }

        // ==================== 场景引用解析 ====================

        private void ResolveSceneReferences()
        {
            var canvasObject = GameObject.Find("Canvas");
            canvasRect = canvasObject != null ? canvasObject.GetComponent<RectTransform>() : null;
            cardDealer = FindFirstObjectByType<CardDealAnimator>();
            npcPortrait = GameObject.Find("MerchantPortrait")?.GetComponent<Image>();
            coinText = GameObject.Find("CoinText")?.GetComponent<TMP_Text>();
            leaveButton = GameObject.Find("Btn_EndTurn")?.GetComponent<Button>()
                          ?? GameObject.Find("Btn_LeaveEvent")?.GetComponent<Button>();

            // 对话 UI — DialoguePanel 默认 inactive，GameObject.Find 找不到，
            // 需要在 Canvas 子物体里递归查找
            dialoguePanel = FindChildRecursive(canvasObject, "DialoguePanel")?.GetComponent<RectTransform>();
            speakerText = FindChildRecursive(canvasObject, "SpeakerText")?.GetComponent<Text>();
            lineText = FindChildRecursive(canvasObject, "LineText")?.GetComponent<Text>();
            nextButton = FindChildRecursive(canvasObject, "Btn_NextDialogue")?.GetComponent<Button>();
            nextWordIcon = FindChildRecursive(canvasObject, "NextWordIcon");
            if (nextWordIcon != null)
            {
                _nextWordAnimator = nextWordIcon.GetComponent<Animator>();
                if (_nextWordAnimator == null)
                    _nextWordAnimator = nextWordIcon.AddComponent<Animator>();
                _nextWordAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animations/NextWord/NextWordController");
                _nextWordAnimator.enabled = false;
            }

            if (canvasRect == null)
                Debug.LogError("[EventScene] Canvas not found.", this);
            if (cardDealer == null)
                Debug.LogError("[EventScene] CardDealAnimator not found.", this);
            if (dialoguePanel == null)
                Debug.LogError("[EventScene] DialoguePanel not found in scene.", this);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);
        }

        private static GameObject FindChildRecursive(GameObject root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root.transform)
            {
                var found = FindChildRecursive(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }

        private void ConfigureSceneUI()
        {
            // 离开按钮
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(LeaveEvent);
                leaveButton.onClick.AddListener(LeaveEvent);
                leaveButton.gameObject.name = "Btn_LeaveEvent";

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

            // 隐藏 HP UI — 事件区需要显示血量（选项会扣血）
            var hpText = FindChildRecursive(GameObject.Find("Canvas"), "HpText");
            if (hpText != null)
            {
                hpText.SetActive(true);
                _hpText = hpText.GetComponent<TMP_Text>();
            }
            var hpIcon = FindChildRecursive(GameObject.Find("Canvas"), "HpIcon");
            if (hpIcon != null) hpIcon.SetActive(true);
            RefreshHpDisplay();

            // 对话面板初始隐藏
            if (dialoguePanel != null)
                dialoguePanel.gameObject.SetActive(false);
        }

        private void RefreshGoldDisplay()
        {
            if (coinText != null)
                coinText.text = $"{RuntimeGameRepository.Gold}C";
        }

        private void RefreshHpDisplay()
        {
            if (_hpText != null)
                _hpText.text = $"{PlayerGlobalStats.CurrentHealth}/{PlayerGlobalStats.MaxHealth}";
        }

        private void ResolveRewardTray()
        {
            rewardTray = GameObject.Find("EventRewardTray")?.GetComponent<RectTransform>()
                         ?? GameObject.Find("TreasureRewardTray")?.GetComponent<RectTransform>();

            if (rewardTray != null || canvasRect == null)
                return;

            rewardTray = CreateRuntimeArea(
                "EventRewardTray",
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

        // ==================== 阶段 3: 轻量对话播放 ====================

        private IEnumerator PlayDialogue(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId))
            {
                Debug.LogWarning("[EventScene] Empty dialogueId; skipping.");
                yield break;
            }

            var data = EventDialogueLoader.Get(dialogueId);
            if (data == null || data.lines == null || data.lines.Count == 0)
            {
                Debug.LogWarning("[EventScene] Dialogue not found or empty: " + dialogueId);
                yield break;
            }

            Debug.Log($"[EventScene] Playing dialogue '{dialogueId}' with {data.lines.Count} lines.");

            _dialogueRunning = true;
            if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);
            if (nextButton != null) nextButton.interactable = true;
            if (nextWordIcon != null) nextWordIcon.SetActive(true);
            if (_nextWordAnimator != null) _nextWordAnimator.enabled = false;

            for (var i = 0; i < data.lines.Count; i++)
            {
                var line = data.lines[i];
                Debug.Log($"[EventScene] Line {i}: speaker='{line.speaker}', text='{line.text}'");

                // 说话者名字 + 颜色
                if (speakerText != null)
                {
                    speakerText.text = line.speaker ?? string.Empty;
                    speakerText.color = line.speaker == PlayerName ? _playerColor : _npcColor;
                }

                // 立绘弹跳
                AnimateSpeaker(line.speaker);

                // 表情切换
                SetExpression(line.speaker, line.expression);

                // 打字机 — 用 flag 轮询，不用 yield return coroutine
                // （StopCoroutine + yield return 组合在 Unity 中不可靠，会导致父协程卡住）
                _currentFullText = line.text ?? string.Empty;
                _typingDone = false;
                _isTyping = true;
                _typingRoutine = StartCoroutine(TypeText(_currentFullText));

                while (_isTyping && _dialogueRunning)
                    yield return null;

                // 如果是跳过打字（OnNextClicked 停了协程），确保全文显示
                if (lineText != null) lineText.text = _currentFullText;
                if (nextWordIcon != null) nextWordIcon.SetActive(true);
                if (_nextWordAnimator != null) _nextWordAnimator.enabled = true;

                // 等玩家点击下一句
                _waitingNext = true;
                while (_waitingNext && _dialogueRunning)
                    yield return null;
            }

            EndDialogue();
            Debug.Log($"[EventScene] Dialogue '{dialogueId}' completed.");
        }

        private IEnumerator TypeText(string fullText)
        {
            _isTyping = true;
            if (lineText != null) lineText.text = "";
            if (nextWordIcon != null) nextWordIcon.SetActive(true);
            if (_nextWordAnimator != null) _nextWordAnimator.enabled = false;

            var delay = 1f / CharsPerSecond;
            for (var i = 0; i < fullText.Length; i++)
            {
                // 如果 OnNextClicked 设了 _isTyping=false，提前退出
                if (!_isTyping) break;
                if (lineText != null)
                    lineText.text = fullText.Substring(0, i + 1);
                yield return new WaitForSeconds(delay);
            }

            _isTyping = false;
            if (nextWordIcon != null) nextWordIcon.SetActive(true);
            if (_nextWordAnimator != null) _nextWordAnimator.enabled = true;
        }

        private void OnNextClicked()
        {
            if (!_dialogueRunning) return;

            if (_isTyping)
            {
                // 跳过打字动画：直接设 flag 让 PlayDialogue 的 while 循环退出
                // 不用 StopCoroutine（和 yield return 组合不可靠）
                _isTyping = false;
                return;
            }

            _waitingNext = false;
        }
        private void EndDialogueEarly()
        {
            _isTyping = false;
            _waitingNext = false;
            EndDialogue();
        }

        private void EndDialogue()
        {
            _dialogueRunning = false;
            if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(false);
            if (nextWordIcon != null) nextWordIcon.SetActive(false);
            if (_nextWordAnimator != null) _nextWordAnimator.enabled = false;
        }

        // --- 立绘弹跳 ---

        private void AnimateSpeaker(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return;

            RectTransform target = speaker == PlayerName
                ? GameObject.Find("Canvas/PlayerArea/PlayerP")?.GetComponent<RectTransform>()
                : npcPortrait?.rectTransform;

            if (target == null) return;

            target.DOKill();
            target.localScale = Vector3.one;
            target.DOScale(1.08f, 0.12f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
        }

        // --- 表情切换 ---

        private void SetExpression(string speaker, string expression)
        {
            if (string.IsNullOrEmpty(speaker) || string.IsNullOrEmpty(expression)) return;
            var cache = KiKs.UI.PortraitExpressionCache.Instance;
            if (cache == null) return;

            Image portrait = speaker == PlayerName
                ? GameObject.Find("Canvas/PlayerArea/PlayerP")?.GetComponent<Image>()
                : npcPortrait;

            if (portrait == null) return;

            var sprite = cache.GetSprite(speaker, expression);
            if (sprite != null)
                portrait.sprite = sprite;
        }

        private void SetNpcDefaultPortrait(string npcId)
        {
            if (npcPortrait == null) return;

            // 先尝试从 PortraitExpressionCache 取
            var cache = KiKs.UI.PortraitExpressionCache.Instance;
            if (cache != null)
            {
                var sprite = cache.GetSprite(npcId, null);
                if (sprite != null)
                {
                    npcPortrait.sprite = sprite;
                    return;
                }
            }

            // fallback: 从 Resources 按路径加载
            var path = $"Art/NPC/Event/{npcId}";
            var loaded = Resources.Load<Sprite>(path);
            // Multiple sprite mode 下 Load<Sprite> 返回 null，需要 LoadAll
            if (loaded == null)
            {
                var all = Resources.LoadAll<Sprite>(path);
                if (all != null && all.Length > 0)
                    loaded = all[0];
            }
            if (loaded != null)
            {
                npcPortrait.sprite = loaded;
                Debug.Log($"[EventScene] Loaded NPC portrait from Resources/{path}");
            }
            else
            {
                Debug.LogWarning($"[EventScene] NPC portrait not found at Resources/{path} or PortraitExpressionCache.");
            }
        }

        // ==================== 阶段 4: 发卡 ====================

        private void DealCards(IReadOnlyList<EventCardDefinition> cards)
        {
            if (cardDealer == null || cards == null || cards.Count == 0) return;

            cardDealer.OnCardPlayed = HandleEventCardPlayed;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var spec = CreateEventCardSpec(card, i);
                var cardView = cardDealer.DrawCard(spec, $"event_card_{i}");
                if (cardView != null)
                {
                    cardsByView[cardView] = card;
                    HideCombatOnlyCardText(cardView);
                }
            }
        }

        private static CardSpec CreateEventCardSpec(EventCardDefinition card, int index)
        {
            var visualOnlyEffect = new CardEffectSpec(
                CardEffectType.BlockDamage,
                UpgradeableNumber.Zero,
                UpgradeableNumber.One,
                ValueUnit.Points,
                0d);

            return new CardSpec(
                id: $"event_card_{index}",
                displayNameZhCn: string.Empty,
                displayNameEn: string.Empty,
                category: "event",
                costResource: CardResourceType.ActionPoint,
                costAmount: 0,
                isSpecial: false,
                targetType: CardTargetType.SingleEnemy,
                effects: new[] { visualOnlyEffect },
                imagePath: card.imagePath ?? string.Empty);
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

        // ==================== 阶段 5: 选牌分支 ====================

        private bool HandleEventCardPlayed(CardView card)
        {
            if (card == null || isLeaving || !cardsByView.TryGetValue(card, out var cardDef))
                return false;

            cardsByView.Remove(card);
            StartCoroutine(ResolveCard(cardDef));
            return true;
        }

        private IEnumerator ResolveCard(EventCardDefinition cardDef)
        {
            switch (cardDef.type)
            {
                case "effect":
                    yield return ResolveEffectCard(cardDef);
                    break;

                case "attack":
                    yield return ResolveAttackCard(cardDef);
                    break;

                case "pilfer":
                    yield return ResolvePilferCard(cardDef);
                    break;

                case "end":
                    // end 可以带代价和奖励
                    ApplyCosts(cardDef);
                    yield return ApplyRewards(cardDef);
                    yield return PlayDialogue(cardDef.dialogueId);
                    break;
            }

            FinishEvent();
        }

        /// <summary>处理代价（扣血/扣金币），end 类型共用</summary>
        private void ApplyCosts(EventCardDefinition cardDef)
        {
            if (cardDef.hpCost > 0)
            {
                var newHp = Mathf.Max(0, PlayerGlobalStats.CurrentHealth - cardDef.hpCost);
                PlayerGlobalStats.SetHealth(newHp, PlayerGlobalStats.MaxHealth);
                RefreshHpDisplay();
                Debug.Log($"[EventScene] HP -{cardDef.hpCost} → {PlayerGlobalStats.CurrentHealth}");
            }

            if (cardDef.goldCost > 0)
            {
                if (!RuntimeGameRepository.SpendGold(cardDef.goldCost))
                {
                    KiKs.UI.WarningToast.Show($"金币不足：需要 {cardDef.goldCost}C");
                    return;
                }
                RefreshGoldDisplay();
                Debug.Log($"[EventScene] Gold -{cardDef.goldCost} → {RuntimeGameRepository.Gold}");
            }
        }

        /// <summary>处理奖励（金币/材料/卡牌/healFull），end 类型共用</summary>
        private IEnumerator ApplyRewards(EventCardDefinition cardDef)
        {
            // 随机金币奖励
            if (cardDef.goldRewardMax > 0)
            {
                var gold = Random.Range(cardDef.goldRewardMin, cardDef.goldRewardMax + 1);
                RuntimeGameRepository.AddGold(gold);
                RefreshGoldDisplay();
                RevealRewardToken($"金币 ×{gold}", new Color32(210, 170, 60, 255));
                Debug.Log($"[EventScene] Gold +{gold} → {RuntimeGameRepository.Gold}");
            }

            // 材料奖励
            if (!string.IsNullOrWhiteSpace(cardDef.materialRewardId) && cardDef.materialRewardAmount > 0)
            {
                var matId = cardDef.materialRewardId;
                if (matId == "random_raw")
                    matId = PickRandomRawMaterial();

                if (!string.IsNullOrEmpty(matId))
                {
                    RuntimeGameRepository.AddResource(matId, cardDef.materialRewardAmount);
                    var matName = GetMaterialDisplayName(matId);
                    RevealRewardToken($"{matName} ×{cardDef.materialRewardAmount}", new Color32(116, 82, 54, 255));
                    Debug.Log($"[EventScene] Material +{cardDef.materialRewardAmount} {matId}");
                }
            }

            // 指定卡牌奖励
            if (cardDef.cardRewardIds != null && cardDef.cardRewardIds.Length > 0)
            {
                foreach (var cardId in cardDef.cardRewardIds)
                {
                    if (string.IsNullOrEmpty(cardId)) continue;
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Card reward: {cardId}");
                }
            }
            else if (!string.IsNullOrWhiteSpace(cardDef.cardRewardMode))
            {
                var cardId = PickRandomCard(cardDef.cardRewardMode);
                if (!string.IsNullOrEmpty(cardId))
                {
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Card reward: {cardId}");
                }
            }

            // HP 回满
            if (cardDef.healFull)
            {
                PlayerGlobalStats.SetHealth(PlayerGlobalStats.MaxHealth, PlayerGlobalStats.MaxHealth);
                RefreshHpDisplay();
                RevealRewardToken("生命值回满", new Color32(80, 200, 80, 255));
                Debug.Log("[EventScene] HP healed to full.");
            }

            yield return null;
        }

        /// <summary>pilfer：不扣不杀 → 给指定卡牌 → 播对话</summary>
        private IEnumerator ResolvePilferCard(EventCardDefinition cardDef)
        {
            // 给指定卡牌
            if (cardDef.cardRewardIds != null && cardDef.cardRewardIds.Length > 0)
            {
                foreach (var cardId in cardDef.cardRewardIds)
                {
                    if (string.IsNullOrEmpty(cardId)) continue;
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Pilfer: {cardId}");
                }
            }

            yield return PlayDialogue(cardDef.dialogueId);
        }

        /// <summary>选项1/2：扣血或扣金币 → 给随机奖励 → 播放对话</summary>
        private IEnumerator ResolveEffectCard(EventCardDefinition cardDef)
        {
            // 扣血
            if (cardDef.hpCost > 0)
            {
                var newHp = Mathf.Max(0, PlayerGlobalStats.CurrentHealth - cardDef.hpCost);
                PlayerGlobalStats.SetHealth(newHp, PlayerGlobalStats.MaxHealth);
                RefreshHpDisplay();
                Debug.Log($"[EventScene] HP -{cardDef.hpCost} → {PlayerGlobalStats.CurrentHealth}");
            }

            // 扣金币（不够则中止）
            if (cardDef.goldCost > 0)
            {
                if (!RuntimeGameRepository.SpendGold(cardDef.goldCost))
                {
                    KiKs.UI.WarningToast.Show($"金币不足：需要 {cardDef.goldCost}C");
                    FinishEvent();
                    yield break;
                }
                RefreshGoldDisplay();
                Debug.Log($"[EventScene] Gold -{cardDef.goldCost} → {RuntimeGameRepository.Gold}");
            }

            // 随机金币奖励
            if (cardDef.goldRewardMax > 0)
            {
                var gold = Random.Range(cardDef.goldRewardMin, cardDef.goldRewardMax + 1);
                RuntimeGameRepository.AddGold(gold);
                RefreshGoldDisplay();
                RevealRewardToken($"金币 ×{gold}", new Color32(210, 170, 60, 255));
                Debug.Log($"[EventScene] Gold +{gold} → {RuntimeGameRepository.Gold}");
            }

            // 材料奖励
            if (!string.IsNullOrWhiteSpace(cardDef.materialRewardId) && cardDef.materialRewardAmount > 0)
            {
                var matId = cardDef.materialRewardId;
                if (matId == "random_raw")
                    matId = PickRandomRawMaterial();

                if (!string.IsNullOrEmpty(matId))
                {
                    RuntimeGameRepository.AddResource(matId, cardDef.materialRewardAmount);
                    var matName = GetMaterialDisplayName(matId);
                    RevealRewardToken($"{matName} ×{cardDef.materialRewardAmount}", new Color32(116, 82, 54, 255));
                    Debug.Log($"[EventScene] Material +{cardDef.materialRewardAmount} {matId}");
                }
            }

            // 卡牌奖励
            if (!string.IsNullOrWhiteSpace(cardDef.cardRewardMode))
            {
                var cardId = PickRandomCard(cardDef.cardRewardMode);
                if (!string.IsNullOrEmpty(cardId))
                {
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Card reward: {cardId}");
                }
            }

            // 指定卡牌奖励
            if (cardDef.cardRewardIds != null && cardDef.cardRewardIds.Length > 0)
            {
                foreach (var cardId in cardDef.cardRewardIds)
                {
                    if (string.IsNullOrEmpty(cardId)) continue;
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Card reward: {cardId}");
                }
            }

            // HP 回满
            if (cardDef.healFull)
            {
                PlayerGlobalStats.SetHealth(PlayerGlobalStats.MaxHealth, PlayerGlobalStats.MaxHealth);
                RefreshHpDisplay();
                RevealRewardToken("生命值回满", new Color32(80, 200, 80, 255));
                Debug.Log("[EventScene] HP healed to full.");
            }

            // 播放分支对话
            yield return PlayDialogue(cardDef.dialogueId);
        }

        /// <summary>选项3：播放刀光特效 → NPC消失 → 给掉落卡牌</summary>
        private IEnumerator ResolveAttackCard(EventCardDefinition cardDef)
        {
            // 播放简单刀光特效 + 立绘冲刺
            yield return PlayAttackEffect();

            // NPC 消失
            if (npcPortrait != null)
            {
                npcPortrait.DOFade(0f, 0.4f).OnComplete(() => npcPortrait.gameObject.SetActive(false));
            }

            // 标记 NPC 死亡
            var evt = EventSelectionState.CurrentEvent;
            if (evt != null)
            {
                EventSelectionState.MarkNpcDead(evt.npcId);
                Debug.Log($"[EventScene] NPC '{evt.npcId}' killed.");
            }

            // 给卡牌掉落（指定或随机）
            if (cardDef.cardRewardIds != null && cardDef.cardRewardIds.Length > 0)
            {
                foreach (var cardId in cardDef.cardRewardIds)
                {
                    if (string.IsNullOrEmpty(cardId)) continue;
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Attack drop: {cardId}");
                }
            }
            else if (!string.IsNullOrWhiteSpace(cardDef.cardRewardMode))
            {
                var cardId = PickRandomCard(cardDef.cardRewardMode);
                if (!string.IsNullOrEmpty(cardId))
                {
                    RuntimeGameRepository.AddOwnedCard(cardId);
                    RevealRewardToken($"卡牌: {cardId}", new Color32(87, 102, 142, 255));
                    Debug.Log($"[EventScene] Attack drop: {cardId}");
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>简单攻击特效：玩家立绘前冲 → 刀光 → 弹回</summary>
        private IEnumerator PlayAttackEffect()
        {
            var playerP = GameObject.Find("Canvas/PlayerArea/PlayerP")?.GetComponent<RectTransform>();
            if (playerP == null) yield break;

            var originPos = playerP.anchoredPosition;
            var targetPos = npcPortrait != null
                ? playerP.anchoredPosition + new Vector2(
                    (npcPortrait.rectTransform.position.x - playerP.position.x) * 0.7f, 0)
                : originPos + new Vector2(300f, 0);

            // 前冲
            playerP.DOAnchorPos(targetPos, 0.12f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.12f);

            // 刀光特效
            SpawnSimpleSlash(targetPos);

            // 弹回
            yield return new WaitForSeconds(0.15f);
            playerP.DOAnchorPos(originPos, 0.25f).SetEase(Ease.OutQuart);
            yield return new WaitForSeconds(0.25f);
        }

        private void SpawnSimpleSlash(Vector2 canvasPos)
        {
            var slashObj = new GameObject("EventSlashVFX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slashObj.layer = 5;
            slashObj.transform.SetParent(canvasRect, false);
            var rt = slashObj.GetComponent<RectTransform>();
            rt.anchoredPosition = canvasPos;
            rt.sizeDelta = new Vector2(200, 200);

            var img = slashObj.GetComponent<Image>();
            img.color = new Color(1f, 0.9f, 0.9f, 1f);
            img.raycastTarget = false;

            var seq = DOTween.Sequence();
            rt.localRotation = Quaternion.Euler(0, 0, 135f);
            seq.Join(rt.DOScale(1.5f, 0.15f).SetEase(Ease.OutQuart));
            seq.Join(rt.DORotate(new Vector3(0, 0, -45f), 0.15f).SetEase(Ease.OutQuart));
            seq.Join(img.DOFade(0f, 0.2f).SetDelay(0.1f));
            seq.OnComplete(() => Destroy(slashObj));
        }

        // ==================== 随机奖励工具方法 ====================

        private static readonly string[] RawMaterialIds =
            { "claw", "wolffur", "eye", "fire", "oil", "snake", "tentacle" };

        private static string PickRandomRawMaterial()
        {
            return RawMaterialIds[Random.Range(0, RawMaterialIds.Length)];
        }

        private static string GetMaterialDisplayName(string matId)
        {
            return matId switch
            {
                "claw" => "爪子",
                "wolffur" => "狼毫",
                "eye" => "眼珠",
                "fire" => "紫色火焰",
                "oil" => "肥油",
                "snake" => "蛇干",
                "tentacle" => "触手",
                "CoffeeBean" => "咖啡豆",
                "Milk" => "牛奶",
                "Sugar" => "糖",
                "Water" => "水",
                _ => matId
            };
        }

        private static string PickRandomCard(string mode)
        {
            var allCards = StaticGameRepository.PlayerCards;
            if (allCards == null || allCards.Count == 0) return null;

            switch (mode)
            {
                case "random_normal":
                {
                    var pool = new System.Collections.Generic.List<CardSpec>();
                    foreach (var c in allCards)
                        if (!c.IsSpecial && !c.IsEnemyCard && !string.IsNullOrEmpty(c.ImagePath))
                            pool.Add(c);
                    if (pool.Count == 0) return null;
                    return pool[Random.Range(0, pool.Count)].Id;
                }
                case "random_special":
                {
                    var pool = new System.Collections.Generic.List<CardSpec>();
                    foreach (var c in allCards)
                        if (c.IsSpecial && !c.IsEnemyCard && !string.IsNullOrEmpty(c.ImagePath))
                            pool.Add(c);
                    if (pool.Count == 0)
                    {
                        // fallback to normal
                        foreach (var c in allCards)
                            if (!c.IsSpecial && !c.IsEnemyCard && !string.IsNullOrEmpty(c.ImagePath))
                                pool.Add(c);
                    }
                    if (pool.Count == 0) return null;
                    return pool[Random.Range(0, pool.Count)].Id;
                }
                case "specific":
                    return null; // cardRewardSpecificId handled by caller if needed
                default:
                    return null;
            }
        }

        // ==================== 奖励展示（复用宝藏区逻辑） ====================

        private void RevealRewardToken(string displayText, Color32 color)
        {
            if (canvasRect == null || rewardTray == null) return;

            var token = new GameObject(
                $"EventReward_{revealedRewardCount + 1}",
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
            tokenRect.anchoredPosition = npcPortrait != null
                ? GetCanvasPosition(npcPortrait.rectTransform)
                : new Vector2(300f, 165f);

            var tokenImage = token.GetComponent<Image>();
            tokenImage.color = color;
            tokenImage.raycastTarget = false;
            CreateText("RewardName", tokenRect, displayText, 22f, Color.white);

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

        // ==================== 阶段 6: 结束 ====================

        private void FinishEvent()
        {
            var evt = EventSelectionState.CurrentEvent;
            if (evt != null)
            {
                EventSelectionState.MarkEventCompleted(evt.id);
                EventSelectionState.ClearCurrent();
            }

            var completion = RuntimeGameRepository.CompleteSelectedArea(defeated: false);
            var nextScene = string.IsNullOrWhiteSpace(completion.NextSceneName)
                ? ReturnSceneName
                : completion.NextSceneName;

            LeaveEvent(nextScene);
        }

        private void LeaveEvent()
        {
            LeaveEvent(ReturnSceneName);
        }

        private void LeaveEvent(string nextSceneName)
        {
            if (isLeaving)
                return;

            var sceneName = string.IsNullOrWhiteSpace(nextSceneName) ? ReturnSceneName : nextSceneName;

            isLeaving = true;
            if (leaveButton != null)
                leaveButton.interactable = false;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[EventScene] Scene '{sceneName}' is not in build settings.", this);
                isLeaving = false;
                if (leaveButton != null) leaveButton.interactable = true;
                return;
            }

            if (KiKs.UI.TransitionEffect.Instance != null)
                KiKs.UI.TransitionEffect.Instance.TransitionTo(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        // ==================== 工具方法（从宝藏区复制） ====================

        private static RectTransform CreateRuntimeArea(
            string name, RectTransform parent, Vector2 position, Vector2 size)
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
            string name, RectTransform parent, string content, float fontSize, Color color)
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
            if (text == null) return;
            if (chineseFont == null)
                chineseFont = Resources.Load<TMP_FontAsset>(ChineseFontResourcePath);
            if (chineseFont != null)
                text.font = chineseFont;
            else
                Debug.LogWarning($"[EventScene] Chinese TMP font not found at Resources/{ChineseFontResourcePath}.");
        }

        private Vector2 GetCanvasPosition(RectTransform target)
        {
            if (target == null || canvasRect == null)
                return Vector2.zero;
            return canvasRect.InverseTransformPoint(target.position);
        }
    }

    /// <summary>
    /// Auto-installs EventSceneController when the "Event" scene loads.
    /// Same bootstrap pattern as TreasureSceneBootstrap.
    /// </summary>
    internal static class EventSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != EventSceneController.SceneName ||
                UnityEngine.Object.FindFirstObjectByType<EventSceneController>() != null)
                return;

            new GameObject(nameof(EventSceneController)).AddComponent<EventSceneController>();
        }
    }
}