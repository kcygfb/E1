using System.Collections.Generic;
using UnityEngine;
using TMPro;
using KiKs.Core;
using KiKs.Data;

public class IngredientUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string[] excludedResourceIds = { "gold" };

    private readonly Dictionary<string, int> cachedAmounts = new Dictionary<string, int>();

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
        cachedAmounts[resourceId] = newAmount;
        UpdateText();
    }

    private void RefreshDisplay()
    {
        if (InventorySystem.Instance == null) return;

        var snapshot = InventorySystem.Instance.GetSnapshot();
        cachedAmounts.Clear();
        foreach (var kvp in snapshot)
            cachedAmounts[kvp.Key] = kvp.Value;

        UpdateText();
    }

    private bool IsExcluded(string resourceId)
    {
        if (excludedResourceIds == null) return false;
        foreach (var id in excludedResourceIds)
            if (id == resourceId) return true;
        return false;
    }

    private void UpdateText()
    {
        if (displayText == null) return;

        var sb = new System.Text.StringBuilder();

        var snapshot = InventorySystem.Instance != null
            ? InventorySystem.Instance.GetSnapshot()
            : cachedAmounts;

        foreach (var kvp in snapshot)
        {
            if (IsExcluded(kvp.Key)) continue;

            if (sb.Length > 0)
                sb.AppendLine();

            sb.Append($"{kvp.Key}: {kvp.Value}");
        }

        displayText.text = sb.ToString();
    }
}
