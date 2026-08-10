using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace KiKs.Combat
{
    /// <summary>
    /// 怪物卡牌视觉表现：监听 CombatEvent，在怪物区域显示抽牌/出牌/弃牌动画。
    /// 和玩家系统完全独立，不依赖 CardDealAnimator/BattleCardBridge。
    /// </summary>
    [RequireComponent(typeof(BattleController))]
    public class EnemyCardPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BattleController battleController;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private RectTransform enemyHandArea;
        [SerializeField] private RectTransform enemyDeckArea;
        [SerializeField] private RectTransform enemyDiscardArea;
        [SerializeField] private RectTransform playerArea;

        [Header("Animation")]
        [SerializeField] private float drawDuration = 0.4f;
        [SerializeField] private float playDuration = 0.5f;
        [SerializeField] private float discardDuration = 0.3f;
        [SerializeField] private float cardSpacing = 140f;
        [SerializeField] private float cardScale = 0.6f;

        private readonly Dictionary<string, CardView> _enemyCards = new();
        private readonly List<CardView> _enemyHandCards = new();
        private string _currentEnemyId;

        private void Awake()
        {
            if (battleController == null)
                battleController = GetComponent<BattleController>();
        }

        private void OnEnable()
        {
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDisable()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            switch (evt.Type)
            {
                case CombatEventType.EnemyTurnStarted:
                    _currentEnemyId = evt.SourceId;
                    break;

                case CombatEventType.CardDrawn when IsEnemySource(evt.SourceId):
                    OnEnemyCardDrawn(evt);
                    break;

                case CombatEventType.CardPlayed when IsEnemySource(evt.SourceId):
                    OnEnemyCardPlayed(evt);
                    break;

                case CombatEventType.CardDiscarded when IsEnemySource(evt.SourceId):
                    OnEnemyCardDiscarded(evt);
                    break;

                case CombatEventType.PhaseChanged when evt.Amount == (int)CombatPhase.PlayerTurnStart:
                    ClearAllEnemyCards();
                    break;
            }
        }

        private bool IsEnemySource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return false;
            if (battleController?.State == null) return false;
            if (sourceId == battleController.State.Player.Id) return false;
            return battleController.State.FindEnemy(sourceId) != null;
        }

        private void OnEnemyCardDrawn(CombatEvent evt)
        {
            if (cardPrefab == null || enemyHandArea == null) return;

            var deck = battleController.State.GetEnemyDeck(evt.SourceId);
            if (deck == null) return;

            var card = deck.FindInHand(evt.CardInstanceId);
            if (card == null) return;

            var cardObj = Instantiate(cardPrefab, enemyHandArea);
            var cardView = cardObj.GetComponent<CardView>();
            if (cardView == null)
            {
                cardView = cardObj.AddComponent<CardView>();
            }

            cardView.Setup(card.Spec, card.InstanceId);

            // Disable interaction — enemy cards are not clickable
            DisableInteraction(cardObj);

            // Scale down
            cardObj.transform.localScale = Vector3.one * cardScale;

            // Start position: from deck area
            Vector2 startPos = enemyDeckArea != null
                ? enemyDeckArea.anchoredPosition
                : new Vector2(-400, 0);

            var rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = startPos;
            rt.localScale = Vector3.one * (cardScale * 0.3f);

            _enemyCards[evt.CardInstanceId] = cardView;
            _enemyHandCards.Add(cardView);

            // Animate to hand position
            ArrangeEnemyHand();
            Vector2 targetPos = GetEnemyCardPosition(_enemyHandCards.Count - 1);

            var seq = DOTween.Sequence();
            seq.Join(rt.DOAnchorPos(targetPos, drawDuration).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(cardScale, drawDuration).SetEase(Ease.OutBack));
            // 入场动画结束后再记录基准值，避免与飞入缩放冲突
            seq.OnComplete(() =>
            {
                var hover = cardObj.GetComponent<EnemyCardHover>();
                if (hover == null) hover = cardObj.AddComponent<EnemyCardHover>();
                hover.Initialize();
            });
        }

        private void OnEnemyCardPlayed(CombatEvent evt)
        {
            if (!_enemyCards.TryGetValue(evt.CardInstanceId, out var cardView))
            {
                // Card not tracked, create a temporary one
                if (cardPrefab != null && enemyHandArea != null)
                {
                    var tempObj = Instantiate(cardPrefab, enemyHandArea);
                    cardView = tempObj.GetComponent<CardView>();
                    if (cardView == null) cardView = tempObj.AddComponent<CardView>();
                    var spec = FindCardSpec(evt.CardInstanceId);
                    if (spec != null) cardView.Setup(spec, evt.CardInstanceId);
                    DisableInteraction(tempObj);
                    tempObj.transform.localScale = Vector3.one * cardScale;
                }
            }

            if (cardView == null) return;

            _enemyHandCards.Remove(cardView);

            // Animate flying toward player
            Vector3 targetPos = playerArea != null
                ? playerArea.position
                : new Vector3(960, 540, 0);

            var rt = cardView.GetComponent<RectTransform>();
            var seq = DOTween.Sequence();
            // 先快速左右晃动几下（增强打出确认感），再侧滑向玩家区域
            var originPos = rt.anchoredPosition;
            seq.Append(rt.DOShakeAnchorPos(
                0.4f,
                new Vector2(26f, 0f),
                vibrato: 7,
                randomness: 15,
                snapping: false,
                fadeOut: true).SetEase(Ease.OutQuad));
            seq.AppendCallback(() => rt.anchoredPosition = originPos);
            seq.Append(rt.DOMove(targetPos, playDuration).SetEase(Ease.InCubic));
            seq.Join(rt.DOScale(cardScale * 0.5f, playDuration).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                Destroy(cardView.gameObject);
                _enemyCards.Remove(evt.CardInstanceId);
                ArrangeEnemyHand();
            });
        }

        private void OnEnemyCardDiscarded(CombatEvent evt)
        {
            if (!_enemyCards.TryGetValue(evt.CardInstanceId, out var cardView)) return;

            _enemyHandCards.Remove(cardView);

            Vector3 targetPos = enemyDiscardArea != null
                ? enemyDiscardArea.position
                : new Vector3(1500, 200, 0);

            var rt = cardView.GetComponent<RectTransform>();
            var seq = DOTween.Sequence();
            seq.Append(rt.DOMove(targetPos, discardDuration).SetEase(Ease.InCubic));
            seq.Join(rt.DOScale(cardScale * 0.3f, discardDuration).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                Destroy(cardView.gameObject);
                _enemyCards.Remove(evt.CardInstanceId);
                ArrangeEnemyHand();
            });
        }

        private void ClearAllEnemyCards()
        {
            foreach (var card in _enemyCards.Values)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _enemyCards.Clear();
            _enemyHandCards.Clear();
            _currentEnemyId = null;
        }

        private void ArrangeEnemyHand()
        {
            for (int i = 0; i < _enemyHandCards.Count; i++)
            {
                var card = _enemyHandCards[i];
                if (card == null) continue;
                var pos = GetEnemyCardPosition(i);
                card.GetComponent<RectTransform>().DOAnchorPos(pos, 0.2f).SetEase(Ease.OutCubic);
                // 手牌位置变动后，同步悬停组件的基准位置
                var hover = card.GetComponent<EnemyCardHover>();
                if (hover != null) hover.SetBasePosition(pos);
            }
        }

        private Vector2 GetEnemyCardPosition(int index)
        {
            float totalWidth = (_enemyHandCards.Count - 1) * cardSpacing;
            float startX = -totalWidth / 2f;
            return new Vector2(startX + index * cardSpacing, 0);
        }

        private CardSpec FindCardSpec(string instanceId)
        {
            if (battleController?.State == null) return null;
            foreach (var enemy in battleController.State.Enemies)
            {
                var specialCard = battleController.State.GetEnemySpecialCard(enemy.Id);
                if (specialCard != null && specialCard.InstanceId == instanceId)
                    return specialCard.Spec;
                var deck = battleController.State.GetEnemyDeck(enemy.Id);
                if (deck == null) continue;
                var card = deck.FindInHand(instanceId);
                if (card != null) return card.Spec;
                foreach (var discarded in deck.DiscardPile)
                {
                    if (discarded.InstanceId == instanceId) return discarded.Spec;
                }
            }
            return null;
        }

        private static void DisableInteraction(GameObject cardObj)
        {
            var interaction = cardObj.GetComponent<KiKs.UI.CardInteraction>();
            if (interaction != null) interaction.enabled = false;

            var draggable = cardObj.GetComponent<KiKs.UI.Draggable>();
            if (draggable != null) draggable.enabled = false;

            var bridge = cardObj.GetComponent<CardDragBridge>();
            if (bridge != null) bridge.enabled = false;

            var button = cardObj.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.enabled = false;

            var feedback = cardObj.GetComponent<KiKs.UI.ButtonFeedback>();
            if (feedback != null) feedback.enabled = false;
        }
    }
}
