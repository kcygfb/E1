using KiKs.Combat;
using KiKs.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KiKs
{
    /// <summary>
    /// Composition root for ending a running game session.
    /// UI requests the operation; only this coordinator owns the full cross-system reset.
    /// </summary>
    internal static class GameSessionLifecycle
    {
        private static bool _isReturningToMainMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            ExitGamePanel.ReturnToMainMenuRequested -= ReturnToMainMenu;
            ExitGamePanel.ReturnToMainMenuRequested += ReturnToMainMenu;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            _isReturningToMainMenu = false;
            ResetEntireGameSession();
        }

        private static void ReturnToMainMenu()
        {
            if (_isReturningToMainMenu)
                return;

            _isReturningToMainMenu = true;
            var transition = TransitionEffect.Instance;
            if (transition != null)
            {
                transition.TransitionTo(ExitGamePanel.MainMenuSceneName);
                return;
            }

            SceneManager.LoadScene(ExitGamePanel.MainMenuSceneName);
        }

        private static void ResetEntireGameSession()
        {
            GameRunLifecycle.ResetForNewGame();
            InventorySystem.Instance?.ResetToStartingAmounts();
            IngredientTray.Clear();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_isReturningToMainMenu || scene.name != ExitGamePanel.MainMenuSceneName)
                return;

            // Reset only after the previous scene has unloaded. Its teardown callbacks can no longer
            // write stale battle or cafe state back into the freshly reset session.
            try
            {
                ResetEntireGameSession();
            }
            finally
            {
                _isReturningToMainMenu = false;
            }
        }
    }
}
