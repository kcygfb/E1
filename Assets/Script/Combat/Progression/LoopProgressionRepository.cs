using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KiKs.Combat
{
    public static class LoopProgressionRepository
    {
        public const string RelativePath = "LoopData/loop_progression.json";
        public const int SupportedSchemaVersion = 1;

        private static LoopProgressionDefinition definition;
        private static HashSet<string> hiddenCardIds;
        private static HashSet<string> hiddenRecipeIds;
        private static Dictionary<int, TreasureOfferDefinition> treasureOffersByPrice;
        private static Dictionary<string, EnemyLoopRewardDefinition> enemyRewardsById;

        public static LoopProgressionDefinition Definition
        {
            get
            {
                EnsureLoaded();
                return definition;
            }
        }

        public static int FinalDay => Definition.finalDay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            definition = null;
            hiddenCardIds = null;
            hiddenRecipeIds = null;
            treasureOffersByPrice = null;
            enemyRewardsById = null;
        }

        public static bool IsInitiallyHiddenCard(string cardId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(cardId) && hiddenCardIds.Contains(cardId);
        }

        public static bool IsInitiallyHiddenRecipe(string recipeId)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(recipeId) && hiddenRecipeIds.Contains(recipeId);
        }

        public static bool TryGetTreasureOffer(int price, out TreasureOfferDefinition offer)
        {
            EnsureLoaded();
            return treasureOffersByPrice.TryGetValue(price, out offer);
        }

        public static bool TryGetEnemyReward(
            string enemyId,
            int victoryNumber,
            out LoopRewardBundleDefinition rewards)
        {
            EnsureLoaded();
            rewards = null;
            if (string.IsNullOrWhiteSpace(enemyId) || victoryNumber <= 0 ||
                !enemyRewardsById.TryGetValue(enemyId, out var enemy))
                return false;

            foreach (var stage in enemy.stages)
            {
                if (stage != null && stage.victoryNumber == victoryNumber)
                {
                    rewards = stage.rewards;
                    return rewards != null;
                }
            }

            return false;
        }

        public static LoopProgressionDefinition LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Loop progression JSON is empty.");

            LoopProgressionDefinition loaded;
            try
            {
                loaded = JsonUtility.FromJson<LoopProgressionDefinition>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidDataException("Cannot parse loop progression JSON.", exception);
            }

            Validate(loaded);
            return loaded;
        }

        public static void Validate(LoopProgressionDefinition value)
        {
            if (value == null) throw new InvalidDataException("Loop progression root is null.");
            if (value.schemaVersion != SupportedSchemaVersion)
                throw new InvalidDataException(
                    $"Unsupported loop progression schemaVersion {value.schemaVersion}.");
            if (value.finalDay != 4)
                throw new InvalidDataException("The current story loop must end on day 4.");

            ValidateUniqueIds(value.initiallyHiddenCardIds, "initiallyHiddenCardIds");
            ValidateUniqueIds(value.initiallyHiddenRecipeIds, "initiallyHiddenRecipeIds");

            if (value.treasureOffers == null || value.treasureOffers.Length != 4)
                throw new InvalidDataException("Exactly four treasure offers are required.");
            var offerIds = new HashSet<string>(StringComparer.Ordinal);
            var prices = new HashSet<int>();
            foreach (var offer in value.treasureOffers)
            {
                if (offer == null || string.IsNullOrWhiteSpace(offer.id) ||
                    !offerIds.Add(offer.id) || offer.price <= 0 || !prices.Add(offer.price) ||
                    string.IsNullOrWhiteSpace(offer.imagePath))
                    throw new InvalidDataException("Treasure offers need unique ids/prices and valid images.");
                ValidateRewardBundle(offer.rewards, $"treasure offer '{offer.id}'");
            }

            if (!prices.SetEquals(new[] { 50, 100, 200, 400 }))
                throw new InvalidDataException("Treasure prices must be exactly 50, 100, 200 and 400.");

            if (value.enemyRewards == null || value.enemyRewards.Length != 3)
                throw new InvalidDataException("Exactly three enemy reward definitions are required.");
            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var enemy in value.enemyRewards)
            {
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId) || !enemyIds.Add(enemy.enemyId))
                    throw new InvalidDataException("Enemy reward ids must be non-empty and unique.");
                if (enemy.stages == null || enemy.stages.Length != 3)
                    throw new InvalidDataException($"Enemy '{enemy.enemyId}' needs three reward stages.");
                var stageNumbers = new HashSet<int>();
                foreach (var stage in enemy.stages)
                {
                    if (stage == null || !stageNumbers.Add(stage.victoryNumber))
                        throw new InvalidDataException($"Enemy '{enemy.enemyId}' has an invalid stage.");
                    ValidateRewardBundle(stage.rewards, $"enemy '{enemy.enemyId}' stage {stage.victoryNumber}");
                }
                if (!stageNumbers.SetEquals(new[] { 1, 2, 3 }))
                    throw new InvalidDataException($"Enemy '{enemy.enemyId}' stages must be 1, 2 and 3.");
            }
            if (!enemyIds.SetEquals(new[] { "demo_ghost", "demo_little_girl", "demo_big_eye" }))
                throw new InvalidDataException("Enemy reward ids must be ghost, little girl and big eye.");
        }

        public static void ValidateReferences(
            LoopProgressionDefinition value,
            IEnumerable<string> knownCardIds,
            IEnumerable<string> knownRecipeIds,
            IEnumerable<string> knownResourceIds)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            ValidateKnownIds(EnumerateReferencedCardIds(value), knownCardIds, "card");
            ValidateKnownIds(EnumerateReferencedRecipeIds(value), knownRecipeIds, "recipe");
            ValidateKnownIds(EnumerateReferencedResourceIds(value), knownResourceIds, "resource");
        }

        private static void ValidateStreamingAssetReferences(LoopProgressionDefinition value)
        {
            var resourcesPath = Path.Combine(Application.streamingAssetsPath, "Resources", "resources.json");
            if (!File.Exists(resourcesPath))
                throw new FileNotFoundException("Resource catalog was not found.", resourcesPath);
            var resourceCatalog = JsonUtility.FromJson<ResourceCatalogJson>(File.ReadAllText(resourcesPath));
            var knownResources = (resourceCatalog?.resources ?? Array.Empty<ResourceIdJson>())
                .Where(item => item != null)
                .Select(item => item.id);

            var coffeeDirectory = Path.Combine(Application.streamingAssetsPath, "CoffeeData");
            if (!Directory.Exists(coffeeDirectory))
                throw new DirectoryNotFoundException("CoffeeData directory was not found: " + coffeeDirectory);
            var knownRecipes = Directory.GetFiles(coffeeDirectory, "*.json")
                .Select(file => JsonUtility.FromJson<RecipeIdJson>(File.ReadAllText(file)))
                .Where(item => item != null)
                .Select(item => item.coffeeId);

            ValidateReferences(value, null, knownRecipes, knownResources);
        }

        private static void ValidateKnownIds(
            IEnumerable<string> referencedIds,
            IEnumerable<string> knownIds,
            string kind)
        {
            if (knownIds == null) return;
            var known = new HashSet<string>(
                knownIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.Ordinal);
            foreach (var id in referencedIds.Distinct(StringComparer.Ordinal))
                if (!known.Contains(id))
                    throw new InvalidDataException($"Loop progression references unknown {kind} '{id}'.");
        }
        public static void ValidateLoadedCardReferences()
        {
            EnsureLoaded();
            if (!StaticGameRepository.HasCards) return;

            foreach (var cardId in EnumerateReferencedCardIds(definition))
                if (!StaticGameRepository.TryGetCard(cardId, out _))
                    throw new InvalidDataException($"Loop progression references unknown card '{cardId}'.");
        }

        private static void EnsureLoaded()
        {
            if (definition != null) return;
            var path = Path.Combine(Application.streamingAssetsPath, RelativePath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Loop progression configuration was not found.", path);

            definition = LoadFromJson(File.ReadAllText(path));
            ValidateStreamingAssetReferences(definition);
            hiddenCardIds = new HashSet<string>(definition.initiallyHiddenCardIds, StringComparer.Ordinal);
            hiddenRecipeIds = new HashSet<string>(definition.initiallyHiddenRecipeIds, StringComparer.Ordinal);
            treasureOffersByPrice = definition.treasureOffers.ToDictionary(item => item.price);
            enemyRewardsById = definition.enemyRewards.ToDictionary(item => item.enemyId, StringComparer.Ordinal);
        }

        private static void ValidateUniqueIds(IEnumerable<string> ids, string field)
        {
            if (ids == null) throw new InvalidDataException(field + " is null.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    throw new InvalidDataException(field + " contains an empty or duplicate id.");
        }

        private static void ValidateRewardBundle(LoopRewardBundleDefinition rewards, string context)
        {
            if (rewards == null || !rewards.HasAnyReward)
                throw new InvalidDataException(context + " has no rewards.");
            if (rewards.gold < 0) throw new InvalidDataException(context + " has negative gold.");
            ValidateUniqueIds(rewards.cardIds ?? Array.Empty<string>(), context + " cardIds");
            ValidateUniqueIds(rewards.recipeIds ?? Array.Empty<string>(), context + " recipeIds");
            var resourceIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resource in rewards.resources ?? Array.Empty<LoopResourceRewardDefinition>())
            {
                if (resource == null || string.IsNullOrWhiteSpace(resource.resourceId) ||
                    resource.amount <= 0 || !resourceIds.Add(resource.resourceId))
                    throw new InvalidDataException(context + " contains an invalid resource reward.");
            }
        }

        private static IEnumerable<string> EnumerateReferencedRecipeIds(LoopProgressionDefinition value)
        {
            foreach (var id in value.initiallyHiddenRecipeIds) yield return id;
            foreach (var offer in value.treasureOffers)
                foreach (var id in offer.rewards.recipeIds ?? Array.Empty<string>()) yield return id;
            foreach (var enemy in value.enemyRewards)
                foreach (var stage in enemy.stages)
                    foreach (var id in stage.rewards.recipeIds ?? Array.Empty<string>()) yield return id;
        }

        private static IEnumerable<string> EnumerateReferencedResourceIds(LoopProgressionDefinition value)
        {
            foreach (var offer in value.treasureOffers)
                foreach (var reward in offer.rewards.resources ?? Array.Empty<LoopResourceRewardDefinition>()) yield return reward.resourceId;
            foreach (var enemy in value.enemyRewards)
                foreach (var stage in enemy.stages)
                    foreach (var reward in stage.rewards.resources ?? Array.Empty<LoopResourceRewardDefinition>()) yield return reward.resourceId;
        }
        private static IEnumerable<string> EnumerateReferencedCardIds(LoopProgressionDefinition value)
        {
            foreach (var id in value.initiallyHiddenCardIds) yield return id;
            foreach (var offer in value.treasureOffers)
                foreach (var id in offer.rewards.cardIds ?? Array.Empty<string>()) yield return id;
            foreach (var enemy in value.enemyRewards)
                foreach (var stage in enemy.stages)
                    foreach (var id in stage.rewards.cardIds ?? Array.Empty<string>()) yield return id;
        }
        [Serializable]
        private sealed class ResourceCatalogJson
        {
            public ResourceIdJson[] resources = Array.Empty<ResourceIdJson>();
        }

        [Serializable]
        private sealed class ResourceIdJson
        {
            public string id;
        }

        [Serializable]
        private sealed class RecipeIdJson
        {
            public string coffeeId;
        }
    }
}