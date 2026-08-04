using UnityEngine;

namespace KiKs.Combat
{
    [CreateAssetMenu(fileName = "CombatRules", menuName = "KiKs/Combat/Combat Rules")]
    public sealed class CombatRulesConfig : ScriptableObject
    {
        [Header("Turn")]
        [Min(0)] [SerializeField] private int baseActionPoints = 3;
        [Min(0)] [SerializeField] private int cardsDrawnPerTurn = 4;
        [Min(1)] [SerializeField] private int handLimit = 10;
        [Min(1)] [SerializeField] private int expectedInitialDeckSize = 15;

        [Header("Mana and in-battle upgrades")]
        [Tooltip("玩家回合开始时获得的魔法点。后续机制可提高这个数量。")]
        [Min(1)] [SerializeField] private int manaPerTurn = 1;
        [Min(0)] [SerializeField] private int cardUpgradeManaCost = 1;
        [Header("Enemy cards - Minion")]
        [Min(1)] [SerializeField] private int minionDeckSize = 3;
        [Min(0)] [SerializeField] private int minionActionPoints = 3;
        [Min(0)] [SerializeField] private int minionCardsDrawnPerTurn = 1;
        [Min(0)] [SerializeField] private int minionCardsPlayedPerTurn = 1;

        [Header("Enemy cards - Elite")]
        [Min(1)] [SerializeField] private int eliteDeckSize = 4;
        [Min(0)] [SerializeField] private int eliteActionPoints = 4;
        [Min(0)] [SerializeField] private int eliteCardsDrawnPerTurn = 2;
        [Min(0)] [SerializeField] private int eliteCardsPlayedPerTurn = 2;

        [Header("Enemy cards - Boss")]
        [Min(1)] [SerializeField] private int bossDeckSize = 4;
        [Min(0)] [SerializeField] private int bossActionPoints = 5;
        [Min(0)] [SerializeField] private int bossCardsDrawnPerTurn = 2;
        [Min(0)] [SerializeField] private int bossCardsPlayedPerTurn = 2;
        [Min(1)] [SerializeField] private int bossBerserkTurn = 12;

        [Header("Enemy cards - Shared")]
        [Min(1)] [SerializeField] private int enemyHandLimit = 5;
        [Tooltip("Elite/Boss: inspect this many most recently played cards.")]
        [Min(1)] [SerializeField] private int expensiveCardWindowSize = 5;
        [Tooltip("Elite/Boss: maximum number of 2-AP cards within the recent-card window.")]
        [Min(0)] [SerializeField] private int maxTwoCostCardsInWindow = 2;

        [Header("Automatic execution")]
        [Min(0)] [SerializeField] private int executionDamage = 60;

        [Header("Toughness restore - pending final balance")]
        [SerializeField] private ToughnessRestoreMode toughnessRestoreMode = ToughnessRestoreMode.Full;
        [Min(0)] [SerializeField] private int fixedToughnessRestoreAmount = 0;

        public int BaseActionPoints => baseActionPoints;
        public int CardsDrawnPerTurn => cardsDrawnPerTurn;
        public int HandLimit => handLimit;
        public int ExpectedInitialDeckSize => expectedInitialDeckSize;
        public int ManaPerTurn => manaPerTurn;

        public CombatRules CreateRuntimeRules()
        {
            return new CombatRules(
                baseActionPoints,
                cardsDrawnPerTurn,
                handLimit,
                expectedInitialDeckSize,
                executionDamage,
                toughnessRestoreMode,
                fixedToughnessRestoreAmount,
                manaPerTurn,
                cardUpgradeManaCost,
                new EnemyTurnRules(
                    minionDeckSize,
                    minionActionPoints,
                    minionCardsDrawnPerTurn,
                    minionCardsPlayedPerTurn,
                    enemyHandLimit),
                new EnemyTurnRules(
                    eliteDeckSize,
                    eliteActionPoints,
                    eliteCardsDrawnPerTurn,
                    eliteCardsPlayedPerTurn,
                    enemyHandLimit,
                    expensiveCardWindowSize,
                    maxTwoCostCardsInWindow),
                new EnemyTurnRules(
                    bossDeckSize,
                    bossActionPoints,
                    bossCardsDrawnPerTurn,
                    bossCardsPlayedPerTurn,
                    enemyHandLimit,
                    expensiveCardWindowSize,
                    maxTwoCostCardsInWindow,
                    bossBerserkTurn));
        }
    }
}
