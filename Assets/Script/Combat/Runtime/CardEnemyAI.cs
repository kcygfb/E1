using System.Collections;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// 卡牌怪物 AI 驱动器：监听 EnemyTurnStarted 事件，调用策略执行回合。
    /// 替换旧的 SimpleEnemyAI。在 Inspector 里拖入 EnemyAIStrategy ScriptableObject。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleController))]
    public class CardEnemyAI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [Tooltip("AI 策略 ScriptableObject，右键 Create > KiKs > Combat > AI > Simple Card AI")]
        [SerializeField] private EnemyAIStrategy strategy;

        [Tooltip("如果策略未指定，使用默认 SimpleCardAI 逻辑")]
        [SerializeField] private bool useFallbackIfNoStrategy = true;

        [SerializeField] private int fallbackDamage = 20;
        [SerializeField] private int fallbackToughnessDamage = 10;
        [SerializeField] private float fallbackDelay = 0.3f;

        private Coroutine _turnRoutine;
        private SimpleCardAI _fallbackStrategy;

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
                Debug.LogError("[CardEnemyAI] requires a BattleController.", this);
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
            {
                var enemy = battleController.State?.FindFirstLivingEnemy();
                if (enemy != null)
                    _turnRoutine = StartCoroutine(RunEnemyTurn(enemy.Id));
            }
        }

        private IEnumerator RunEnemyTurn(string enemyId)
        {
            // 等一帧让 BattleController 转发完所有事件
            yield return null;

            var state = battleController.State;
            if (state == null || state.Phase != CombatPhase.EnemyTurn ||
                state.Outcome != BattleOutcome.None)
            {
                _turnRoutine = null;
                yield break;
            }

            var activeStrategy = strategy;

            // 没有策略 → 创建临时 fallback
            if (activeStrategy == null && useFallbackIfNoStrategy)
            {
                if (_fallbackStrategy == null)
                    _fallbackStrategy = ScriptableObject.CreateInstance<SimpleCardAI>();
                activeStrategy = _fallbackStrategy;
            }

            if (activeStrategy != null)
            {
                var engine = battleController.GetEngineInternal();
                if (engine != null)
                    yield return StartCoroutine(activeStrategy.ExecuteTurn(enemyId, battleController, engine));
            }
            else
            {
                // 最后 fallback：固定伤害
                yield return new WaitForSeconds(fallbackDelay);
                battleController.ResolveEnemyAttack(enemyId, fallbackDamage, fallbackToughnessDamage);
            }

            // 结束回合
            state = battleController.State;
            if (state != null && state.Phase == CombatPhase.EnemyTurn &&
                state.Outcome == BattleOutcome.None)
            {
                battleController.CompleteEnemyTurn();
            }

            _turnRoutine = null;
        }
    }
}
