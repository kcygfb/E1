using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class TreasurePurchaseSessionTests
    {
        [SetUp]
        public void SetUp()
        {
            DailyAreaMapState.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            DailyAreaMapState.Reset();
        }

        [Test]
        public void SuccessfulPurchaseSpendsGoldAndReturnsHiddenReward()
        {
            var offer = TreasureJsonRepository.CreateFallback().offers[0];
            var session = new TreasurePurchaseSession(120);

            var result = session.TryPurchase(offer);

            Assert.That(result.Status, Is.EqualTo(TreasurePurchaseStatus.Success));
            Assert.That(session.Gold, Is.EqualTo(70));
            Assert.That(result.Reward.id, Is.EqualTo("claw"));
            Assert.That(result.Reward.amount, Is.EqualTo(2));
        }

        [Test]
        public void PurchasedOfferCannotBeBoughtTwice()
        {
            var offer = TreasureJsonRepository.CreateFallback().offers[0];
            var session = new TreasurePurchaseSession(200);

            Assert.That(session.TryPurchase(offer).IsSuccess, Is.True);
            var secondResult = session.TryPurchase(offer);

            Assert.That(secondResult.Status, Is.EqualTo(TreasurePurchaseStatus.AlreadyPurchased));
            Assert.That(session.Gold, Is.EqualTo(150));
            Assert.That(session.Rewards.Count, Is.EqualTo(1));
        }

        [Test]
        public void InsufficientGoldDoesNotSpendOrGrantReward()
        {
            var offer = TreasureJsonRepository.CreateFallback().offers[3];
            var session = new TreasurePurchaseSession(149);

            var result = session.TryPurchase(offer);

            Assert.That(result.Status, Is.EqualTo(TreasurePurchaseStatus.InsufficientGold));
            Assert.That(session.Gold, Is.EqualTo(149));
            Assert.That(session.Rewards, Is.Empty);
        }

        [Test]
        public void TreasurePointCanDisappearWithoutAdvancingBattleOnlyPlaytestCounter()
        {
            DailyAreaMapState.EnsureGenerated();
            var treasureIndex = DailyAreaMapState.MapPoints
                .Select((point, index) => new { point, index })
                .Single(item => item.point.Type == AreaPointType.Treasure)
                .index;

            Assert.That(DailyAreaMapState.TrySelectPoint(treasureIndex, out _), Is.True);
            DailyAreaMapState.CompleteSelectedPointWithoutCountingExploration();

            Assert.That(DailyAreaMapState.MapPoints[treasureIndex].IsCompleted, Is.True);
            Assert.That(DailyAreaMapState.CompletedExplorationCount, Is.Zero);
            Assert.That(DailyAreaMapState.HasSelectedPoint, Is.False);
        }

        [Test]
        public void FirstVersionFallbackContainsExactlyFourValidOffers()
        {
            var definition = TreasureJsonRepository.CreateFallback();

            Assert.That(TreasureJsonRepository.TryValidate(definition, out var error), Is.True, error);
            Assert.That(definition.offers, Has.Length.EqualTo(4));
            Assert.That(definition.offers.All(offer => offer.productPool.Length == 1), Is.True);
        }
    }
}
