using System;
using System.IO;
using UnityEngine;

namespace KiKs.Combat
{
    public static class TreasureJsonRepository
    {
        public const string RelativePath = "Treasure/treasures.json";
        public const int RequiredOfferCount = 4;

        public static TreasureSceneDefinition Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, RelativePath);
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("Treasure configuration was not found.", path);

                var definition = JsonUtility.FromJson<TreasureSceneDefinition>(File.ReadAllText(path));
                if (!TryValidate(definition, out var validationError))
                    throw new InvalidDataException(validationError);

                return definition;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Treasure] Failed to load '{path}': {exception.Message}. Using test fallback data.");
                return CreateFallback();
            }
        }

        public static bool TryValidate(TreasureSceneDefinition definition, out string error)
        {
            if (definition == null)
            {
                error = "The root object is null.";
                return false;
            }

            if (definition.testStartingGold < 0)
            {
                error = "testStartingGold cannot be negative.";
                return false;
            }

            if (definition.offers == null || definition.offers.Length != RequiredOfferCount)
            {
                error = $"Exactly {RequiredOfferCount} offers are required for the first treasure version.";
                return false;
            }

            for (var index = 0; index < definition.offers.Length; index++)
            {
                var offer = definition.offers[index];
                if (offer == null || string.IsNullOrWhiteSpace(offer.id))
                {
                    error = $"Offer {index + 1} needs a non-empty id.";
                    return false;
                }

                if (offer.price <= 0)
                {
                    error = $"Offer '{offer.id}' needs a positive price.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(offer.imagePath))
                {
                    error = $"Offer '{offer.id}' needs a card imagePath.";
                    return false;
                }

                if (offer.productPool == null || offer.productPool.Length == 0)
                {
                    error = $"Offer '{offer.id}' needs at least one product.";
                    return false;
                }

                foreach (var product in offer.productPool)
                {
                    if (product == null || product.weight <= 0 || product.reward == null ||
                        string.IsNullOrWhiteSpace(product.reward.type) ||
                        string.IsNullOrWhiteSpace(product.reward.id) || product.reward.amount <= 0)
                    {
                        error = $"Offer '{offer.id}' contains an invalid product or reward.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        public static TreasureSceneDefinition CreateFallback()
        {
            return new TreasureSceneDefinition
            {
                testStartingGold = 1000,
                offers = new[]
                {
                    CreateOffer("claw_offer", 50, "Art/Cards/50C.png", "resource", "claw", "爪子", 2),
                    CreateOffer("card_offer", 100, "Art/Cards/100C.png", "card", "flexible_chain", "绊脚锁", 1),
                    CreateOffer("eye_offer", 200, "Art/Cards/200C.png", "resource", "eye", "眼珠", 2),
                    CreateOffer("fire_offer", 400, "Art/Cards/400C.png", "resource", "fire", "紫色火焰", 1)
                }
            };
        }

        private static TreasureOfferDefinition CreateOffer(
            string offerId,
            int price,
            string imagePath,
            string rewardType,
            string rewardId,
            string displayName,
            int amount)
        {
            return new TreasureOfferDefinition
            {
                id = offerId,
                price = price,
                imagePath = imagePath,
                productPool = new[]
                {
                    new TreasureProductDefinition
                    {
                        weight = 1,
                        reward = new TreasureRewardDefinition
                        {
                            type = rewardType,
                            id = rewardId,
                            displayName = displayName,
                            amount = amount
                        }
                    }
                }
            };
        }
    }
}
