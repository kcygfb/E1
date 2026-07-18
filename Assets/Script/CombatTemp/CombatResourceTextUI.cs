using System.Collections;
using TMPro;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>Keeps the combat action-point and mana labels in sync with BattleState.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatResourceTextUI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private TMP_Text actionPointText;
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private string actionPointFormat = "ACT Point : {0}";
        [SerializeField] private string manaFormat = "MAGIC Point : {0}";

        private void OnEnable()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (actionPointText == null)
                actionPointText = GetComponent<TMP_Text>();

            if (battleController == null)
                return;

            battleController.CombatEventRaised += OnCombatEvent;
            StartCoroutine(RefreshWhenBattleIsReady());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private IEnumerator RefreshWhenBattleIsReady()
        {
            while (battleController != null && battleController.State == null)
                yield return null;

            RefreshText();
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Type == CombatEventType.ActionPointsChanged ||
                combatEvent.Type == CombatEventType.ManaChanged)
            {
                RefreshText();
            }
        }

        private void RefreshText()
        {
            var state = battleController != null ? battleController.State : null;
            if (state == null)
                return;

            if (actionPointText != null)
                actionPointText.text = string.Format(actionPointFormat, state.Player.CurrentActionPoints);
            if (manaText != null)
                manaText.text = string.Format(manaFormat, state.Mana.Current);
        }
    }
}
