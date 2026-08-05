using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    /// <summary>
    /// Read-only access point for data loaded from project JSON files.
    /// Runtime systems should query definitions here instead of holding their own copies.
    /// </summary>
    public static class StaticGameRepository
    {
        private static readonly IReadOnlyList<CardSpec> EmptyCards =
            new ReadOnlyCollection<CardSpec>(new List<CardSpec>());
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<CardSpec>> EmptyCategories =
            new ReadOnlyDictionary<string, IReadOnlyList<CardSpec>>(
                new Dictionary<string, IReadOnlyList<CardSpec>>(StringComparer.Ordinal));

        private static CardJsonRepository _cardRepository;
        private static IReadOnlyList<CardSpec> _allCards = EmptyCards;
        private static IReadOnlyList<CardSpec> _playerCards = EmptyCards;
        private static IReadOnlyList<CardSpec> _enemyCards = EmptyCards;
        private static IReadOnlyDictionary<string, IReadOnlyList<CardSpec>> _cardsByCategory = EmptyCategories;

        public static bool HasCards => _cardRepository != null;
        public static CardJsonRepository CardRepository => _cardRepository;
        public static IReadOnlyList<CardSpec> AllCards => _allCards;
        public static IReadOnlyList<CardSpec> PlayerCards => _playerCards;
        public static IReadOnlyList<CardSpec> EnemyCards => _enemyCards;

        public static void SetCardRepository(CardJsonRepository repository)
        {
            _cardRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            RebuildCardIndexes(repository.Cards);
        }

        public static void ClearCardRepository(CardJsonRepository repository)
        {
            if (!ReferenceEquals(_cardRepository, repository))
                return;

            _cardRepository = null;
            _allCards = EmptyCards;
            _playerCards = EmptyCards;
            _enemyCards = EmptyCards;
            _cardsByCategory = EmptyCategories;
        }

        public static bool TryGetCard(string cardId, out CardSpec card)
        {
            card = null;
            return _cardRepository != null && _cardRepository.TryGetCard(cardId, out card);
        }

        public static CardSpec GetRequiredCard(string cardId)
        {
            if (_cardRepository == null)
                throw new InvalidOperationException("Card static repository has not been loaded.");

            return _cardRepository.GetRequiredCard(cardId);
        }

        public static IReadOnlyList<CardSpec> GetCardsByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return EmptyCards;

            return _cardsByCategory.TryGetValue(category, out var cards) ? cards : EmptyCards;
        }

        private static void RebuildCardIndexes(IReadOnlyList<CardSpec> cards)
        {
            var allCards = new List<CardSpec>(cards.Count);
            var playerCards = new List<CardSpec>();
            var enemyCards = new List<CardSpec>();
            var byCategory = new Dictionary<string, List<CardSpec>>(StringComparer.Ordinal);

            foreach (var card in cards)
            {
                if (card == null) continue;

                allCards.Add(card);
                if (card.IsEnemyCard)
                    enemyCards.Add(card);
                else
                    playerCards.Add(card);

                if (!byCategory.TryGetValue(card.Category, out var categoryCards))
                {
                    categoryCards = new List<CardSpec>();
                    byCategory[card.Category] = categoryCards;
                }
                categoryCards.Add(card);
            }

            var readOnlyCategories =
                new Dictionary<string, IReadOnlyList<CardSpec>>(StringComparer.Ordinal);
            foreach (var pair in byCategory)
                readOnlyCategories[pair.Key] = new ReadOnlyCollection<CardSpec>(pair.Value);

            _allCards = new ReadOnlyCollection<CardSpec>(allCards);
            _playerCards = new ReadOnlyCollection<CardSpec>(playerCards);
            _enemyCards = new ReadOnlyCollection<CardSpec>(enemyCards);
            _cardsByCategory = new ReadOnlyDictionary<string, IReadOnlyList<CardSpec>>(readOnlyCategories);
        }
    }
}
