using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using KiKs.UI;
using System.Reflection;

namespace KiKs.Combat
{
    [RequireComponent(typeof(RectTransform))]
    public class CardView : MonoBehaviour, IPointerClickHandler
    {
        public string CardId { get; private set; }
        public string InstanceId { get; private set; }
        public CardSpec Spec { get; private set; }
        public bool IsUpgraded { get; private set; }

        public System.Action<CardView> OnPlayRequested;

        [Header("Card UI")]
        [SerializeField] private TMP_Text cardNameText;

        private RectTransform _rect;
        private bool _isAnimating;
        private bool _wasDragged;
        private Vector2 _dragStartPos;
        private const float DRAG_THRESHOLD = 10f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Setup(CardSpec spec, string instanceId = null)
        {
            Spec = spec;
            CardId = spec.Id;
            InstanceId = instanceId ?? spec.Id;
            IsUpgraded = false;
            gameObject.name = $"Card_{spec.Id}";
            transform.localScale = Vector3.one;

            if (cardNameText == null)
                cardNameText = GetComponentInChildren<TMP_Text>(true);
            RefreshCardName();
        }

        public void SetUpgraded(bool isUpgraded)
        {
            IsUpgraded = isUpgraded;
            RefreshCardName();
        }

        private void RefreshCardName()
        {
            if (cardNameText != null && Spec != null)
                cardNameText.text = Spec.DisplayName + (IsUpgraded ? " (UPGRADED)" : string.Empty);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isAnimating) return;
            if (_wasDragged) return;
            TryPlayCard();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _wasDragged = false;
            _dragStartPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Vector2.Distance(eventData.position, _dragStartPos) > DRAG_THRESHOLD)
                _wasDragged = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_wasDragged) return;
            _wasDragged = false;

            if (eventData.position.y > Screen.height * 0.5f)
            {
                // 不在这里 kill DOTween，由 CardDealAnimator 根据出牌结果决定
                TryPlayCard();
            }
        }

        private void TryPlayCard()
        {
            OnPlayRequested?.Invoke(this);
        }

        /// <summary>出牌失败时，弹回手牌位置</summary>
        public void ReturnToHand(Vector2 handPosition)
        {
            _rect.DOKill();
            _rect.DOLocalMove(handPosition, 0.25f).SetEase(Ease.OutBack);
        }

        public void SyncCardInteraction()
        {
            var interaction = GetComponent<CardInteraction>();
            if (interaction == null) return;
            // Private-field access kept as typed reflection until CardInteraction exposes
            // a public UpdateOrigin() method in its own assembly.
            typeof(CardInteraction).GetField("_originPos",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(interaction, _rect.localPosition);
            typeof(CardInteraction).GetField("_originScale",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(interaction, _rect.localScale);
        }

        public void PlayDrawAnimation(Vector2 deckPos, Vector2 targetPos, float duration, System.Action onComplete)
        {
            _isAnimating = true;
            _rect.localPosition = deckPos;
            _rect.localScale = Vector3.one * 0.3f;

            var seq = DOTween.Sequence();
            seq.Join(_rect.DOLocalMove(targetPos, duration).SetEase(Ease.OutCubic));
            seq.Join(_rect.DOScale(1f, duration).SetEase(Ease.OutBack));
            seq.OnComplete(() =>
            {
                _isAnimating = false;
                SyncCardInteraction();
                onComplete?.Invoke();
            });
        }

        public void PlayDiscardAnimation(Vector3 targetWorldPos, System.Action onComplete)
        {
            _isAnimating = true;

            // 先 kill 所有动画（包括 Draggable 的回归动画）
            _rect.DOKill();

            var interaction = GetComponent<CardInteraction>();
            var draggable = GetComponent<Draggable>();
            if (interaction != null) interaction.enabled = false;
            if (draggable != null) draggable.enabled = false;

            interaction?.DestroyGlow();

            var seq = DOTween.Sequence();
            seq.AppendInterval(5f / 60f);
            seq.Append(transform.DOScale(0.3f, 0.3f).SetEase(Ease.InCubic));
            seq.Join(transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.InCubic));
            seq.OnComplete(() =>
            {
                _isAnimating = false;
                onComplete?.Invoke();
            });
        }
    }
}
