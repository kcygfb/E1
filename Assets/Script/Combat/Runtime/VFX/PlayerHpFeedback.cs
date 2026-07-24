using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace KiKs.Combat
{
    /// <summary>
    /// 玩家血量UI受击动效：图标抖动+闪红，文字变红后数字递减。
    /// 挂在 PlayerHpIconText 容器上，需要 HpIcon(Image) 和 HpText(TMP_Text) 子物体。
    /// </summary>
    public class PlayerHpFeedback : MonoBehaviour
    {
        [Header("图标受击")]
        [SerializeField] private float iconShakeStrength = 15f;
        [SerializeField] private float iconShakeDuration = 0.3f;
        [SerializeField] private int iconShakeVibrato = 20;
        [SerializeField] private float iconScalePunch = 1.3f;
        [SerializeField] private float iconScaleDuration = 0.2f;
        [SerializeField] private Color iconFlashColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float iconFlashDuration = 0.2f;

        [Header("文字受击")]
        [SerializeField] private Color textHitColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private float textColorDuration = 0.3f;
        [SerializeField] private float numberCountDuration = 0.5f;

        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        private Image _iconImage;
        private TMP_Text _hpText;
        private RectTransform _iconRect;
        private Color _iconOriginColor;
        private Color _textOriginColor;
        private int _displayedHp;
        private Sequence _iconSeq;
        private Sequence _textSeq;
        private bool _subscribed;

        private void Awake()
        {
            var icon = transform.Find("HpIcon");
            if (icon != null)
            {
                _iconImage = icon.GetComponent<Image>();
                _iconRect = icon.GetComponent<RectTransform>();
                if (_iconImage != null)
                    _iconOriginColor = _iconImage.color;
            }

            var text = transform.Find("HpText");
            if (text != null)
            {
                _hpText = text.GetComponent<TMP_Text>();
                if (_hpText != null)
                    _textOriginColor = _hpText.color;
            }
        }

        private void Start()
        {
            if (battleController == null || !battleController.IsInitialized)
            {
                var root = GameObject.Find("BattleController");
                if (root != null)
                    battleController = root.GetComponent<BattleController>();
            }
            if (battleController != null)
            {
                battleController.CombatEventRaised += OnCombatEvent;
                _subscribed = true;
                if (battleController.IsInitialized)
                    _displayedHp = battleController.State.Player.CurrentHealth;
            }
        }

        private void OnDestroy()
        {
            if (battleController != null && _subscribed)
                battleController.CombatEventRaised -= OnCombatEvent;
            _iconSeq?.Kill();
            _textSeq?.Kill();
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            if (evt.Type != CombatEventType.DamageApplied) return;
            if (battleController == null || !battleController.IsInitialized) return;
            if (evt.TargetId != battleController.State.Player.Id) return;
            if (evt.Amount <= 0) return;

            PlayHitFeedback(evt.Amount, battleController.State.Player.CurrentHealth);
        }

        private void PlayHitFeedback(int damageAmount, int targetHp)
        {
            // --- 图标：抖动 + 闪红 + 缩放 ---
            if (_iconImage != null && _iconRect != null)
            {
                _iconSeq?.Kill();
                _iconImage.color = iconFlashColor;

                var currentScale = _iconRect.localScale;
                _iconRect.localScale = currentScale * iconScalePunch;

                _iconSeq = DOTween.Sequence();
                _iconSeq.Join(_iconRect.DOShakePosition(iconShakeDuration, iconShakeStrength, iconShakeVibrato, 90f, false)
                    .SetEase(Ease.InOutQuad));
                _iconSeq.Join(_iconRect.DOScale(currentScale, iconScaleDuration).SetEase(Ease.OutBack));
                _iconSeq.Join(DOTween.To(() => _iconImage.color, c => _iconImage.color = c, _iconOriginColor, iconFlashDuration)
                    .SetEase(Ease.OutQuart));
            }

            // --- 文字：变红 + 数字递减 ---
            if (_hpText != null)
            {
                _textSeq?.Kill();

                _hpText.color = textHitColor;

                _textSeq = DOTween.Sequence();
                _textSeq.Join(DOTween.To(() => _displayedHp, v =>
                {
                    _displayedHp = Mathf.RoundToInt(v);
                    _hpText.text = _displayedHp + " / " + battleController.State.Player.MaxHealth;
                }, targetHp, numberCountDuration).SetEase(Ease.OutQuart));
                _textSeq.Join(DOTween.To(() => _hpText.color, c => _hpText.color = c, _textOriginColor, textColorDuration)
                    .SetEase(Ease.OutQuart).SetDelay(0.1f));
                _textSeq.OnComplete(() =>
                {
                    _displayedHp = targetHp;
                    _hpText.text = targetHp + " / " + battleController.State.Player.MaxHealth;
                });
            }
        }
    }
}
