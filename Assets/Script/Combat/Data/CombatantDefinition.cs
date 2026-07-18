using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Static authoring data for a player or enemy. Current HP and toughness live in CombatantState.
    /// </summary>
    [CreateAssetMenu(fileName = "Combatant", menuName = "KiKs/Combat/Combatant Definition")]
    public sealed class CombatantDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string combatantId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private CombatantSide side = CombatantSide.Enemy;
        [SerializeField] private EnemyRank enemyRank = EnemyRank.Minion;

        [Header("Base stats")]
        [Min(1)] [SerializeField] private int maxHealth = 100;
        [Min(0)] [SerializeField] private int maxToughness = 100;

        public string CombatantId => combatantId;
        public string DisplayName => displayName;
        public CombatantSide Side => side;
        public EnemyRank EnemyRank => enemyRank;

        public CombatantState CreateRuntimeState()
        {
            return new CombatantState(
                combatantId,
                displayName,
                side,
                enemyRank,
                maxHealth,
                maxToughness);
        }
    }
}
