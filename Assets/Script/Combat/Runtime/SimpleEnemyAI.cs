using System.Collections;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>Automatically ends an exhausted player turn and runs one fixed enemy attack.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleController))]
    public sealed class SimpleEnemyAI : MonoBehaviour
    {
        private const int AttackDamage = 20;

        [SerializeField] private BattleController battleController;

        private Coroutine _turnRoutine;
        private Coroutine _autoEndRoutine;

        private void Awake()
        {
            if (battleController == null)
                battleController = GetComponent<BattleController>();
        }

        private void OnEnable()
        {
            if (battleController == null)
                battleController = GetComponent<BattleController>();

            if (battleController == null)
            {
                Debug.LogError("SimpleEnemyAI requires a BattleController.", this);
                return;
            }

            battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDisable()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;

            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }

            if (_autoEndRoutine != null)
            {
                StopCoroutine(_autoEndRoutine);
                _autoEndRoutine = null;
            }
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Type == CombatEventType.EnemyTurnStarted && _turnRoutine == null)
                _turnRoutine = StartCoroutine(RunEnemyTurn());

            if (combatEvent.Type == CombatEventType.ActionPointsChanged)
                TryStartAutoEndPlayerTurn();
        }

        private void TryStartAutoEndPlayerTurn()
        {
            var state = battleController != null ? battleController.State : null;
            if (_autoEndRoutine == null && state != null &&
                state.Outcome == BattleOutcome.None &&
                state.Player.CurrentActionPoints <= 0)
            {
                _autoEndRoutine = StartCoroutine(EndPlayerTurnWhenReady());
            }
        }

        private IEnumerator EndPlayerTurnWhenReady()
        {
            // Let the card and all of its effects finish before changing turns.
            yield return null;

            while (battleController != null)
            {
                var state = battleController.State;
                if (state == null || state.Outcome != BattleOutcome.None ||
                    state.Player.CurrentActionPoints > 0)
                    break;

                if (state.Phase == CombatPhase.PlayerInput)
                {
                    var endResult = battleController.EndPlayerTurn();
                    if (!endResult.Success)
                        Debug.LogWarning("Automatic player turn end failed: " + endResult.Message, this);
                    break;
                }

                if (state.Phase != CombatPhase.PlayerTurnStart &&
                    state.Phase != CombatPhase.ResolvingCard &&
                    state.Phase != CombatPhase.AwaitingExecutionConfirmation)
                    break;

                yield return null;
            }

            _autoEndRoutine = null;
        }

        private IEnumerator RunEnemyTurn()
        {
            // Wait until BattleController has finished forwarding all turn-start events.
            yield return null;

            var state = battleController != null ? battleController.State : null;
            if (state == null || state.Phase != CombatPhase.EnemyTurn ||
                state.Outcome != BattleOutcome.None)
            {
                _turnRoutine = null;
                yield break;
            }

            var enemy = state.FindFirstLivingEnemy();
            if (enemy != null)
            {
                var attackResult = battleController.ResolveEnemyAttack(enemy.Id, AttackDamage);
                if (!attackResult.Success)
                    Debug.LogWarning("Simple enemy attack failed: " + attackResult.Message, this);
            }

            state = battleController.State;
            if (state != null && state.Phase == CombatPhase.EnemyTurn &&
                state.Outcome == BattleOutcome.None)
            {
                var endResult = battleController.CompleteEnemyTurn();
                if (!endResult.Success)
                    Debug.LogWarning("Simple enemy turn could not finish: " + endResult.Message, this);
            }

            _turnRoutine = null;
        }
    }
}
