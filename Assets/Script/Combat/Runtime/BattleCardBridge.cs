using System.Collections;
using UnityEngine;
using KiKs.UI;

namespace KiKs.Combat
{
    [RequireComponent(typeof(CardDealAnimator))]
    public class BattleCardBridge : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private CardDealAnimator animator;
        [SerializeField] private string defaultTargetId = "";

        private bool _initialHandDrawn;
        private bool _engineReady;
        private bool _rebuildingHand;
        private PlayerAttackFeedback _playerAttackFeedback;

        private void Start()
        {
            if (animator == null) animator = GetComponent<CardDealAnimator>();
            if (animator != null && !animator.HasPlayArea)
                Debug.LogError("[BattleCardBridge] CardDealAnimator PlayArea is not assigned.", this);

            if (battleController == null) battleController = FindFirstObjectByType<BattleController>();
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;

            ConfigureMagicHandCardBridge();

            if (animator != null)
            {
                animator.OnCardPlayed += OnCardPlayed;
                animator.OnCardShot += OnCardShot;
            }

            StartCoroutine(WaitAndDrawInitialHand());
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
            if (animator != null)
            {
                animator.OnCardPlayed -= OnCardPlayed;
                animator.OnCardShot -= OnCardShot;
            }
        }

        /// <summary>
        /// 战斗中玩家抽牌（如烟雾弹的 draw_cards 效果）时，动态把新牌加入手牌区。
        /// 初始手牌与回合开始的重建仍由 DrawInitialHand / DrawNewHandNextTurn 全量刷新，
        /// 这里只负责中途增量抽牌，并通过 _initialHandDrawn 避免与开局抽牌重复。
        /// </summary>
        private void OnCombatEvent(CombatEvent evt)
        {
            if (evt == null || evt.Type != CombatEventType.CardDrawn) return;
            if (!_initialHandDrawn || _rebuildingHand) return; // 开局与回合重建由全量刷新统一处理
            if (battleController == null || !battleController.IsInitialized) return;
            if (battleController.State?.Player == null) return;
            if (evt.SourceId != battleController.State.Player.Id) return; // 只看玩家抽牌

            // 规则层抽牌发生在手牌区动画之后，直接用引擎手牌状态增量绘制
            var hand = battleController.State.Deck.Hand;
            CardInstance drawn = null;
            foreach (var card in hand)
            {
                if (card.InstanceId == evt.CardInstanceId)
                {
                    drawn = card;
                    break;
                }
            }
            if (drawn == null) return;

            DrawCardIfAbsent(drawn);
        }

        /// <summary>
        /// 按 InstanceId 去重后绘制一张手牌。开局的 DrawInitialHand、回合开始的全量重建
        /// 以及战斗中 CardDrawn 增量绘制都走这里，保证同一张牌不会在 UI 上重复出现。
        /// </summary>
        private void DrawCardIfAbsent(CardInstance card)
        {
            if (card == null || animator == null) return;
            foreach (var existing in animator.HandCards)
            {
                if (existing != null && existing.InstanceId == card.InstanceId)
                    return;
            }

            var cardView = animator.DrawCard(card.Spec, card.InstanceId, card.IsUpgraded, card.IsActivated);
            if (cardView != null)
                HookCardHover(cardView);
        }

        /// <summary>检查是否有任意 Draggable 正在拖拽（魔手拖拽时保持魔法姿态）</summary>
        private static bool IsAnyDraggableActive()
        {
            var prop = typeof(KiKs.UI.Draggable).GetProperty(
                "AnyDragging",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return prop != null && (bool)prop.GetValue(null);
        }

        private IEnumerator WaitAndDrawInitialHand()
        {
            while (battleController == null || !battleController.IsInitialized)
                yield return null;

            _engineReady = true;
            Debug.Log("[BattleCardBridge] Engine ready, drawing initial hand");

            DrawInitialHand();
        }

        private void DrawInitialHand()
        {
            if (_initialHandDrawn || battleController == null || animator == null) return;
            _initialHandDrawn = true;

            _playerAttackFeedback = FindFirstObjectByType<PlayerAttackFeedback>();

            var hand = battleController.State.Deck.Hand;
            Debug.Log($"[BattleCardBridge] Drawing initial hand: {hand.Count} cards");

            foreach (var cardInstance in hand)
                DrawCardIfAbsent(cardInstance);

            // 卡牌生成后播放 BGM（避免 playOnAwake 阻塞卡牌生成）
            var bgmObj = GameObject.Find("BattleBGM");
            var bgmSource = bgmObj?.GetComponent<AudioSource>();
            if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            {
                if (!bgmSource.enabled) bgmSource.enabled = true;
                bgmSource.Play();
            }
        }

        private void HookCardHover(CardView cardView)
        {
            if (_playerAttackFeedback == null) return;
            cardView.OnHoverEnter += OnCardHoverEnter;
        }

        private void OnCardHoverEnter(CardView cardView)
        {
            if (_playerAttackFeedback == null || cardView?.Spec == null) return;

            // 魔手拖拽中：保持魔法预备姿态，不切换
            if (IsAnyDraggableActive()) return;

            var category = cardView.Spec.Category;
            if (category == "ranged" || category == "guns")
                _playerAttackFeedback.SwitchToRangedPose();
            else if (category == "magic")
                _playerAttackFeedback.SwitchToMagicPose();
            else
                _playerAttackFeedback.SwitchToMeleePose();
        }

        private bool OnCardPlayed(CardView cardView)
        {
            if (cardView == null) return false;

            if (!_engineReady || battleController == null || !battleController.IsInitialized)
            {
                WarningToast.Show("Battle is not ready.");
                return false;
            }

            var targetId = string.IsNullOrEmpty(defaultTargetId)
                ? battleController.State?.FindFirstLivingEnemy()?.Id
                : defaultTargetId;

            CombatResult result;
            if (battleController.IsShooting(cardView.InstanceId))
            {
                result = battleController.PlayRemainingShots(cardView.InstanceId, targetId);
                if (!result.Success)
                {
                    Debug.LogWarning($"[BattleCardBridge] PlayRemainingShots failed: {result.Message}");
                    WarningToast.Show(CombatWarningText.FromResult(result));
                    return false;
                }

                if (_playerAttackFeedback != null)
                    _playerAttackFeedback.PlayRangedSingleShot(cardView.IsUpgraded);
                return true;
            }

            result = battleController.PlayCard(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning($"[BattleCardBridge] PlayCard failed: {result.Message}");
                WarningToast.Show(CombatWarningText.FromResult(result));
                return false;
            }

            return true;
        }

        private bool OnCardShot(CardView cardView)
        {
            if (cardView == null) return false;
            if (!_engineReady || battleController == null || !battleController.IsInitialized)
            {
                WarningToast.Show("Battle is not ready.");
                return false;
            }

            var targetId = string.IsNullOrEmpty(defaultTargetId)
                ? battleController.State?.FindFirstLivingEnemy()?.Id
                : defaultTargetId;

            var result = battleController.PlaySingleShot(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning($"[BattleCardBridge] PlaySingleShot failed: {result.Message}");
                WarningToast.Show(CombatWarningText.FromResult(result));
                return false;
            }

            if (_playerAttackFeedback != null)
                _playerAttackFeedback.PlayRangedSingleShot(cardView.IsUpgraded);
            return true;
        }

        public void EndTurn()
        {
            if (!_engineReady || battleController == null || !battleController.IsInitialized)
            {
                WarningToast.Show("Battle is not ready.");
                return;
            }

            var state = battleController.State;
            if (state == null)
            {
                WarningToast.Show("Battle state is not ready.");
                return;
            }

            var handCards = animator.HandCards;
            var hasShootingCard = false;
            foreach (var card in handCards)
            {
                if (card != null && battleController.IsShooting(card.InstanceId))
                {
                    hasShootingCard = true;
                    break;
                }
            }

            if (state.Phase != CombatPhase.PlayerInput && !hasShootingCard)
            {
                WarningToast.Show("You cannot end the turn now.");
                return;
            }

            foreach (var card in handCards)
            {
                if (card == null || !battleController.IsShooting(card.InstanceId))
                    continue;

                var cancelResult = battleController.CancelShooting(card.InstanceId);
                if (!cancelResult.Success)
                {
                    Debug.LogWarning("[BattleCardBridge] CancelShooting failed: " + cancelResult.Message);
                    WarningToast.Show(CombatWarningText.FromResult(cancelResult));
                    return;
                }
            }

            var result = battleController.EndPlayerTurn();
            if (!result.Success)
            {
                Debug.LogWarning("[BattleCardBridge] EndPlayerTurn failed: " + result.Message);
                WarningToast.Show(CombatWarningText.FromResult(result));
                return;
            }

            animator.DiscardAllCards();
            Debug.Log("[BattleCardBridge] EndPlayerTurn: success");
            _rebuildingHand = true;
            StartCoroutine(DrawNewHandNextTurn());
        }

        private IEnumerator DrawNewHandNextTurn()
        {
            // 等引擎回到 PlayerInput 阶段
            while (battleController == null || !battleController.IsInitialized
                || battleController.State.Phase != CombatPhase.PlayerInput)
                yield return null;

            var hand = battleController.State.Deck.Hand;
            Debug.Log($"[BattleCardBridge] New turn, drawing {hand.Count} cards");

            foreach (var cardInstance in hand)
                DrawCardIfAbsent(cardInstance);

            _rebuildingHand = false;
        }

        private void ConfigureMagicHandCardBridge()
        {
            var magicHand = GameObject.Find("Magichand");
            if (magicHand == null)
                magicHand = GameObject.Find("PlayerPanel");
            if (magicHand == null)
            {
                Debug.LogWarning("[BattleCardBridge] PlayerPanel magic hand was not found.", this);
                return;
            }

            var cardBridge = magicHand.GetComponent<MagicHandCardBridge>();
            if (cardBridge == null)
                cardBridge = magicHand.AddComponent<MagicHandCardBridge>();
            cardBridge.Configure(battleController, animator);
        }
    }
}
