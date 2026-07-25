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
        [Min(1)] [SerializeField] private int expectedInitialDeckSize = 5;

        [Header("Mana and in-battle upgrades")]
        [Min(0)] [SerializeField] private int startingMana = 3;
        [Min(0)] [SerializeField] private int maximumMana = 3;
        [Min(0)] [SerializeField] private int maximumManaSpendPerTurn = 1;
        [Min(0)] [SerializeField] private int cardUpgradeManaCost = 1;
        [Min(0)] [SerializeField] private int magicCardsPerTurn = 1;

        [Header("Automatic ultimate")]
        [Min(1)] [SerializeField] private int ultimateManaThreshold = 3;
        [Min(0)] [SerializeField] private int ultimateDamage = 0;
        [Min(0)] [SerializeField] private int ultimateStunTurns = 1;

        [Header("Execution values - pending final balance")]
        [Min(0)] [SerializeField] private int eliteExecutionDamage = 40;
        [Min(0)] [SerializeField] private int eliteStunTurns = 1;
        [Min(0)] [SerializeField] private int bossExecutionDamage = 40;
        [Min(0)] [SerializeField] private int bossStunTurns = 1;

        [Header("Toughness restore - pending final balance")]
        [SerializeField] private ToughnessRestoreMode toughnessRestoreMode = ToughnessRestoreMode.Full;
        [Min(0)] [SerializeField] private int fixedToughnessRestoreAmount = 0;

        public int BaseActionPoints => baseActionPoints;
        public int CardsDrawnPerTurn => cardsDrawnPerTurn;
        public int HandLimit => handLimit;
        public int ExpectedInitialDeckSize => expectedInitialDeckSize;

        public CombatRules CreateRuntimeRules()
        {
            return new CombatRules(
                baseActionPoints,
                cardsDrawnPerTurn,
                handLimit,
                expectedInitialDeckSize,
                eliteExecutionDamage,
                eliteStunTurns,
                bossExecutionDamage,
                bossStunTurns,
                toughnessRestoreMode,
                fixedToughnessRestoreAmount,
                startingMana,
                maximumMana,
                maximumManaSpendPerTurn,
                cardUpgradeManaCost,
                magicCardsPerTurn,
                ultimateManaThreshold,
                ultimateDamage,
                ultimateStunTurns);
        }

        private void OnValidate()
        {
            if (startingMana > maximumMana) startingMana = maximumMana;
        }
    }
}
