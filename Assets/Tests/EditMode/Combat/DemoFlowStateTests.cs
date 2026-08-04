using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KiKs.Combat.Tests
{
    public sealed class DemoFlowStateTests
    {
        [SetUp]
        public void SetUp()
        {
            DemoFlowState.ResetDemoProgress();
        }

        [TearDown]
        public void TearDown()
        {
            DemoFlowState.ResetDemoProgress();
        }

        [Test]
        public void BattlesAdvanceInDogGirlEyeOrderAndThenComplete()
        {
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.DogBattle));
            Assert.That(DemoFlowState.CurrentDay, Is.EqualTo(1));

            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.DogBattle), Is.True);
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.LittleGirlBattle));
            Assert.That(DemoFlowState.CurrentDay, Is.EqualTo(2));

            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.LittleGirlBattle), Is.True);
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.BigEyeBattle));
            Assert.That(DemoFlowState.CurrentDay, Is.EqualTo(3));

            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.BigEyeBattle), Is.True);
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.Completed));
            Assert.That(DemoFlowState.IsCompleted, Is.True);
        }

        [Test]
        public void LockedOrCompletedStageCannotAdvanceProgress()
        {
            LogAssert.Expect(
                LogType.Error, new Regex(@"\[DemoFlow\] Cannot complete LittleGirlBattle;.*"));
            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.LittleGirlBattle), Is.False);
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.DogBattle));

            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.DogBattle), Is.True);
            LogAssert.Expect(
                LogType.Error, new Regex(@"\[DemoFlow\] Cannot complete BigEyeBattle;.*"));
            Assert.That(DemoFlowState.CompleteCurrentBattle(DemoStage.BigEyeBattle), Is.False);
            Assert.That(DemoFlowState.CurrentStage, Is.EqualTo(DemoStage.LittleGirlBattle));
        }
    }
}
