using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class TreasurePurchaseSessionTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeGameRepository.ResetRunState();
            SetGold(0);
        }

        [TearDown]
        public void TearDown()
        {
            SetGold(0);
            RuntimeGameRepository.ResetRunState();
        }

        [Test]
        public void SuccessfulPurchaseUsesRepositoryGoldAndUnlocksDeterministicBundle()
        {
            var offer = LoopProgressionRepository.Definition.treasureOffers.Single(item => item.price == 50);
            RuntimeGameRepository.AddGold(120);
            var session = new TreasurePurchaseSession();

            var result = session.TryPurchase(offer);

            Assert.That(result.Status, Is.EqualTo(TreasurePurchaseStatus.Success));
            Assert.That(RuntimeGameRepository.Gold, Is.EqualTo(70));
            Assert.That(result.Reward.NewCardIds, Is.EquivalentTo(new[] { "ranged_rocket_launcher" }));
            Assert.That(RuntimeGameRepository.IsCardUnlocked("ranged_rocket_launcher"), Is.True);
        }

        [Test]
        public void PurchasedTierCannotBeBoughtTwiceDuringOneVisit()
        {
            var offer = LoopProgressionRepository.Definition.treasureOffers.Single(item => item.price == 50);
            RuntimeGameRepository.AddGold(200);
            var session = new TreasurePurchaseSession();

            Assert.That(session.TryPurchase(offer).IsSuccess, Is.True);
            var secondResult = session.TryPurchase(offer);

            Assert.That(secondResult.Status, Is.EqualTo(TreasurePurchaseStatus.AlreadyPurchased));
            Assert.That(RuntimeGameRepository.Gold, Is.EqualTo(150));
        }

        [Test]
        public void InsufficientRepositoryGoldDoesNotGrantAnything()
        {
            var offer = LoopProgressionRepository.Definition.treasureOffers.Single(item => item.price == 400);
            RuntimeGameRepository.AddGold(399);
            var session = new TreasurePurchaseSession();

            var result = session.TryPurchase(offer);

            Assert.That(result.Status, Is.EqualTo(TreasurePurchaseStatus.InsufficientGold));
            Assert.That(RuntimeGameRepository.Gold, Is.EqualTo(399));
            Assert.That(RuntimeGameRepository.IsRecipeUnlocked("TheFifthFlavor"), Is.False);
        }

        [Test]
        public void FullyOwnedTierIsDisabledWithoutSpendingGold()
        {
            var offer = LoopProgressionRepository.Definition.treasureOffers.Single(item => item.price == 100);
            foreach (var cardId in offer.rewards.cardIds)
                RuntimeGameRepository.UnlockCard(cardId);
            RuntimeGameRepository.AddGold(200);
            var session = new TreasurePurchaseSession();

            var result = session.TryPurchase(offer);

            Assert.That(result.Status, Is.EqualTo(TreasurePurchaseStatus.AllRewardsOwned));
            Assert.That(RuntimeGameRepository.Gold, Is.EqualTo(200));
        }

        [Test]
        public void LeavingTreasureConsumesOneDailyExploration()
        {
            DailyAreaMapState.EnsureGenerated();
            var treasureIndex = DailyAreaMapState.MapPoints
                .Select((point, index) => new { point, index })
                .Single(item => item.point.Type == AreaPointType.Treasure)
                .index;

            Assert.That(DailyAreaMapState.TrySelectPoint(treasureIndex, out _), Is.True);
            var completion = RuntimeGameRepository.CompleteSelectedArea(defeated: false);

            Assert.That(completion.Completed, Is.True);
            Assert.That(DailyAreaMapState.MapPoints[treasureIndex].IsCompleted, Is.True);
            Assert.That(DailyAreaMapState.CompletedExplorationCount, Is.EqualTo(1));
        }

        [Test]
        public void LoopConfigContainsExactlyFourDeterministicPriceTiers()
        {
            var definition = LoopProgressionRepository.Definition;

            Assert.That(definition.treasureOffers, Has.Length.EqualTo(4));
            Assert.That(definition.treasureOffers.Select(item => item.price),
                Is.EquivalentTo(new[] { 50, 100, 200, 400 }));
            Assert.That(definition.treasureOffers.All(item => item.rewards.HasAnyReward), Is.True);
        }

        private static void SetGold(int target)
        {
            var current = RuntimeGameRepository.Gold;
            if (current < target) RuntimeGameRepository.AddGold(target - current);
            else if (current > target) Assert.That(RuntimeGameRepository.SpendGold(current - target), Is.True);
        }
    }
}