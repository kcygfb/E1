using System;

namespace KiKs.Combat
{
    /// <summary>Battle-scoped mana and automatic-ultimate progress.</summary>
    public sealed class ManaState
    {
        public int Current { get; private set; }
        public int Maximum { get; }
        public int SpentThisTurn { get; private set; }
        public int SpentTowardUltimate { get; private set; }
        public int MagicCardsPlayedThisTurn { get; private set; }

        public ManaState(int startingMana, int maximumMana)
        {
            if (maximumMana < 0) throw new ArgumentOutOfRangeException(nameof(maximumMana));
            if (startingMana < 0 || startingMana > maximumMana)
                throw new ArgumentOutOfRangeException(nameof(startingMana));

            Current = startingMana;
            Maximum = maximumMana;
        }

        public bool CanSpend(int amount, int maximumSpendPerTurn)
        {
            return amount >= 0 && Current >= amount &&
                   SpentThisTurn + amount <= maximumSpendPerTurn;
        }

        internal bool TrySpend(int amount, int maximumSpendPerTurn)
        {
            if (!CanSpend(amount, maximumSpendPerTurn)) return false;
            Current -= amount;
            SpentThisTurn += amount;
            SpentTowardUltimate += amount;
            return true;
        }

        internal void RegisterMagicCardPlayed() { MagicCardsPlayedThisTurn++; }

        internal void BeginTurn()
        {
            SpentThisTurn = 0;
            MagicCardsPlayedThisTurn = 0;
        }

        internal int RestoreToMaximum()
        {
            var previous = Current;
            Current = Maximum;
            return Current - previous;
        }

        internal bool ConsumeUltimateThreshold(int threshold)
        {
            if (threshold <= 0 || SpentTowardUltimate < threshold) return false;
            SpentTowardUltimate -= threshold;
            return true;
        }
    }
}
