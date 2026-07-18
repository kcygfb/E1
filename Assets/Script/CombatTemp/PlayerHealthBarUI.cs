using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>Displays the authoritative player health from BattleState.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBarUI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text displayText;

        private BattleController _subscribedController;
        private Coroutine _initialRefreshRoutine;

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

            Unsubscribe();
        }

        private IEnumerator RefreshWhenBattleIsReady()
        {
            while (battleController != null && battleController.State == null)
                yield return null;

            _initialRefreshRoutine = null;
            Refresh();
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

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        private void Subscribe()
        {
            if (battleController == null || _subscribedController == battleController)
                return;

            Unsubscribe();
            _subscribedController = battleController;
            _subscribedController.CombatEventRaised += OnCombatEvent;
        }

        private void Unsubscribe()
        {
            if (_subscribedController != null)
                _subscribedController.CombatEventRaised -= OnCombatEvent;

            _subscribedController = null;
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            switch (combatEvent.Type)
            {
                case CombatEventType.BattleStarted:
                case CombatEventType.DamageApplied:
                case CombatEventType.HealingApplied:
                case CombatEventType.TurnStarted:
                    Refresh();
                    break;
            }
        }

        private void Refresh()
        {
            var state = battleController != null ? battleController.State : null;
            if (state == null)
                return;

            var current = state.Player.CurrentHealth;
            var maximum = state.Player.MaxHealth;

            if (fillImage != null)
                fillImage.fillAmount = maximum > 0 ? (float)current / maximum : 0f;
            if (displayText != null)
                displayText.text = current + " / " + maximum;
        }
    }
}
