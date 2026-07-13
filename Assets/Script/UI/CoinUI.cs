using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KiKs.Data;

namespace KiKs.Core
{
    public class CoinUI : MonoBehaviour
    {
        [SerializeField] private ResourceData targetResource;
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private string format = "{0}";

        private void OnEnable()
        {
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
            RefreshDisplay();
        }

        private void Start()
        {
            if (InventorySystem.Instance == null) return;
            InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;
            InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;
        }

        private void HandleResourceChanged(string resourceId, int newAmount)
        {
            if (targetResource == null || resourceId != targetResource.ResourceId) return;
            UpdateText(newAmount);
        }

        private void RefreshDisplay()
        {
            if (targetResource != null && InventorySystem.Instance != null)
                UpdateText(InventorySystem.Instance.GetAmount(targetResource));
        }

        private void UpdateText(int amount)
        {
            if (displayText != null)
                displayText.text = string.Format(format, amount);
        }
    }
}
