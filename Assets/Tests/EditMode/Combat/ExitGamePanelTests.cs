using KiKs.UI;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class ExitGamePanelTests
    {
        [TestCase("MainMenu", ExitConfirmationAction.QuitApplication)]
        [TestCase("Cafe", ExitConfirmationAction.ReturnToMainMenu)]
        [TestCase("PreBattle", ExitConfirmationAction.ReturnToMainMenu)]
        [TestCase("Card", ExitConfirmationAction.ReturnToMainMenu)]
        [TestCase("Treasure", ExitConfirmationAction.ReturnToMainMenu)]
        [TestCase("Collect", ExitConfirmationAction.ReturnToMainMenu)]
        [TestCase("Event", ExitConfirmationAction.ReturnToMainMenu)]
        public void ConfirmationActionDependsOnlyOnWhetherSceneIsMainMenu(
            string sceneName,
            ExitConfirmationAction expected)
        {
            Assert.That(ExitGamePanel.GetConfirmationAction(sceneName), Is.EqualTo(expected));
        }
    }
}
