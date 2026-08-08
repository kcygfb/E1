using System;

namespace KiKs.Combat
{
    /// <summary>Battle-scoped mana that refills to its per-turn amount each player turn.</summary>
    public sealed class ManaState
    {
        public int Current { get; private set; }
        public int BasePerTurn { get; }
        public int BonusPerTurn { get; private set; }
        public int PerTurn { get; private set; }

        /// <summary>Mana cards (played or activated) spent during the current player turn. Reset each turn.</summary>
        public int ManaCardsSpentThisTurn { get; private set; }

        public ManaState(int manaPerTurn)
        {
            if (manaPerTurn <= 0) throw new ArgumentOutOfRangeException(nameof(manaPerTurn));

            BasePerTurn = manaPerTurn;
            PerTurn = manaPerTurn;
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && Current >= amount;
        }

        internal bool TrySpend(int amount)
        {
            if (!CanSpend(amount)) return false;
            Current -= amount;
            return true;
        }

        internal void BeginTurn(int bonusPerTurn = 0)
        {
            if (bonusPerTurn < 0) throw new ArgumentOutOfRangeException(nameof(bonusPerTurn));
            BonusPerTurn = bonusPerTurn;
            PerTurn = BasePerTurn + bonusPerTurn;
            Current = PerTurn;
            ManaCardsSpentThisTurn = 0;
        }

        /// <summary>Record that one mana card was spent this turn (for mana-burst style effects).</summary>
        internal void RecordManaCardSpent()
        {
            ManaCardsSpentThisTurn++;
        }
    }
}