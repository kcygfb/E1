using System;
using System.Collections.Generic;

namespace KiKs.Combat
{
    public enum TreasurePurchaseStatus
    {
        Success,
        InvalidOffer,
        AlreadyPurchased,
        InsufficientGold
    }

    public sealed class TreasurePurchaseResult
    {
        public TreasurePurchaseStatus Status { get; }
        public TreasureRewardDefinition Reward { get; }

        public bool IsSuccess => Status == TreasurePurchaseStatus.Success;

        public TreasurePurchaseResult(TreasurePurchaseStatus status, TreasureRewardDefinition reward = null)
        {
            Status = status;
            Reward = reward;
        }
    }

    /// <summary>
    /// Temporary treasure-only wallet and reward history. It deliberately does not write
    /// to RuntimeGameRepository or InventorySystem; the shared save/inventory integration
    /// will replace this session boundary later.
    /// </summary>
    public sealed class TreasurePurchaseSession
    {
        private readonly HashSet<string> purchasedOfferIds = new(StringComparer.Ordinal);
        private readonly List<TreasureRewardDefinition> rewards = new();

        public int Gold { get; private set; }
        public IReadOnlyList<TreasureRewardDefinition> Rewards => rewards;

        public TreasurePurchaseSession(int startingGold)
        {
            Gold = Math.Max(0, startingGold);
        }

        public TreasurePurchaseResult TryPurchase(
            TreasureOfferDefinition offer,
            Func<float> randomValue = null)
        {
            if (offer == null || string.IsNullOrWhiteSpace(offer.id) || offer.price <= 0 ||
                offer.productPool == null || offer.productPool.Length == 0)
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InvalidOffer);

            if (purchasedOfferIds.Contains(offer.id))
                return new TreasurePurchaseResult(TreasurePurchaseStatus.AlreadyPurchased);

            if (Gold < offer.price)
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InsufficientGold);

            var reward = ChooseReward(offer.productPool, randomValue?.Invoke() ?? 0f);
            if (reward == null)
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InvalidOffer);

            Gold -= offer.price;
            purchasedOfferIds.Add(offer.id);
            rewards.Add(reward);
            return new TreasurePurchaseResult(TreasurePurchaseStatus.Success, reward);
        }

        private static TreasureRewardDefinition ChooseReward(
            IReadOnlyList<TreasureProductDefinition> products,
            float randomValue)
        {
            var totalWeight = 0;
            foreach (var product in products)
                if (product != null && product.reward != null && product.weight > 0)
                    totalWeight += product.weight;

            if (totalWeight <= 0)
                return null;

            var roll = Math.Clamp(randomValue, 0f, 0.999999f) * totalWeight;
            var cumulative = 0;
            foreach (var product in products)
            {
                if (product == null || product.reward == null || product.weight <= 0)
                    continue;

                cumulative += product.weight;
                if (roll < cumulative)
                    return product.reward;
            }

            return null;
        }
    }
}
