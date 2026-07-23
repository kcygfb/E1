using System.Collections;
using UnityEngine;

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
        private PlayerAttackFeedback _playerAttackFeedback;

        private void Start()
        {
            if (animator == null) animator = GetComponent<CardDealAnimator>();
            if (battleController == null) battleController = FindFirstObjectByType<BattleController>();

            ConfigureMagicHandUpgradeBridge();

            if (animator != null)
            {
                animator.OnCardPlayed += OnCardPlayed;
                animator.OnCardShot += OnCardShot;
            }

            StartCoroutine(WaitAndDrawInitialHand());
        }

        private void OnDestroy()
        {
            if (animator != null)
            {
                animator.OnCardPlayed -= OnCardPlayed;
                animator.OnCardShot -= OnCardShot;
            }
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
            {
                var cardView = animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded);
                if (cardView != null)
                    HookCardHover(cardView);
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
                return false;

            var targetId = string.IsNullOrEmpty(defaultTargetId)
                ? battleController.State?.FindFirstLivingEnemy()?.Id
                : defaultTargetId;

            // 如果正在多段射击中拖拽，一次性打完剩余子弹
            CombatResult result;
            if (battleController.IsShooting(cardView.InstanceId))
            {
                result = battleController.PlayRemainingShots(cardView.InstanceId, targetId);
                if (!result.Success)
                {
                    Debug.LogWarning($"[BattleCardBridge] PlayRemainingShots failed: {result.Message}");
                    return false;
                }

                // 播一次特效
                if (_playerAttackFeedback != null)
                    _playerAttackFeedback.PlayRangedSingleShot();
                return true;
            }

            result = battleController.PlayCard(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning($"[BattleCardBridge] PlayCard failed: {result.Message}");
                return false;
            }

            return true;
        }

        private void OnCardShot(CardView cardView)
        {
            if (cardView == null) return;
            if (!_engineReady || battleController == null || !battleController.IsInitialized) return;

            var targetId = string.IsNullOrEmpty(defaultTargetId)
                ? battleController.State?.FindFirstLivingEnemy()?.Id
                : defaultTargetId;

            var result = battleController.PlaySingleShot(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning($"[BattleCardBridge] PlaySingleShot failed: {result.Message}");
                return;
            }

            // 播放单发射击特效
            if (_playerAttackFeedback != null)
                _playerAttackFeedback.PlayRangedSingleShot();
        }

        /// <summary>结束回合：回收手牌 + 引擎结束回合 + 抽新牌</summary>
        public void EndTurn()
        {
            if (!_engineReady || battleController == null || !battleController.IsInitialized) return;

            // 0. 如果正在多段射击，先取消（强制弃牌剩余子弹）
            var handCards = animator.HandCards;
            foreach (var card in handCards)
            {
                if (card != null && battleController.IsShooting(card.InstanceId))
                {
                    var cancelResult = battleController.CancelShooting(card.InstanceId);
                    Debug.Log("[BattleCardBridge] CancelShooting: " + (cancelResult.Success ? "success" : cancelResult.Message));
                }
            }

            // 1. 回收所有手牌到弃牌堆
            animator.DiscardAllCards();

            // 2. 引擎结束玩家回合
            var result = battleController.EndPlayerTurn();
            Debug.Log("[BattleCardBridge] EndPlayerTurn: " + (result.Success ? "success" : result.Message));

            // 3. 等新回合开始后抽新手牌
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
            {
                var cardView = animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded);
                if (cardView != null)
                    HookCardHover(cardView);
            }
        }

        private void ConfigureMagicHandUpgradeBridge()
        {
            var magicHand = GameObject.Find("Magichand");
            if (magicHand == null)
                magicHand = GameObject.Find("PlayerPanel");
            if (magicHand == null)
            {
                Debug.LogWarning("[BattleCardBridge] PlayerPanel magic hand was not found.", this);
                return;
            }

            var upgradeBridge = magicHand.GetComponent<MagicHandUpgradeBridge>();
            if (upgradeBridge == null)
                upgradeBridge = magicHand.AddComponent<MagicHandUpgradeBridge>();
            upgradeBridge.Configure(battleController, animator);
        }
    }
}
