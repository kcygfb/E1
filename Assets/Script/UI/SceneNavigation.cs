using UnityEngine;
using UnityEngine.SceneManagement;

namespace KiKs.UI
{
    /// <summary>
    /// 简单的场景跳转工具，挂到任意 GameObject 上，把方法绑定到 Button 的 OnClick。
    /// </summary>
    public class SceneNavigation : MonoBehaviour
    {
        public void LoadCafeScene()
        {
            if (TransitionEffect.Instance != null)
                TransitionEffect.Instance.TransitionTo("Cafe");
            else
                SceneManager.LoadScene("Cafe");
        }

        public void LoadPreBattleScene()
        {
            if (TransitionEffect.Instance != null)
                TransitionEffect.Instance.TransitionTo("PreBattle");
            else
                SceneManager.LoadScene("PreBattle");
        }
    }
}
