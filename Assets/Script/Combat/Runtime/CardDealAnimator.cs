using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace KiKs.Combat
{
    public class CardDealAnimator : MonoBehaviour
    {
        [Header("区域")]
        [SerializeField] private RectTransform deckArea;
        [SerializeField] private RectTransform handArea;
        [SerializeField] private RectTransform discardArea;

        [Header("卡牌")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private float cardSpacing = 250f;
        [SerializeField] private float drawDuration = 0.35f;
        [SerializeField] private float maxHandWidth = 1600f;
        [SerializeField] private float heightJitter = 20f;      // 高低交错幅度
        [SerializeField] private float rotationJitter = 5f;    // 倾斜交错幅度

        private readonly List<CardView> _handCards = new();

        public IReadOnlyList<CardView> HandCards => _handCards;
        public System.Func<CardView, bool> OnCardPlayed;

        public CardView DrawCard(CardSpec spec, string instanceId = null)
        {
            if (cardPrefab == null)
            {
                Debug.LogError("[CardDealAnimator] cardPrefab not assigned");
                return null;
            }

            var cardObj = Instantiate(cardPrefab, handArea != null ? handArea : transform);
            var cardView = cardObj.GetComponent<CardView>();
            if (cardView == null)
                cardView = cardObj.AddComponent<CardView>();

            cardView.Setup(spec, instanceId);
            cardView.OnPlayRequested += HandleCardPlayed;
            _handCards.Add(cardView);

            // 计算 deck 在 handArea 空间下的 anchoredPosition
            Vector2 deckAnchored = GetAnchoredPosInHand(deckArea);
            Vector2 targetAnchored = GetCardAnchoredPosition(_handCards.Count - 1);

            cardView.PlayDrawAnimation(deckAnchored, targetAnchored, drawDuration, ArrangeHand);
            return cardView;
        }

        public void DiscardCard(CardView cardView)
        {
            if (cardView == null || !_handCards.Contains(cardView)) return;
            cardView.OnPlayRequested -= HandleCardPlayed;
            _handCards.Remove(cardView);

            Vector3 discardWorld = discardArea != null ? discardArea.position : Vector3.zero;
            cardView.PlayDiscardAnimation(discardWorld, () => Destroy(cardView.gameObject));
            ArrangeHand();
        }

        public void ArrangeHand()
        {
            if (_handCards.Count == 0) return;
            for (int i = 0; i < _handCards.Count; i++)
            {
                var card = _handCards[i];
                if (card == null) continue;
                var rt = card.GetComponent<RectTransform>();
                if (rt == null) continue;
                var targetAnchored = GetCardAnchoredPosition(i);
                var targetRot = GetCardRotation(i);
                rt.DOKill();

                int capturedIndex = i;
                rt.DOLocalMove(targetAnchored, 0.2f).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    card.SyncCardInteraction();
                });
                rt.DOLocalRotate(targetRot, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        private Vector3 GetCardRotation(int index)
        {
            // 奇数张正向倾斜、偶数张反向倾斜
            float angle = (index % 2 == 0) ? -rotationJitter : rotationJitter;
            return new Vector3(0, 0, angle);
        }

        /// <summary>
        /// 计算第 index 张手牌在 handArea 空间下的 anchoredPosition
        /// </summary>
        private Vector2 GetCardAnchoredPosition(int index)
        {
            if (handArea == null) return Vector2.zero;

            int count = _handCards.Count;
            // 用 handArea 的实际宽度，减去两侧留白
            float areaWidth = Mathf.Abs(handArea.rect.width);
            if (areaWidth < 100f) areaWidth = maxHandWidth;

            // 动态间距：手牌少时用固定间距，多了就按区域宽度平分
            float spacing = cardSpacing;
            float totalWidth = (count - 1) * spacing;
            if (totalWidth > areaWidth)
            {
                totalWidth = areaWidth;
                spacing = count > 1 ? areaWidth / (count - 1) : 0;
            }

            var startX = -totalWidth * 0.5f;
            float x = startX + index * spacing;
            // 奇数张高、偶数张低，形成高低交错
            float y = (index % 2 == 0) ? heightJitter : -heightJitter;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 将 target 的世界坐标转换为 handArea 空间下的 anchoredPosition
        /// </summary>
        private Vector2 GetAnchoredPosInHand(RectTransform target)
        {
            if (target == null || handArea == null) return Vector2.zero;

            // 把 target 的世界坐标转为 handArea 的本地坐标
            Vector3 localPos = handArea.InverseTransformPoint(target.position);

            // 对于 anchor=(0.5,0.5) 的子物体，anchoredPosition ≈ localPosition
            return new Vector2(localPos.x, localPos.y);
        }

        private void HandleCardPlayed(CardView cardView)
        {
            bool success = OnCardPlayed?.Invoke(cardView) ?? true;
            if (success)
            {
                DiscardCard(cardView);
            }
            else
            {
                var index = _handCards.IndexOf(cardView);
                if (index >= 0)
                    cardView.ReturnToHand(GetCardAnchoredPosition(index));
            }
        }

        /// <summary>结束回合：所有手牌飞到弃牌堆</summary>
        public void DiscardAllCards()
        {
            if (discardArea == null) return;
            Vector3 discardWorld = discardArea.position;

            for (int i = _handCards.Count - 1; i >= 0; i--)
            {
                var card = _handCards[i];
                if (card == null) continue;
                card.OnPlayRequested -= HandleCardPlayed;
                _handCards.RemoveAt(i);
                card.PlayDiscardAnimation(discardWorld, () => Destroy(card.gameObject));
            }
        }
    }
}