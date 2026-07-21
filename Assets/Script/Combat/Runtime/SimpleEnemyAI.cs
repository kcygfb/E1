using System.Collections;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>Runs one fixed enemy attack after the player explicitly ends the turn.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleController))]
    public sealed class SimpleEnemyAI : MonoBehaviour
    {
        private const int AttackDamage = 20;
        private const int AttackToughnessDamage = 10;

        [SerializeField] private BattleController battleController;

        private Coroutine _turnRoutine;

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
        }

        private void OnCombatEvent(CombatEvent combatEvent)
        {
            if (combatEvent.Type == CombatEventType.EnemyTurnStarted && _turnRoutine == null)
                _turnRoutine = StartCoroutine(RunEnemyTurn());
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
                var attackResult = battleController.ResolveEnemyAttack(
                    enemy.Id,
                    AttackDamage,
                    AttackToughnessDamage);
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
