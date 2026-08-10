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
        private static readonly HashSet<string> _craftedCoffeeIds = new(StringComparer.Ordinal);
        private static readonly HashSet<string> UnlockedCardIds = new(StringComparer.Ordinal);
        private static readonly HashSet<string> UnlockedRecipeIds = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> EnemyVictoryCounts = new(StringComparer.Ordinal);
        private static readonly HashSet<string> ProcessedSettlementIds = new(StringComparer.Ordinal);
        private static int currentDay = 1;

        public static event Action<int> DayChanged;
        public static event Action<int> FinalCafeCompleted;
        private static DemoStage? _selectedDemoStage;
        private static int? _selectedEncounterIndex;

        public static IReadOnlyList<string> SelectedCardIds => _selectedCardIds;
        public static bool HasSelectedDeck => _selectedCardIds.Count > 0;
        public static bool HasSelectedDemoStage => _selectedDemoStage.HasValue;
        public static DemoStage SelectedDemoStage => _selectedDemoStage ?? DemoStage.Completed;

        /// <summary>当前选中战斗点的敌人槽位索引（0=狗, 1=小女孩, 2=大眼）。</summary>
        public static bool HasSelectedEncounterIndex => _selectedEncounterIndex.HasValue;
        public static int SelectedEncounterIndex => _selectedEncounterIndex ?? 0;
        public static IReadOnlyList<string> SelectedCoffeeIds => _selectedCoffeeIds;
        public static bool HasSelectedCoffees => _selectedCoffeeIds.Count > 0;
        public static IReadOnlyCollection<string> CraftedCoffeeIds => _craftedCoffeeIds;
        public static bool HasCraftedCoffees => _craftedCoffeeIds.Count > 0;
        public static int Gold => GetResourceAmount(GoldResourceId);
        public static int CurrentDay => currentDay;
        public static int CurrentDayExplorationCount => DailyAreaMapState.CompletedExplorationCount;
        public static bool IsFinalCafeDay => currentDay >= LoopProgressionRepository.FinalDay;
        public static IReadOnlyCollection<string> UnlockedRecipes => UnlockedRecipeIds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            ResetRunState();
        }
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
            _selectedEncounterIndex = null;
        }

        public static void SetSelectedEncounterIndex(int index)
        {
            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException(nameof(index), "Encounter slot must be 0..2.");

            _selectedEncounterIndex = index;
        }

        public static void ClearSelectedEncounterIndex()
        {
            _selectedEncounterIndex = null;
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

        /// <summary>记录当天制作过的咖啡种类（去重）。跨场景传递到 PreBattle 供选择。</summary>
        public static void AddCraftedCoffee(string coffeeId)
        {
            if (string.IsNullOrWhiteSpace(coffeeId)) return;
            _craftedCoffeeIds.Add(coffeeId);
        }

        public static void ClearCraftedCoffees()
        {
            _craftedCoffeeIds.Clear();
        }

        public static bool IsCardUnlocked(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return false;
            return !LoopProgressionRepository.IsInitiallyHiddenCard(cardId) ||
                   UnlockedCardIds.Contains(cardId);
        }

        public static bool UnlockCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return false;
            return UnlockedCardIds.Add(cardId);
        }

        public static bool IsRecipeUnlocked(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            return !LoopProgressionRepository.IsInitiallyHiddenRecipe(recipeId) ||
                   UnlockedRecipeIds.Contains(recipeId);
        }

        public static bool UnlockRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            return UnlockedRecipeIds.Add(recipeId);
        }

        /// <summary>
        /// Records a valid recipe discovered through hands-on coffee crafting.
        /// Order acceptance is intentionally outside this boundary.
        /// </summary>
        public static bool DiscoverRecipeFromCrafting(string recipeId)
        {
            if (IsRecipeUnlocked(recipeId)) return false;
            return UnlockRecipe(recipeId);
        }

        public static bool LockRecipe(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && UnlockedRecipeIds.Remove(recipeId);
        }

        public static int GetEnemyVictoryCount(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) &&
                   EnemyVictoryCounts.TryGetValue(enemyId, out var count)
                ? count
                : 0;
        }

        public static bool RecordEnemyVictory(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return false;
            EnemyVictoryCounts[enemyId] = GetEnemyVictoryCount(enemyId) + 1;
            return true;
        }

        public static void AddOwnedCard(string cardId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(cardId) || amount <= 0) return;
            UnlockCard(cardId);
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
            UnlockCard(cardId);
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

        public static bool WouldGrantAnyNewUnlock(LoopRewardBundleDefinition rewards)
        {
            if (rewards == null) return false;
            if (rewards.gold > 0 || (rewards.resources != null && rewards.resources.Length > 0))
                return true;
            if (rewards.cardIds != null)
                foreach (var cardId in rewards.cardIds)
                    if (!IsCardUnlocked(cardId)) return true;
            if (rewards.recipeIds != null)
                foreach (var recipeId in rewards.recipeIds)
                    if (!IsRecipeUnlocked(recipeId)) return true;
            return false;
        }

        public static bool HasProcessedSettlement(string settlementId)
        {
            return !string.IsNullOrWhiteSpace(settlementId) && ProcessedSettlementIds.Contains(settlementId);
        }

        public static RewardGrantResult ApplyRewardBundle(
            string settlementId,
            LoopRewardBundleDefinition rewards)
        {
            if (string.IsNullOrWhiteSpace(settlementId))
                throw new ArgumentException("Settlement id is required.", nameof(settlementId));
            if (rewards == null) throw new ArgumentNullException(nameof(rewards));
            if (ProcessedSettlementIds.Contains(settlementId))
                return RewardGrantResult.Duplicate();

            ValidateRewardBundle(rewards);
            var resourceResults = new List<ResourceGrantResult>();
            var newCards = new List<string>();
            var existingCards = new List<string>();
            var newRecipes = new List<string>();
            var existingRecipes = new List<string>();

            if (rewards.gold > 0) AddGold(rewards.gold);
            foreach (var resource in rewards.resources ?? Array.Empty<LoopResourceRewardDefinition>())
            {
                AddResource(resource.resourceId, resource.amount);
                resourceResults.Add(new ResourceGrantResult(resource.resourceId, resource.amount));
            }

            foreach (var cardId in rewards.cardIds ?? Array.Empty<string>())
            {
                if (IsCardUnlocked(cardId))
                {
                    existingCards.Add(cardId);
                    continue;
                }

                UnlockCard(cardId);
                AddOwnedCard(cardId);
                LastBattleRewardCardIds.Add(cardId);
                newCards.Add(cardId);
            }

            foreach (var recipeId in rewards.recipeIds ?? Array.Empty<string>())
            {
                if (IsRecipeUnlocked(recipeId)) existingRecipes.Add(recipeId);
                else
                {
                    UnlockRecipe(recipeId);
                    newRecipes.Add(recipeId);
                }
            }

            ProcessedSettlementIds.Add(settlementId);
            return new RewardGrantResult(
                true,
                false,
                rewards.gold,
                new ReadOnlyCollection<ResourceGrantResult>(resourceResults),
                new ReadOnlyCollection<string>(newCards),
                new ReadOnlyCollection<string>(existingCards),
                new ReadOnlyCollection<string>(newRecipes),
                new ReadOnlyCollection<string>(existingRecipes));
        }

        public static RewardGrantResult ApplyEnemyVictoryReward(string enemyId, string settlementId)
        {
            if (HasProcessedSettlement(settlementId)) return RewardGrantResult.Duplicate();
            var victoryNumber = GetEnemyVictoryCount(enemyId) + 1;
            if (!LoopProgressionRepository.TryGetEnemyReward(enemyId, victoryNumber, out var rewards))
                throw new InvalidOperationException(
                    $"No loop reward is configured for enemy '{enemyId}' victory {victoryNumber}.");

            BeginBattleRewards();
            var result = ApplyRewardBundle(settlementId, rewards);
            if (result.Applied) RecordEnemyVictory(enemyId);
            return result;
        }

        public static bool TryPurchaseReward(
            string settlementId,
            int price,
            LoopRewardBundleDefinition rewards,
            out RewardGrantResult result)
        {
            result = null;
            if (price <= 0 || rewards == null || Gold < price ||
                ProcessedSettlementIds.Contains(settlementId))
                return false;
            if (!WouldGrantAnyNewUnlock(rewards)) return false;
            if (!SpendGold(price)) return false;

            try
            {
                result = ApplyRewardBundle(settlementId, rewards);
                return result.Applied;
            }
            catch
            {
                AddGold(price);
                throw;
            }
        }

        public static bool AdvanceDay()
        {
            if (currentDay >= LoopProgressionRepository.FinalDay) return false;
            currentDay++;
            ClearSelectedDemoStage();
            ClearSelectedEncounterIndex();
            // Keep selected coffees across days, exactly like the deck: the player's
            // loadout (deck + coffee) is a persistent configuration for the run.
            EventSelectionState.ClearCurrent();
            DailyAreaMapState.Reset();
            // Do NOT reset player health here — HP carries across days within a run.
            // Defeat already halves HP via RestoreAfterDefeat; full heal only on new run.
            DayChanged?.Invoke(currentDay);
            return true;
        }

        public static void NotifyFinalCafeCompleted()
        {
            if (!IsFinalCafeDay) return;
            FinalCafeCompleted?.Invoke(currentDay);
        }

        public static AreaCompletionResult CompleteSelectedArea(bool defeated)
        {
            if (!DailyAreaMapState.HasSelectedPoint)
                return new AreaCompletionResult(false, false, currentDay, "PreBattle");

            if (defeated)
                return ResolveSelectedAreaDefeat();

            DailyAreaMapState.CompleteSelectedPoint();
            ClearSelectedDemoStage();
            ClearSelectedEncounterIndex();
            if (DailyAreaMapState.CompletedExplorationCount < DailyAreaMapState.MaxExplorations)
                return new AreaCompletionResult(true, false, currentDay, "PreBattle");

            var advanced = AdvanceDay();
            return new AreaCompletionResult(true, advanced, currentDay, "Cafe");
        }

        public static AreaCompletionResult ResolveSelectedAreaDefeat()
        {
            if (!DailyAreaMapState.HasSelectedPoint)
                return new AreaCompletionResult(false, false, currentDay, "PreBattle");

            PlayerGlobalStats.RestoreAfterDefeat();
            DailyAreaMapState.CancelSelectedPoint();
            ClearSelectedDemoStage();
            ClearSelectedEncounterIndex();
            return new AreaCompletionResult(false, false, currentDay, "PreBattle");
        }

        private static void ValidateRewardBundle(LoopRewardBundleDefinition rewards)
        {
            if (rewards.gold < 0) throw new InvalidOperationException("Reward gold cannot be negative.");
            foreach (var resource in rewards.resources ?? Array.Empty<LoopResourceRewardDefinition>())
                if (resource == null || string.IsNullOrWhiteSpace(resource.resourceId) || resource.amount <= 0)
                    throw new InvalidOperationException("Reward bundle contains an invalid resource.");
            foreach (var cardId in rewards.cardIds ?? Array.Empty<string>())
                if (string.IsNullOrWhiteSpace(cardId) ||
                    (StaticGameRepository.HasCards && !StaticGameRepository.TryGetCard(cardId, out _)))
                    throw new InvalidOperationException($"Reward bundle contains unknown card '{cardId}'.");
            foreach (var recipeId in rewards.recipeIds ?? Array.Empty<string>())
                if (string.IsNullOrWhiteSpace(recipeId))
                    throw new InvalidOperationException("Reward bundle contains an empty recipe id.");
        }

        public static void ResetRunState()
        {
            ClearSelectedDeck();
            ClearSelectedDemoStage();
            ClearSelectedCoffees();
            ClearCraftedCoffees();
            EventSelectionState.Reset();
            OwnedCardCopies.Clear();
            LastBattleRewardCardIds.Clear();
            FallbackResources.Clear();
            UnlockedCardIds.Clear();
            UnlockedRecipeIds.Clear();
            EnemyVictoryCounts.Clear();
            ProcessedSettlementIds.Clear();
            currentDay = 1;
            DailyAreaMapState.Reset();
            PlayerGlobalStats.ResetToFull();
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
