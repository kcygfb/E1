using UnityEngine;
using UnityEngine.EventSystems;
using KiKs.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// Uses the draggable magic hand to upgrade action cards or activate magic cards in battle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MagicHandCardBridge : MonoBehaviour,
        IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleController _battleController;
        private CardDealAnimator _animator;
        private PlayerAttackFeedback _playerAttackFeedback;

        public void Configure(BattleController battleController, CardDealAnimator animator)
        {
            _battleController = battleController;
            _animator = animator;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_playerAttackFeedback == null)
                _playerAttackFeedback = UnityEngine.Object.FindFirstObjectByType<PlayerAttackFeedback>();
            if (_playerAttackFeedback != null)
                _playerAttackFeedback.SwitchToMagicPose();
            else
                Debug.LogWarning("[MagicHandCardBridge] PlayerAttackFeedback not found on hover enter");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 离开魔手时不自动恢复，等悬浮到其他牌时自然切换
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_battleController == null || !_battleController.IsInitialized || _animator == null)
            {
                WarningToast.Show("Battle is not ready.");
                return;
            }

            var cardView = FindCardAt(eventData.position, eventData.pressEventCamera);

            if (cardView == null || cardView.Spec == null)
            {
                ReevaluatePose(eventData.position, eventData.pressEventCamera);
                WarningToast.Show("Drag the magic hand onto a card to upgrade or activate it.");
                return;
            }

            if (cardView.Spec.CostResource == CardResourceType.Mana)
            {
                ActivateMagicCard(cardView);
                ReevaluatePose(eventData.position, eventData.pressEventCamera);
                return;
            }

            if (!cardView.Spec.CanUpgrade)
            {
                ReevaluatePose(eventData.position, eventData.pressEventCamera);
                WarningToast.Show("This card cannot be upgraded.");
                return;
            }

            if (cardView.IsUpgraded)
            {
                ReevaluatePose(eventData.position, eventData.pressEventCamera);
                WarningToast.Show("This card is already upgraded.");
                return;
            }

            var result = _battleController.UpgradeCard(cardView.InstanceId);
            if (!result.Success)
            {
                Debug.LogWarning("[MagicHandCardBridge] Card upgrade failed: " + result.Message, this);
                WarningToast.Show(CombatWarningText.FromResult(result));
                return;
            }

            if (_playerAttackFeedback != null)
                _playerAttackFeedback.SpawnMagicFire();
            cardView.PlayUpgradeFlip();
            ReevaluatePose(eventData.position, eventData.pressEventCamera);
            Debug.Log("[MagicHandCardBridge] Upgraded " + cardView.Spec.DisplayName + ".", this);
        }

        /// <summary>拖拽结束后重新检测鼠标下的卡牌并切换姿态</summary>
        private void ReevaluatePose(Vector2 screenPosition, Camera eventCamera)
        {
            if (_playerAttackFeedback == null)
                _playerAttackFeedback = UnityEngine.Object.FindFirstObjectByType<PlayerAttackFeedback>();
            if (_playerAttackFeedback == null || _playerAttackFeedback.IsBusy) return;

            var cardView = FindCardAt(screenPosition, eventCamera);
            if (cardView == null || cardView.Spec == null)
            {
                _playerAttackFeedback.SwitchToMeleePose();
                return;
            }

            var category = cardView.Spec.Category;
            if (category == "ranged" || category == "guns")
                _playerAttackFeedback.SwitchToRangedPose();
            else if (category == "magic")
                _playerAttackFeedback.SwitchToMagicPose();
            else
                _playerAttackFeedback.SwitchToMeleePose();
        }

        private void ActivateMagicCard(CardView cardView)
        {
            if (cardView.IsActivated)
            {
                WarningToast.Show("This magic card is already activated.");
                return;
            }

            var result = _battleController.ActivateCard(cardView.InstanceId);
            if (!result.Success)
            {
                Debug.LogWarning("[MagicHandCardBridge] Card activation failed: " + result.Message, this);
                WarningToast.Show(CombatWarningText.FromResult(result));
                return;
            }

            if (_playerAttackFeedback != null)
                _playerAttackFeedback.SpawnMagicFire();
            cardView.PlayActivationFlip();
            Debug.Log("[MagicHandCardBridge] Activated " + cardView.Spec.DisplayName + ".", this);
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
