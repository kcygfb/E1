using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using KiKs.UI;
using System.Reflection;

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
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text toughnessText;
        [SerializeField] private Image cardArtImage;

        [Header("Card Text Auto Size")]
        [SerializeField] private float descriptionFontSizeMin = 18f;
        [SerializeField] private float descriptionFontSizeMax = 32f;
        [SerializeField] private float statFontSizeMin = 28f;
        [SerializeField] private float statFontSizeMax = 48f;

        private RectTransform _rect;
        private float _lastDragEndTime;
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

            ResolveCardTextReferences();
            ConfigureCardTexts();
            RefreshCardText();

            // 根据卡牌类型设置高光颜色
            SyncCardInteraction();
            GetComponent<CardInteraction>()?.SetGlowColorByCategory(spec.Category);
            if (cardNameText != null)
                cardNameText.gameObject.SetActive(false);

            EnsureCardArt();
            RefreshCardArt();
        }

        public void SetUpgraded(bool isUpgraded)
        {
            IsUpgraded = isUpgraded;
            RefreshCardText();
            RefreshCardArt();
        }

        /// <summary>强化翻转动画：Y轴翻转，中途切换为强化精灵图</summary>
        public void PlayUpgradeFlip(System.Action onComplete = null)
        {
            IsUpgraded = true;
            RefreshCardText();

            _isAnimating = true;
            _rect.DOKill();

            // 暂时禁用交互，防止悬浮动画干扰翻转
            var interaction = GetComponent<CardInteraction>();
            if (interaction != null) interaction.enabled = false;
            var skew = GetComponent<CardSkew>();
            float originalSkew = skew != null ? skew.Skew : 0f;
            if (skew != null) { skew.Skew = 0f; skew.enabled = false; }

            var originScale = _rect.localScale;

            var seq = DOTween.Sequence();
            // 前半段：Y轴旋转 0→90°（卡牌侧转消失）
            seq.Append(_rect.DOLocalRotate(new Vector3(0, 90, 0), 0.18f).SetEase(Ease.InCubic));
            // 中点：切换为强化精灵图
            seq.AppendCallback(RefreshCardArt);
            // 后半段：Y轴旋转 90→0°（翻回正面，显示强化图）
            seq.Append(_rect.DOLocalRotate(Vector3.zero, 0.18f).SetEase(Ease.OutCubic));
            // 翻转时轻微放大，结束回弹
            seq.Join(_rect.DOScale(originScale * 1.08f, 0.18f).SetEase(Ease.OutCubic));
            seq.Append(_rect.DOScale(originScale, 0.1f).SetEase(Ease.OutQuint));
            seq.OnComplete(() =>
            {
                _isAnimating = false;
                if (interaction != null) interaction.enabled = true;
                if (skew != null) { skew.enabled = true; skew.Skew = originalSkew; }
                SyncCardInteraction();
                onComplete?.Invoke();
            });
        }

        private void RefreshCardText()
        {
            RefreshCardName();

            if (Spec == null)
                return;

            SetText(descriptionText, Spec.DescriptionEn, false);
            SetText(damageText, FormatEffectValue("Damage", CardEffectType.Damage), true);
            SetText(toughnessText, FormatEffectValue("Toughness", CardEffectType.ToughnessDamage), true);
        }

        private void RefreshCardName()
        {
            if (cardNameText == null || Spec == null)
                return;

            var name = Spec.DisplayName + (IsUpgraded ? " (UPGRADED)" : string.Empty);
            if (_totalShots >= 1)
                name += $" [{_remainingShots}/{_totalShots}]";
            cardNameText.text = name;
        }

        private void ResolveCardTextReferences()
        {
            if (cardNameText == null)
                cardNameText = FindText("CardNameText");
            if (descriptionText == null)
                descriptionText = FindText("DescriptionText");
            if (damageText == null)
                damageText = FindText("DamageText");
            if (toughnessText == null)
                toughnessText = FindText("ToughnessText");
        }

        private TMP_Text FindText(string objectName)
        {
            var directChild = transform.Find(objectName);
            if (directChild != null)
                return directChild.GetComponent<TMP_Text>();

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name == objectName)
                    return text;

            return null;
        }

        private void ConfigureCardTexts()
        {
            ConfigureCardText(descriptionText, descriptionFontSizeMin, descriptionFontSizeMax, true);
            ConfigureCardText(damageText, statFontSizeMin, statFontSizeMax, false);
            ConfigureCardText(toughnessText, statFontSizeMin, statFontSizeMax, false);
        }

        private static void ConfigureCardText(TMP_Text text, float minSize, float maxSize, bool allowWrapping)
        {
            if (text == null)
                return;

            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.color = Color.black;
            text.textWrappingMode = allowWrapping
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        private string FormatEffectValue(string label, CardEffectType effectType)
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (var effect in Spec.Effects)
            {
                if (effect.Type != effectType)
                    continue;

                var amount = effect.Amount.Resolve(IsUpgraded);
                var hits = Mathf.Max(1, effect.Hits.Resolve(IsUpgraded));
                if (effect.Unit == ValueUnit.Percent)
                {
                    values.Add(hits == 1 ? amount + "%" : amount + "%x" + hits);
                }
                else
                {
                    values.Add((amount * hits).ToString());
                }
            }

            return values.Count > 0 ? label + ": " + string.Join("+", values) : label + ": -";
        }

        private static void SetText(TMP_Text text, string value, bool showFallback)
        {
            if (text == null)
                return;

            text.text = string.IsNullOrWhiteSpace(value) && showFallback ? "-" : value;
            text.gameObject.SetActive(!string.IsNullOrWhiteSpace(text.text));
        }

        /// <summary>消耗一发子弹，返回是否是最后一发</summary>
        public bool ConsumeShot()
        {
            _remainingShots--;
            RefreshCardName();
            return _remainingShots <= 0;
        }

        private void EnsureCardArt()
        {
            if (cardArtImage != null)
                return;

            var existing = transform.Find("CardArt");
            if (existing != null)
            {
                cardArtImage = existing.GetComponent<Image>();
                return;
            }

            var artGO = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artGO.transform.SetParent(transform, false);
            artGO.transform.SetAsFirstSibling();

            var rt = artGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            cardArtImage = artGO.GetComponent<Image>();
            cardArtImage.preserveAspect = true;
        }

        private void RefreshCardArt()
        {
            if (Spec == null || string.IsNullOrEmpty(Spec.ImagePath))
                return;

            var path = IsUpgraded
                ? CardImageLoader.ResolveUpgradedPath(Spec.ImagePath) ?? Spec.ImagePath
                : Spec.ImagePath;

            CardImageLoader.ApplyToImage(cardArtImage, path);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isAnimating) return;
            if (_wasDragged) return;

            // 魔手拖拽释放在卡牌上时，EventSystem 可能误触发 Click —— 忽略拖拽期间及拖拽刚结束的点击
            if (IsAnyDraggableActive()) return;
            if (Time.realtimeSinceStartup - _lastDragEndTime < 0.2f) return;

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
            _lastDragEndTime = Time.realtimeSinceStartup;

            if (eventData.position.y > Screen.height * 0.5f)
            {
                TryPlayCard();
            }
            else
            {
                WarningToast.Show("Drag cards into the battle area.");
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

        /// <summary>反射检查是否有任意 Draggable 正在拖拽（跨 asmdef，避免编译依赖）</summary>
        private static bool IsAnyDraggableActive()
        {
            return Draggable.AnyDragging;
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
