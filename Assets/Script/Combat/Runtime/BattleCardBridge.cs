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

        private void Start()
        {
            if (animator == null) animator = GetComponent<CardDealAnimator>();
            if (battleController == null) battleController = FindFirstObjectByType<BattleController>();

            ConfigureMagicHandUpgradeBridge();

            if (animator != null)
                animator.OnCardPlayed += OnCardPlayed;

            StartCoroutine(WaitAndDrawInitialHand());
        }

        private void OnDestroy()
        {
            if (animator != null)
                animator.OnCardPlayed -= OnCardPlayed;
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

            var hand = battleController.State.Deck.Hand;
            Debug.Log($"[BattleCardBridge] Drawing initial hand: {hand.Count} cards");

            foreach (var cardInstance in hand)
            {
                animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded);
            }
        }

        private bool OnCardPlayed(CardView cardView)
        {
            if (cardView == null) return false;

            if (!_engineReady || battleController == null || !battleController.IsInitialized)
                return false;

            var targetId = string.IsNullOrEmpty(defaultTargetId)
                ? battleController.State?.FindFirstLivingEnemy()?.Id
                : defaultTargetId;

            var result = battleController.PlayCard(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning($"[BattleCardBridge] PlayCard failed: {result.Message}");
                return false;
            }

            return true;
        }

        /// <summary>结束回合：回收手牌 + 引擎结束回合 + 抽新牌</summary>
        public void EndTurn()
        {
            if (!_engineReady || battleController == null || !battleController.IsInitialized) return;

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
                animator.DrawCard(cardInstance.Spec, cardInstance.InstanceId, cardInstance.IsUpgraded);
        }

        private void ConfigureMagicHandUpgradeBridge()
        {
            var magicHand = GameObject.Find("PlayerPanel");
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
