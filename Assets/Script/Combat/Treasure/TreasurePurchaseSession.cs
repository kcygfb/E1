using System;
using System.Collections.Generic;

namespace KiKs.Combat
{
    public enum TreasurePurchaseStatus
    {
        Success,
        InvalidOffer,
        AlreadyPurchased,
        InsufficientGold,
        AllRewardsOwned
    }

    public sealed class TreasurePurchaseResult
    {
        public TreasurePurchaseStatus Status { get; }
        public RewardGrantResult Reward { get; }
        public bool IsSuccess => Status == TreasurePurchaseStatus.Success;

        public TreasurePurchaseResult(TreasurePurchaseStatus status, RewardGrantResult reward = null)
        {
            Status = status;
            Reward = reward;
        }
    }

    /// <summary>
    /// Per-treasure-visit purchase guard. Currency, rewards and unlocks always come from
    /// RuntimeGameRepository; this object stores only which tiers were bought in this visit.
    /// </summary>
    public sealed class TreasurePurchaseSession
    {
        private readonly HashSet<string> purchasedOfferIds = new(StringComparer.Ordinal);

        public int Gold => RuntimeGameRepository.Gold;

        public bool IsPurchased(string offerId) =>
            !string.IsNullOrWhiteSpace(offerId) && purchasedOfferIds.Contains(offerId);

        public bool IsFullyOwned(TreasureOfferDefinition offer) =>
            offer?.rewards != null && !RuntimeGameRepository.WouldGrantAnyNewUnlock(offer.rewards);

        public TreasurePurchaseResult TryPurchase(TreasureOfferDefinition offer)
        {
            if (offer == null || string.IsNullOrWhiteSpace(offer.id) || offer.price <= 0 ||
                offer.rewards == null || !offer.rewards.HasAnyReward)
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InvalidOffer);

            if (purchasedOfferIds.Contains(offer.id))
                return new TreasurePurchaseResult(TreasurePurchaseStatus.AlreadyPurchased);

            if (IsFullyOwned(offer))
                return new TreasurePurchaseResult(TreasurePurchaseStatus.AllRewardsOwned);

            if (Gold < offer.price)
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InsufficientGold);

            var pointIndex = DailyAreaMapState.HasSelectedPoint
                ? DailyAreaMapState.SelectedPointIndex
                : -1;
            var settlementId = $"treasure:d{RuntimeGameRepository.CurrentDay}:p{pointIndex}:{offer.id}";
            if (!RuntimeGameRepository.TryPurchaseReward(
                    settlementId,
                    offer.price,
                    offer.rewards,
                    out var reward))
                return new TreasurePurchaseResult(TreasurePurchaseStatus.InvalidOffer);

            purchasedOfferIds.Add(offer.id);
            return new TreasurePurchaseResult(TreasurePurchaseStatus.Success, reward);
        }
    }
}