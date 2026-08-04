using System;

namespace KiKs.Combat
{
    /// <summary>Battle-scoped mana that refills to its per-turn amount each player turn.</summary>
    public sealed class ManaState
    {
        public int Current { get; private set; }
        public int PerTurn { get; }

        public ManaState(int manaPerTurn)
        {
            if (manaPerTurn <= 0) throw new ArgumentOutOfRangeException(nameof(manaPerTurn));

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

        internal void BeginTurn()
        {
            Current = PerTurn;
        }
    }
}