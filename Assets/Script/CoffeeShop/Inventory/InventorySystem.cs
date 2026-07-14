using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private readonly Dictionary<string, int> _amounts = new();

    public event Action<string, int> OnResourceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 直接从磁盘加载，不依赖 ResourceDataLoader.Instance 的初始化时机
        var db = ResourceDataLoader.LoadDirect();
        if (db != null)
        {
            foreach (var res in db.resources)
            {
                if (!string.IsNullOrEmpty(res.id) && !_amounts.ContainsKey(res.id))
                    _amounts[res.id] = res.startingAmount;
            }
            Debug.Log($"[InventorySystem] Initialized {_amounts.Count} resources.");
        }
        else
        {
            Debug.LogWarning("[InventorySystem] Failed to load resources from JSON.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetAmount(string resourceId) =>
        _amounts.TryGetValue(resourceId, out var amount) ? amount : 0;

    public void Add(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount == 0) return;
        _amounts.TryGetValue(resourceId, out var current);
        _amounts[resourceId] = current + amount;
        OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
    }

    public bool Spend(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId) || amount <= 0) return false;
        if (!_amounts.TryGetValue(resourceId, out var current) || current < amount) return false;
        _amounts[resourceId] = current - amount;
        OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
        return true;
    }

    public void SetAmount(string resourceId, int amount)
    {
        if (string.IsNullOrEmpty(resourceId)) return;
        _amounts[resourceId] = amount;
        OnResourceChanged?.Invoke(resourceId, amount);
    }

    public Dictionary<string, int> GetSnapshot() => new(_amounts);

    public void RestoreSnapshot(Dictionary<string, int> snapshot)
    {
        if (snapshot == null) return;
        _amounts.Clear();
        foreach (var kvp in snapshot)
        {
            _amounts[kvp.Key] = kvp.Value;
            OnResourceChanged?.Invoke(kvp.Key, kvp.Value);
        }
    }
}
