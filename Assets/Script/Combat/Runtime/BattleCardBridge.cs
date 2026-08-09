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
        private PlayerAttackFeedback _playerAttackFeedback;

        private void Start()
        {
            if (animator == null) animator = GetComponent<CardDealAnimator>();
            if (animator != null && !animator.HasPlayArea)
                Debug.LogError("[BattleCardBridge] CardDealAnimator PlayArea is not assigned.", this);

            if (battleController == null) battleController = FindFirstObjectByType<BattleController>();

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
            if (animator != null)
            {
                animator.OnCardPlayed -= OnCardPlayed;
                animator.OnCardShot -= OnCardShot;
            }
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
            {
                var cardView = animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded, cardInstance.IsActivated);
                if (cardView != null)
                    HookCardHover(cardView);
            }

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
                var cardView = animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded, cardInstance.IsActivated);
                if (cardView != null)
                    HookCardHover(cardView);
            }
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
