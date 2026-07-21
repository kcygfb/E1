using UnityEngine;
using UnityEngine.EventSystems;

namespace KiKs.Combat
{
    /// <summary>Uses the draggable magic hand as an in-battle card-upgrade tool.</summary>
    [DisallowMultipleComponent]
    public sealed class MagicHandUpgradeBridge : MonoBehaviour, IEndDragHandler
    {
        private BattleController _battleController;
        private CardDealAnimator _animator;

        public void Configure(BattleController battleController, CardDealAnimator animator)
        {
            _battleController = battleController;
            _animator = animator;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_battleController == null || !_battleController.IsInitialized || _animator == null)
                return;

            var cardView = FindCardAt(eventData.position, eventData.pressEventCamera);
            if (cardView == null || cardView.Spec == null ||
                !cardView.Spec.CanUpgrade || cardView.IsUpgraded)
                return;

            var targetId = _battleController.State.FindFirstLivingEnemy()?.Id;
            var result = _battleController.UpgradeCard(cardView.InstanceId, targetId);
            if (!result.Success)
            {
                Debug.LogWarning("[MagicHandUpgradeBridge] Card upgrade failed: " + result.Message, this);
                return;
            }

            cardView.SetUpgraded(true);
            Debug.Log("[MagicHandUpgradeBridge] Upgraded " + cardView.Spec.DisplayName + ".", this);
        }

        private CardView FindCardAt(Vector2 screenPosition, Camera eventCamera)
        {
            var handCards = _animator.HandCards;
            for (var i = handCards.Count - 1; i >= 0; i--)
            {
                var cardView = handCards[i];
                if (cardView == null) continue;
                var rect = cardView.transform as RectTransform;
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(
                        rect, screenPosition, eventCamera))
                    return cardView;
            }

            return null;
        }
    }
}
