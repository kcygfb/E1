using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 战斗单位血条 UI —— 纯 Fill 单层驱动。
    /// 受击时 Fill 先闪烁高亮，再平滑下降至目标值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private CombatantSide targetSide = CombatantSide.Player;
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
            RefreshInstant();
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
            _subscribedController.CombatEventRaised += OnCombatEvent;
            Debug.Log("[PlayerHealthBarUI] Subscribed to CombatEventRaised on " + battleController.name, this);
        }

        private void Unsubscribe()
        {
            if (_subscribedController != null)
            {
                _subscribedController.CombatEventRaised -= OnCombatEvent;
                Debug.Log("[PlayerHealthBarUI] Unsubscribed from CombatEventRaised", this);
            }

            _subscribedController = null;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            Debug.Log("[PlayerHealthBarUI] OnCombatEvent: " + combatEvent.Type + " | Amount=" + combatEvent.Amount +
                      " | TargetId=" + combatEvent.TargetId, this);

            switch (combatEvent.Type)
            {
                case CombatEventType.BattleStarted:
                    RefreshInstant();
                    break;
                case CombatEventType.DamageApplied:
                    if (TryResolveEventTarget(combatEvent, out var damageTarget))
                        RefreshWithDamageEffect(damageTarget);
                    break;
                case CombatEventType.HealingApplied:
                    if (TryResolveEventTarget(combatEvent, out var healingTarget))
                        RefreshWithHealEffect(healingTarget);
                    break;
            }
        }

        private bool TryResolveEventTarget(CombatEvent combatEvent, out CombatantState target)
        {
            target = null;
            var state = battleController != null ? battleController.State : null;
            if (state == null)
                return false;

            if (targetSide == CombatantSide.Player)
            {
                target = state.Player;
                return target != null && combatEvent.TargetId == target.Id;
            }

            target = state.FindEnemy(combatEvent.TargetId);
            return target != null;
        }

        private CombatantState ResolveTrackedCombatant()
        {
            var state = battleController != null ? battleController.State : null;
            if (state == null)
                return null;

            return targetSide == CombatantSide.Player
                ? state.Player
                : state.FindFirstLivingEnemy();
        }

        private void RefreshInstant()
        {
            KillAllTween();

            var combatant = ResolveTrackedCombatant();
            if (combatant == null)
                return;

            var current = combatant.CurrentHealth;
            var maximum = combatant.MaxHealth;
            var target = maximum > 0 ? (float)current / maximum : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = target;
                fillImage.color = _originFillColor;
            }

            if (displayText != null)
                displayText.text = current + " / " + maximum;

            _displayedFillAmount = target;

            Debug.Log("[PlayerHealthBarUI] RefreshInstant (" + targetSide + "): " + current + "/" + maximum +
                      " | fillAmount=" + target.ToString("F2"), this);
        }

        private void RefreshWithDamageEffect(CombatantState combatant)
        {
            var current = combatant.CurrentHealth;
            var maximum = combatant.MaxHealth;
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

            Debug.Log("[PlayerHealthBarUI] RefreshWithDamageEffect: target=" + target.ToString("F2") +
                      " | startFill=" + startFill.ToString("F2") +
                      " | health=" + current + "/" + maximum, this);

            // 没有实际减少则不播放特效
            if (target >= startFill - 0.001f)
            {
                Debug.Log("[PlayerHealthBarUI] No player health decrease detected.", this);
                _displayedFillAmount = target;
                return;
            }

            KillAllTween();

            // 1) 冻结 Fill 在当前值
            fillImage.fillAmount = startFill;
            Debug.Log("[PlayerHealthBarUI] Freezing fillAmount at " + startFill.ToString("F2") +
                      " | Starting flash sequence...", this);

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
                Debug.Log("[PlayerHealthBarUI] Animation sequence finished.", this);
            });
        }

        private void RefreshWithHealEffect(CombatantState combatant)
        {
            var current = combatant.CurrentHealth;
            var maximum = combatant.MaxHealth;
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
