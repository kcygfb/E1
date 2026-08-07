using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 玩家血条 UI（玩家头顶）—— 纯 Fill 单层驱动。
    /// 数据来源唯一：BattleController.PlayerHealthChanged（单条血量数据链路）。
    /// 受击/持续伤害时 Fill 先闪烁高亮，再平滑下降至目标值；治疗时平滑上升。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text displayText;

        [Header("伤害动画")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private int flashCount = 2;
        [SerializeField] private float tweenDuration = 0.35f;
        [SerializeField] private Ease tweenEase = Ease.OutCubic;

        private BattleController _subscribedController;
        private Coroutine _initialRefreshRoutine;
        private Tweener _fillTweener;
        private Sequence _damageSequence;
        private float _displayedFillAmount = -1f;
        private Color _originFillColor;
        private bool _colorCached;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            _initialRefreshRoutine = StartCoroutine(RefreshWhenBattleIsReady());
        }

        private void OnDisable()
        {
            if (_initialRefreshRoutine != null)
            {
                StopCoroutine(_initialRefreshRoutine);
                _initialRefreshRoutine = null;
            }

            KillAllTween();
            Unsubscribe();
        }

        private void KillAllTween()
        {
            _fillTweener?.Kill();
            _fillTweener = null;
            _damageSequence?.Kill();
            _damageSequence = null;

            if (fillImage != null && _colorCached)
                fillImage.color = _originFillColor;
        }

        private IEnumerator RefreshWhenBattleIsReady()
        {
            while (battleController != null && battleController.State == null)
                yield return null;

            _initialRefreshRoutine = null;
            if (battleController?.State?.Player != null)
                RefreshInstant(battleController.State.Player.CurrentHealth, battleController.State.Player.MaxHealth);
        }

        private void ResolveReferences()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();

            if (fillImage == null)
            {
                var fill = transform.Find("Fill");
                if (fill != null)
                    fillImage = fill.GetComponent<Image>();
            }

            if (displayText == null)
                displayText = GetComponentInChildren<TMP_Text>(true);

            if (fillImage != null && fillImage.type != Image.Type.Filled)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }

            if (fillImage != null && !_colorCached)
            {
                _originFillColor = fillImage.color;
                _colorCached = true;
            }
        }

        private void Subscribe()
        {
            if (battleController == null || _subscribedController == battleController)
                return;

            Unsubscribe();
            _subscribedController = battleController;
            _subscribedController.PlayerHealthChanged += OnPlayerHealthChanged;
        }

        private void Unsubscribe()
        {
            if (_subscribedController != null)
                _subscribedController.PlayerHealthChanged -= OnPlayerHealthChanged;

            _subscribedController = null;
        }

        private void OnPlayerHealthChanged(
            int currentHealth,
            int maxHealth,
            BattleController.PlayerHealthChangeKind kind)
        {
            switch (kind)
            {
                case BattleController.PlayerHealthChangeKind.Initialize:
                    RefreshInstant(currentHealth, maxHealth);
                    break;
                case BattleController.PlayerHealthChangeKind.Damage:
                case BattleController.PlayerHealthChangeKind.Tick:
                    RefreshWithDamageEffect(currentHealth, maxHealth);
                    break;
                case BattleController.PlayerHealthChangeKind.Heal:
                    RefreshWithHealEffect(currentHealth, maxHealth);
                    break;
            }
        }

        private void RefreshInstant(int current, int maximum)
        {
            KillAllTween();

            var target = maximum > 0 ? (float)current / maximum : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = target;
                fillImage.color = _originFillColor;
            }

            if (displayText != null)
                displayText.text = current + " / " + maximum;

            _displayedFillAmount = target;
        }

        private void RefreshWithDamageEffect(int current, int maximum)
        {
            var target = maximum > 0 ? (float)current / maximum : 0f;

            if (displayText != null)
                displayText.text = current + " / " + maximum;

            if (fillImage == null)
            {
                _displayedFillAmount = target;
                return;
            }

            var startFill = fillImage.fillAmount;
            if (_displayedFillAmount < 0f)
                _displayedFillAmount = startFill;

            // 没有实际减少则不播放特效
            if (target >= startFill - 0.001f)
            {
                _displayedFillAmount = target;
                return;
            }

            KillAllTween();

            // 1) 冻结 Fill 在当前值
            fillImage.fillAmount = startFill;

            // 2) Fill 颜色闪烁（高亮提示即将削减）
            _damageSequence = DOTween.Sequence();

            for (int i = 0; i < flashCount; i++)
            {
                _damageSequence.Append(fillImage.DOColor(flashColor, flashDuration));
                _damageSequence.Append(fillImage.DOColor(_originFillColor, flashDuration));
            }

            // 3) 闪烁结束后，在同一个 Sequence 中平滑下降 fillAmount
            _damageSequence.Append(fillImage.DOFillAmount(target, tweenDuration).SetEase(tweenEase));
            _damageSequence.OnComplete(() =>
            {
                if (fillImage != null)
                {
                    fillImage.fillAmount = target;
                    fillImage.color = _originFillColor;
                }

                _displayedFillAmount = target;
                _damageSequence = null;
            });
        }

        private void RefreshWithHealEffect(int current, int maximum)
        {
            var target = maximum > 0 ? (float)current / maximum : 0f;

            if (displayText != null)
                displayText.text = current + " / " + maximum;

            KillAllTween();

            if (fillImage == null)
            {
                _displayedFillAmount = target;
                return;
            }

            _displayedFillAmount = target;
            _fillTweener = fillImage.DOFillAmount(target, tweenDuration)
                .SetEase(tweenEase)
                .OnComplete(() => _fillTweener = null);
        }
    }
}
