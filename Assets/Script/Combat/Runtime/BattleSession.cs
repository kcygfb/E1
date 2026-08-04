using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    /// <summary>
    /// Cross-scene selection payload. Only card ids are stored; upgrade state starts in battle.
    /// Duplicate ids are allowed because a deck may contain multiple copies.
    /// </summary>
    public static class BattleSession
    {
        private static IReadOnlyList<string> _selectedCardIds =
            new ReadOnlyCollection<string>(new List<string>());
        private static DemoStage? _selectedDemoStage;

        public static IReadOnlyList<string> SelectedCardIds => _selectedCardIds;
        public static bool HasSelectedDeck => _selectedCardIds.Count > 0;
        public static bool HasSelectedDemoStage => _selectedDemoStage.HasValue;
        public static DemoStage SelectedDemoStage => _selectedDemoStage ?? DemoStage.Completed;

        public static void SetSelectedDeck(IEnumerable<string> cardIds)
        {
            if (cardIds == null) throw new ArgumentNullException(nameof(cardIds));
            var copy = new List<string>();
            foreach (var id in cardIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Selected deck contains an empty card id.", nameof(cardIds));
                copy.Add(id);
            }

            if (copy.Count == 0) throw new ArgumentException("Selected deck cannot be empty.", nameof(cardIds));
            _selectedCardIds = new ReadOnlyCollection<string>(copy);
        }

        public static void ClearSelectedDeck()
        {
            _selectedCardIds = new ReadOnlyCollection<string>(new List<string>());
        }

        public static void SetSelectedDemoStage(DemoStage stage)
        {
            if (!DemoFlowState.IsStageAvailable(stage))
                throw new InvalidOperationException(
                    $"Demo stage {stage} is not available. Current stage is {DemoFlowState.CurrentStage}.");

            _selectedDemoStage = stage;
        }

        public static void ClearSelectedDemoStage()
        {
            _selectedDemoStage = null;
        }

        // ─── Coffee slots ───

        private static IReadOnlyList<string> _selectedCoffeeIds =
            new ReadOnlyCollection<string>(new List<string>());

        public static IReadOnlyList<string> SelectedCoffeeIds => _selectedCoffeeIds;
        public static bool HasSelectedCoffees => _selectedCoffeeIds.Count > 0;

        public static void SetSelectedCoffees(IEnumerable<string> coffeeIds)
        {
            if (coffeeIds == null) throw new ArgumentNullException(nameof(coffeeIds));
            var copy = new List<string>();
            foreach (var id in coffeeIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                copy.Add(id);
            }
            _selectedCoffeeIds = new ReadOnlyCollection<string>(copy);
        }

        public static void ClearSelectedCoffees()
        {
            _selectedCoffeeIds = new ReadOnlyCollection<string>(new List<string>());
        }
    }
}
