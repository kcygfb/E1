using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiKs.UI
{
    /// <summary>
    /// 简单的场景跳转工具，挂到任意 GameObject 上，把方法绑定到 Button 的 OnClick。
    /// </summary>
    public class SceneNavigation : MonoBehaviour
    {
        [SerializeField] private Button navigationButton;
        private bool _isLoading;

        private void Awake()
        {
            if (navigationButton == null)
                navigationButton = GetComponent<Button>();
        }

        public void LoadCafeScene()
        {
            LoadScene("Cafe");
        }

        public void LoadPreBattleScene()
        {
            LoadScene("PreBattle");
        }

        private void LoadScene(string sceneName)
        {
            if (_isLoading) return;
            _isLoading = true;

            if (navigationButton != null)
                navigationButton.interactable = false;

            if (TransitionEffect.Instance != null)
                TransitionEffect.Instance.TransitionTo(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
