using System.Collections.Generic;
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

        [Header("Enemy Presentation")]
        [SerializeField] private Sprite portrait;
        [SerializeField] private Vector2 portraitSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 portraitOffset = Vector2.zero;
        [Min(0.01f)]
        [SerializeField] private float portraitScale = 1f;

        [Header("Enemy Deck")]
        [Tooltip("Selects the dedicated enemy JSON deck. Turn rules come from Enemy Rank in CombatRules.")]
        [SerializeField] private EnemyArchetype enemyArchetype = EnemyArchetype.None;

        [Header("Legacy/Custom Enemy Deck")]
        [Tooltip("卡牌ID列表，和玩家共用同一套 CardDataV2 JSON。如 melee_long_axe, defense_block 等")]
        [SerializeField] private List<string> enemyCardIds = new();
        [Tooltip("每回合抽几张牌")]
        [HideInInspector]
        [Min(1)] [SerializeField] private int cardsPerTurn = 2;
        [Tooltip("手牌上限")]
        [HideInInspector]
        [Min(1)] [SerializeField] private int enemyHandLimit = 5;
        [Tooltip("每回合行动次数（出几张牌）")]
        [HideInInspector]
        [Min(1)] [SerializeField] private int enemyActionsPerTurn = 1;
        [Tooltip("没牌或没行动时的固定伤害")]
        [Min(0)] [SerializeField] private int fallbackDamage = 20;
        [Tooltip("没牌时的固定破甲伤害")]
        [Min(0)] [SerializeField] private int fallbackToughnessDamage = 10;
        [Tooltip("怪物每回合的行动点")]
        [HideInInspector]
        [Min(0)] [SerializeField] private int baseActionPoints = 2;

        public string CombatantId => combatantId;
        public string DisplayName => displayName;
        public CombatantSide Side => side;
        public EnemyRank EnemyRank => enemyRank;
        public EnemyArchetype EnemyArchetype => enemyArchetype;
        public Sprite Portrait => portrait;
        public Vector2 PortraitSize => portraitSize;
        public Vector2 PortraitOffset => portraitOffset;
        public float PortraitScale => portraitScale;
        public string EnemyCardCategory
        {
            get
            {
                switch (enemyArchetype)
                {
                    case EnemyArchetype.Dog: return "enemy_dog";
                    case EnemyArchetype.LittleGirl: return "enemy_little_girl";
                    case EnemyArchetype.BigEye: return "enemy_big_eye";
                    case EnemyArchetype.Ghost: return "enemy_ghost";
                    case EnemyArchetype.Cat: return "enemy_cat";
                    case EnemyArchetype.Nightmare: return "enemy_nightmare";
                    case EnemyArchetype.Fatty: return "enemy_fatty";
                    case EnemyArchetype.Thief: return "enemy_thief";
                    case EnemyArchetype.Merchant: return "enemy_merchant";
                    case EnemyArchetype.Butcher: return "enemy_butcher";
                    default: return string.Empty;
                }
            }
        }

        public IReadOnlyList<string> EnemyCardIds => enemyCardIds;
        public int CardsPerTurn => cardsPerTurn;
        public int EnemyHandLimit => enemyHandLimit;
        public int EnemyActionsPerTurn => enemyActionsPerTurn;
        public int FallbackDamage => fallbackDamage;
        public int FallbackToughnessDamage => fallbackToughnessDamage;
        public int BaseActionPoints => baseActionPoints;

        public bool HasEnemyDeck => enemyCardIds != null && enemyCardIds.Count > 0;

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
