using System.Collections;

namespace KiKs.Combat
{
    /// <summary>
    /// AI 策略基类：每种 AI 逻辑是一个 ScriptableObject，在 Inspector 里选。
    /// 未来加新 AI 只需继承此类，不需要改框架代码。
    /// </summary>
    public abstract class EnemyAIStrategy : UnityEngine.ScriptableObject
    {
        /// <summary>执行一个完整的怪物回合。协程结束后自动调用 CompleteEnemyTurn。</summary>
        public abstract IEnumerator ExecuteTurn(
            string enemyId,
            BattleController controller,
            CombatEngine engine);
    }
}
