using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class DailyAreaMapStateTests
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
        public void GeneratedMapAlwaysContainsThreeBattlesOneEventAndOneTreasure()
        {
            DailyAreaMapState.EnsureGenerated();

            Assert.That(DailyAreaMapState.MapPoints.Count, Is.EqualTo(DailyAreaMapState.PointCount));
            Assert.That(
                DailyAreaMapState.MapPoints.Count(point => point.Type == AreaPointType.Battle),
                Is.EqualTo(3));
            Assert.That(
                DailyAreaMapState.MapPoints.Count(point => point.Type == AreaPointType.Event),
                Is.EqualTo(1));
            Assert.That(
                DailyAreaMapState.MapPoints.Count(point => point.Type == AreaPointType.Treasure),
                Is.EqualTo(1));
        }

        [Test]
        public void SelectedPointCompletesAndCannotBeSelectedAgain()
        {
            DailyAreaMapState.EnsureGenerated();
            var pointIndex = DailyAreaMapState.MapPoints
                .Select((point, index) => new { point, index })
                .First(item => item.point.Type == AreaPointType.Battle)
                .index;

            Assert.That(DailyAreaMapState.TrySelectPoint(pointIndex, out _), Is.True);
            Assert.That(DailyAreaMapState.MapPoints[pointIndex].IsSelected, Is.True);

            DailyAreaMapState.CompleteSelectedPoint();

            Assert.That(DailyAreaMapState.CompletedExplorationCount, Is.EqualTo(1));
            Assert.That(DailyAreaMapState.MapPoints[pointIndex].IsCompleted, Is.True);
            Assert.That(DailyAreaMapState.TrySelectPoint(pointIndex, out _), Is.False);
        }

        [Test]
        public void SelectingAnotherPointReplacesThePreviousSelection()
        {
            DailyAreaMapState.EnsureGenerated();
            var battlePointIndexes = DailyAreaMapState.MapPoints
                .Select((point, index) => new { point, index })
                .Where(item => item.point.Type == AreaPointType.Battle)
                .Select(item => item.index)
                .Take(2)
                .ToArray();

            Assert.That(DailyAreaMapState.TrySelectPoint(battlePointIndexes[0], out _), Is.True);
            Assert.That(DailyAreaMapState.TrySelectPoint(battlePointIndexes[1], out _), Is.True);

            Assert.That(DailyAreaMapState.SelectedPointIndex, Is.EqualTo(battlePointIndexes[1]));
            Assert.That(DailyAreaMapState.MapPoints[battlePointIndexes[0]].IsSelected, Is.False);
            Assert.That(DailyAreaMapState.MapPoints[battlePointIndexes[1]].IsSelected, Is.True);
        }
    }
}
