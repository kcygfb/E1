using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Session-lifetime state produced while playing. It stores ids and counts only.
    /// </summary>
    public static class RuntimeGameRepository
    {
        public const string GoldResourceId = "gold";

        private static IReadOnlyList<string> _selectedCardIds =
            new ReadOnlyCollection<string>(new List<string>());
        private static IReadOnlyList<string> _selectedCoffeeIds =
            new ReadOnlyCollection<string>(new List<string>());
        private static readonly Dictionary<string, int> OwnedCardCopies = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> FallbackResources = new(StringComparer.Ordinal);
        private static readonly List<string> LastBattleRewardCardIds = new();
        private static DemoStage? _selectedDemoStage;

        public static IReadOnlyList<string> SelectedCardIds => _selectedCardIds;
        public static bool HasSelectedDeck => _selectedCardIds.Count > 0;
        public static bool HasSelectedDemoStage => _selectedDemoStage.HasValue;
        public static DemoStage SelectedDemoStage => _selectedDemoStage ?? DemoStage.Completed;
        public static IReadOnlyList<string> SelectedCoffeeIds => _selectedCoffeeIds;
        public static bool HasSelectedCoffees => _selectedCoffeeIds.Count > 0;
        public static int Gold => GetResourceAmount(GoldResourceId);

        public static IReadOnlyList<string> LastBattleRewardCards =>
            new ReadOnlyCollection<string>(LastBattleRewardCardIds);

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

        public static void AddOwnedCard(string cardId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(cardId) || amount <= 0) return;
            OwnedCardCopies.TryGetValue(cardId, out var current);
            OwnedCardCopies[cardId] = current + amount;
        }

        public static int GetOwnedCardCopies(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) &&
                   OwnedCardCopies.TryGetValue(cardId, out var count)
                ? count
                : 0;
        }

        public static IReadOnlyDictionary<string, int> GetOwnedCardsSnapshot()
        {
            return new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(OwnedCardCopies, StringComparer.Ordinal));
        }

        public static void BeginBattleRewards()
        {
            LastBattleRewardCardIds.Clear();
        }

        public static void AddBattleRewardCard(string cardId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(cardId) || amount <= 0) return;
            for (var i = 0; i < amount; i++)
                LastBattleRewardCardIds.Add(cardId);
            AddOwnedCard(cardId, amount);
        }

        public static void AddGold(int amount)
        {
            AddResource(GoldResourceId, amount);
        }

        public static bool SpendGold(int amount)
        {
            return SpendResource(GoldResourceId, amount);
        }

        public static void AddResource(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount == 0) return;
            if (InventoryBridge.TryAdd(resourceId, amount)) return;

            FallbackResources.TryGetValue(resourceId, out var current);
            FallbackResources[resourceId] = current + amount;
        }

        public static bool SpendResource(string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0) return false;
            if (InventoryBridge.TrySpend(resourceId, amount, out var spent)) return spent;

            if (!FallbackResources.TryGetValue(resourceId, out var current) || current < amount)
                return false;

            FallbackResources[resourceId] = current - amount;
            return true;
        }

        public static int GetResourceAmount(string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return 0;
            if (InventoryBridge.TryGetAmount(resourceId, out var amount)) return amount;
            return FallbackResources.TryGetValue(resourceId, out amount) ? amount : 0;
        }

        public static void ResetRunState()
        {
            ClearSelectedDeck();
            ClearSelectedDemoStage();
            ClearSelectedCoffees();
            OwnedCardCopies.Clear();
            LastBattleRewardCardIds.Clear();
            FallbackResources.Clear();
        }

        private static class InventoryBridge
        {
            private static object ResolveInventoryInstance(bool createIfMissing)
            {
                var inventoryType = Type.GetType("InventorySystem, Assembly-CSharp") ??
                                    Type.GetType("KiKs.Core.InventorySystem, Assembly-CSharp");
                if (inventoryType == null) return null;

                var instanceProperty = inventoryType.GetProperty(
                    "Instance",
                    BindingFlags.Public | BindingFlags.Static);
                var instance = instanceProperty?.GetValue(null);
                if (instance != null || !createIfMissing || !typeof(MonoBehaviour).IsAssignableFrom(inventoryType))
                    return instance;

                var inventoryObject = new GameObject("InventorySystem");
                return inventoryObject.AddComponent(inventoryType);
            }

            public static bool TryAdd(string resourceId, int amount)
            {
                var instance = ResolveInventoryInstance(createIfMissing: true);
                var method = instance?.GetType().GetMethod(
                    "Add",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);
                if (instance == null || method == null) return false;
                method.Invoke(instance, new object[] { resourceId, amount });
                return true;
            }

            public static bool TrySpend(string resourceId, int amount, out bool spent)
            {
                spent = false;
                var instance = ResolveInventoryInstance(createIfMissing: true);
                var method = instance?.GetType().GetMethod(
                    "Spend",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(int) },
                    null);
                if (instance == null || method == null) return false;
                spent = (bool)method.Invoke(instance, new object[] { resourceId, amount });
                return true;
            }

            public static bool TryGetAmount(string resourceId, out int amount)
            {
                amount = 0;
                var instance = ResolveInventoryInstance(createIfMissing: false);
                var method = instance?.GetType().GetMethod(
                    "GetAmount",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(string) },
                    null);
                if (instance == null || method == null) return false;
                amount = (int)method.Invoke(instance, new object[] { resourceId });
                return true;
            }
        }
    }
}
