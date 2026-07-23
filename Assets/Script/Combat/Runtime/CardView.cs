using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Reflection;
using TMPro;

namespace KiKs.Combat
{
    [RequireComponent(typeof(RectTransform))]
    public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public string CardId { get; private set; }
        public string InstanceId { get; private set; }
        public CardSpec Spec { get; private set; }
        public bool IsUpgraded { get; private set; }

        public System.Action<CardView> OnPlayRequested;
        public System.Action<CardView> OnShootRequested;
        public System.Action<CardView> OnHoverEnter;
        public System.Action<CardView> OnHoverExit;

        [Header("Card UI")]
        [SerializeField] private TMP_Text cardNameText;

        private RectTransform _rect;
        private bool _isAnimating;
        private bool _wasDragged;
        private Vector2 _dragStartPos;
        private const float DRAG_THRESHOLD = 10f;

        private int _totalShots;
        private int _remainingShots;

        /// <summary>是否是多段射击的枪械卡</summary>
        public bool IsMultiShot => _totalShots > 1 && _remainingShots > 0;

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

            // 检查是否是枪械多段射击卡
            _totalShots = GetTotalShots(spec);
            _remainingShots = _totalShots;

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
            {
                var name = Spec.DisplayName + (IsUpgraded ? " (UPGRADED)" : string.Empty);
                if (_totalShots >= 1)
                    name += $" [{_remainingShots}/{_totalShots}]";
                cardNameText.text = name;
            }
        }

        /// <summary>消耗一发子弹，返回是否是最后一发</summary>
        public bool ConsumeShot()
        {
            _remainingShots--;
            RefreshCardName();
            return _remainingShots <= 0;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isAnimating) return;
            if (_wasDragged) return;

            // 枪械多段射击：每次点击都走 OnShootRequested
            if (IsMultiShot && _remainingShots > 0)
            {
                OnShootRequested?.Invoke(this);
                return;
            }

            // 非多段卡：正常出牌
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
            var components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name != "CardInteraction") continue;
                SetField(comp, "_originPos", _rect.localPosition);
                SetField(comp, "_originScale", _rect.localScale);
            }
        }

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(obj, value);
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

            _rect.DOKill();

            var components = GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var name = comp.GetType().Name;
                if (name == "CardInteraction" || name == "Draggable")
                    comp.enabled = false;
            }

            foreach (var comp in components)
            {
                if (comp == null) continue;
                if (comp.GetType().Name == "CardInteraction")
                {
                    var method = comp.GetType().GetMethod("DestroyGlow");
                    method?.Invoke(comp, null);
                }
            }

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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isAnimating) return;
            OnHoverEnter?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke(this);
        }

        private static int GetTotalShots(CardSpec spec)
        {
            if (spec.Category != "ranged" && spec.Category != "guns") return 0;
            foreach (var effect in spec.Effects)
            {
                if (effect.Type == CardEffectType.Damage)
                    return effect.Hits.Resolve(false);
            }
            return 0;
        }
    }
}
