using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class LoopProgressionTests
    {
        [SetUp]
        public void SetUp()
        {
            RuntimeGameRepository.ResetRunState();
            PlayerGlobalStats.ResetToFull(100);
            SetGold(0);
        }

        [TearDown]
        public void TearDown()
        {
            SetGold(0);
            RuntimeGameRepository.ResetRunState();
        }

        [Test]
        public void InitialStateHidesEightCardsAndElevenRecipes()
        {
            var definition = LoopProgressionRepository.Definition;

            Assert.That(definition.initiallyHiddenCardIds, Has.Length.EqualTo(8));
            Assert.That(definition.initiallyHiddenRecipeIds, Has.Length.EqualTo(11));
            Assert.That(definition.initiallyHiddenCardIds.All(id => !RuntimeGameRepository.IsCardUnlocked(id)), Is.True);
            Assert.That(definition.initiallyHiddenRecipeIds.All(id => !RuntimeGameRepository.IsRecipeUnlocked(id)), Is.True);
        }

        [Test]
        public void RewardSettlementIsIdempotentAndReportsDuplicates()
        {
            var bundle = new LoopRewardBundleDefinition
            {
                gold = 100,
                cardIds = new[] { "ranged_rocket_launcher" },
                recipeIds = new[] { "BudgetBrew" }
            };

            var first = RuntimeGameRepository.ApplyRewardBundle("test:settlement", bundle);
            var second = RuntimeGameRepository.ApplyRewardBundle("test:settlement", bundle);

            Assert.That(first.Applied, Is.True);
            Assert.That(first.NewCardIds, Is.EquivalentTo(bundle.cardIds));
            Assert.That(first.NewRecipeIds, Is.EquivalentTo(bundle.recipeIds));
            Assert.That(second.DuplicateSettlement, Is.True);
            Assert.That(RuntimeGameRepository.Gold, Is.EqualTo(100));
        }

        [Test]
        public void ConfigHasThreeFixedVictoryStagesForEveryEnemy()
        {
            var definition = LoopProgressionRepository.Definition;

            Assert.That(definition.enemyRewards, Has.Length.EqualTo(3));
            foreach (var enemy in definition.enemyRewards)
            {
                Assert.That(enemy.stages.Select(stage => stage.victoryNumber),
                    Is.EquivalentTo(new[] { 1, 2, 3 }));
                Assert.That(enemy.stages.All(stage => stage.rewards.gold == 100), Is.True);
            }
        }

        [Test]
        public void DailyMapAssignsEachEnemySlotExactlyOnce()
        {
            DailyAreaMapState.EnsureGenerated();

            var encounterIndexes = DailyAreaMapState.MapPoints
                .Where(point => point.Type == AreaPointType.Battle)
                .Select(point => point.EncounterIndex)
                .ToArray();

            Assert.That(encounterIndexes, Is.EquivalentTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void AnyThreeAccessibleAreasAdvanceTheDayAndFullyHeal()
        {
            PlayerGlobalStats.SetHealth(7, 20);
            for (var exploration = 0; exploration < 3; exploration++)
            {
                DailyAreaMapState.EnsureGenerated();
                var index = DailyAreaMapState.MapPoints
                    .Select((point, pointIndex) => new { point, pointIndex })
                    .First(item => item.point.Type != AreaPointType.Event && !item.point.IsCompleted)
                    .pointIndex;
                Assert.That(DailyAreaMapState.TrySelectPoint(index, out _), Is.True);
                RuntimeGameRepository.CompleteSelectedArea(defeated: false);
            }

            Assert.That(RuntimeGameRepository.CurrentDay, Is.EqualTo(2));
            Assert.That(PlayerGlobalStats.CurrentHealth, Is.EqualTo(20));
        }

        [Test]
        public void DefeatConsumesAreaAndRestoresHalfHealthRoundedUp()
        {
            PlayerGlobalStats.SetHealth(1, 21);
            DailyAreaMapState.EnsureGenerated();
            var battleIndex = DailyAreaMapState.MapPoints
                .Select((point, index) => new { point, index })
                .First(item => item.point.Type == AreaPointType.Battle)
                .index;
            Assert.That(DailyAreaMapState.TrySelectPoint(battleIndex, out _), Is.True);

            RuntimeGameRepository.CompleteSelectedArea(defeated: true);

            Assert.That(PlayerGlobalStats.CurrentHealth, Is.EqualTo(11));
            Assert.That(DailyAreaMapState.CompletedExplorationCount, Is.EqualTo(1));
            Assert.That(DailyAreaMapState.MapPoints[battleIndex].IsCompleted, Is.True);
        }

        [Test]
        public void ReferenceValidationRejectsUnknownCardRecipeAndResourceIds()
        {
            var definition = LoopProgressionRepository.Definition;
            Assert.Throws<InvalidDataException>(() =>
                LoopProgressionRepository.ValidateReferences(
                    definition, Array.Empty<string>(), null, null));
            Assert.Throws<InvalidDataException>(() =>
                LoopProgressionRepository.ValidateReferences(
                    definition, null, Array.Empty<string>(), null));
            Assert.Throws<InvalidDataException>(() =>
                LoopProgressionRepository.ValidateReferences(
                    definition, null, null, Array.Empty<string>()));
        }

        private static void SetGold(int target)
        {
            var current = RuntimeGameRepository.Gold;
            if (current < target) RuntimeGameRepository.AddGold(target - current);
            else if (current > target) Assert.That(RuntimeGameRepository.SpendGold(current - target), Is.True);
        }
    }
}
