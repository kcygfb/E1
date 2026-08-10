using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    [Serializable]
    public sealed class LoopProgressionDefinition
    {
        public int schemaVersion = 1;
        public int finalDay = 4;
        public string[] initiallyHiddenCardIds = Array.Empty<string>();
        public string[] initiallyHiddenRecipeIds = Array.Empty<string>();
        public TreasureOfferDefinition[] treasureOffers = Array.Empty<TreasureOfferDefinition>();
        public EnemyLoopRewardDefinition[] enemyRewards = Array.Empty<EnemyLoopRewardDefinition>();
    }

    [Serializable]
    public sealed class EnemyLoopRewardDefinition
    {
        public string enemyId;
        public LoopRewardStageDefinition[] stages = Array.Empty<LoopRewardStageDefinition>();
    }

    [Serializable]
    public sealed class LoopRewardStageDefinition
    {
        public int victoryNumber;
        public LoopRewardBundleDefinition rewards = new();
    }

    [Serializable]
    public sealed class LoopRewardBundleDefinition
    {
        public int gold;
        public LoopResourceRewardDefinition[] resources = Array.Empty<LoopResourceRewardDefinition>();
        public string[] cardIds = Array.Empty<string>();
        public string[] recipeIds = Array.Empty<string>();

        public bool HasAnyReward =>
            gold > 0 || (resources?.Length ?? 0) > 0 || (cardIds?.Length ?? 0) > 0 ||
            (recipeIds?.Length ?? 0) > 0;
    }

    [Serializable]
    public sealed class LoopResourceRewardDefinition
    {
        public string resourceId;
        public int amount;
    }

    public sealed class ResourceGrantResult
    {
        public string ResourceId { get; }
        public int Amount { get; }

        public ResourceGrantResult(string resourceId, int amount)
        {
            ResourceId = resourceId;
            Amount = amount;
        }
    }

    public sealed class RewardGrantResult
    {
        private static readonly IReadOnlyList<string> EmptyStrings =
            new ReadOnlyCollection<string>(new List<string>());
        private static readonly IReadOnlyList<ResourceGrantResult> EmptyResources =
            new ReadOnlyCollection<ResourceGrantResult>(new List<ResourceGrantResult>());

        public bool Applied { get; }
        public bool DuplicateSettlement { get; }
        public int GoldGranted { get; }
        public IReadOnlyList<ResourceGrantResult> ResourcesGranted { get; }
        public IReadOnlyList<string> NewCardIds { get; }
        public IReadOnlyList<string> ExistingCardIds { get; }
        public IReadOnlyList<string> NewRecipeIds { get; }
        public IReadOnlyList<string> ExistingRecipeIds { get; }

        public RewardGrantResult(
            bool applied,
            bool duplicateSettlement,
            int goldGranted,
            IReadOnlyList<ResourceGrantResult> resourcesGranted,
            IReadOnlyList<string> newCardIds,
            IReadOnlyList<string> existingCardIds,
            IReadOnlyList<string> newRecipeIds,
            IReadOnlyList<string> existingRecipeIds)
        {
            Applied = applied;
            DuplicateSettlement = duplicateSettlement;
            GoldGranted = goldGranted;
            ResourcesGranted = resourcesGranted ?? EmptyResources;
            NewCardIds = newCardIds ?? EmptyStrings;
            ExistingCardIds = existingCardIds ?? EmptyStrings;
            NewRecipeIds = newRecipeIds ?? EmptyStrings;
            ExistingRecipeIds = existingRecipeIds ?? EmptyStrings;
        }

        public static RewardGrantResult Duplicate() =>
            new(false, true, 0, EmptyResources, EmptyStrings, EmptyStrings, EmptyStrings, EmptyStrings);
    }

    public sealed class AreaCompletionResult
    {
        public bool Completed { get; }
        public bool DayAdvanced { get; }
        public int CurrentDay { get; }
        public string NextSceneName { get; }

        public AreaCompletionResult(bool completed, bool dayAdvanced, int currentDay, string nextSceneName)
        {
            Completed = completed;
            DayAdvanced = dayAdvanced;
            CurrentDay = currentDay;
            NextSceneName = nextSceneName ?? string.Empty;
        }
    }
}
