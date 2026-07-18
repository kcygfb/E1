using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private string targetResourceId = "gold";
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string format = "Coin: {0}";

    private void Start()
    {
        if (InventorySystem.Instance == null) return;
        InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;
    }

    private void HandleResourceChanged(string resourceId, int newAmount)
    {
        if (resourceId != targetResourceId) return;
        UpdateText(newAmount);
    }

    private void RefreshDisplay()
    {
        UpdateText(InventorySystem.Instance.GetAmount(targetResourceId));
    }

    private void UpdateText(int amount)
    {
        if (displayText != null)
            displayText.text = string.Format(format, amount);
    }
}
