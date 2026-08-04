using TMPro;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// 跨场景玩家血量显示。从 PlayerGlobalStats 读取当前/最大血量。
    /// </summary>
    public class PlayerGlobalHpUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private string format = "{0} / {1}";

        private void Start()
        {
            RefreshDisplay();
        }

        private void Update()
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (displayText != null)
                displayText.text = string.Format(format, PlayerGlobalStats.CurrentHealth, PlayerGlobalStats.MaxHealth);
        }
    }
}
