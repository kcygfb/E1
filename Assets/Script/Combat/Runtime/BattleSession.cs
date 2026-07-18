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

        public static IReadOnlyList<string> SelectedCardIds => _selectedCardIds;
        public static bool HasSelectedDeck => _selectedCardIds.Count > 0;

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
    }
}
