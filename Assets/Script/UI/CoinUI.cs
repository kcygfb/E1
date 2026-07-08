using UnityEngine;
using UnityEngine.UI;
using KiKs.Data;

namespace KiKs.Core
{
    /// <summary>
    /// 临时UI：订阅库存系统（InventorySystem），在UI文本上显示指定资源的当前数量。
    /// Temporary UI: subscribes to InventorySystem and displays a resource amount on a UI Text.
    /// </summary>
    public class CoinUI : MonoBehaviour
    {
        // 要显示的目标资源
        [SerializeField] private ResourceData targetResource;
        // 用于显示数量的UI文本组件
        [SerializeField] private Text displayText;
        // 显示格式字符串，默认直接显示数字
        [SerializeField] private string format = "{0}";

        /// <summary>启用时订阅资源变化事件并刷新显示</summary>
        private void OnEnable()
        {
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
            RefreshDisplay();
        }

        /// <summary>禁用时取消订阅资源变化事件</summary>
        private void OnDisable()
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;
        }

        /// <summary>当资源数量变化时，检查是否是目标资源，并更新显示</summary>
        private void HandleResourceChanged(string resourceId, int newAmount)
        {
            if (targetResource == null || resourceId != targetResource.ResourceId) return;
            UpdateText(newAmount);
        }

        /// <summary>从库存系统获取目标资源的当前数量并刷新显示</summary>
        private void RefreshDisplay()
        {
            if (targetResource != null && InventorySystem.Instance != null)
                UpdateText(InventorySystem.Instance.GetAmount(targetResource));
        }

        /// <summary>将数量按照格式字符串更新到UI文本上</summary>
        private void UpdateText(int amount)
        {
            if (displayText != null)
                displayText.text = string.Format(format, amount);
        }
    }
}
