using System;
using System.Collections.Generic;
using UnityEngine;
using KiKs.Data;

namespace KiKs.Core
{
    /// <summary>
    /// Singleton that manages runtime resource amounts.
    /// Other scripts read/write values through this API and subscribe to OnResourceChanged.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        [SerializeField] private List<ResourceData> registeredResources = new();

        private readonly Dictionary<string, int> _amounts = new();

        /// <summary>Raised whenever a resource value changes. (resourceId, newAmount)</summary>
        public event Action<string, int> OnResourceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var resource in registeredResources)
            {
                if (resource != null && !_amounts.ContainsKey(resource.ResourceId))
                    _amounts[resource.ResourceId] = resource.StartingAmount;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Query ──────────────────────────────────────────────

        public int GetAmount(string resourceId)
        {
            return _amounts.TryGetValue(resourceId, out var amount) ? amount : 0;
        }

        public int GetAmount(ResourceData resource)
        {
            return resource != null ? GetAmount(resource.ResourceId) : 0;
        }

        // ── Modify ─────────────────────────────────────────────

        public void Add(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount == 0) return;
            _amounts.TryGetValue(resourceId, out var current);
            _amounts[resourceId] = current + amount;
            OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
        }

        public void Add(ResourceData resource, int amount)
        {
            if (resource != null) Add(resource.ResourceId, amount);
        }

        public bool Spend(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount <= 0) return false;
            if (!_amounts.TryGetValue(resourceId, out var current) || current < amount) return false;
            _amounts[resourceId] = current - amount;
            OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
            return true;
        }

        public bool Spend(ResourceData resource, int amount)
        {
            return resource != null && Spend(resource.ResourceId, amount);
        }

        public void SetAmount(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId)) return;
            _amounts[resourceId] = amount;
            OnResourceChanged?.Invoke(resourceId, amount);
        }

        // ── Save / Load ────────────────────────────────────────

        /// <summary>Returns a serializable snapshot of all resource amounts.</summary>
        public Dictionary<string, int> GetSnapshot()
        {
            return new Dictionary<string, int>(_amounts);
        }

        /// <summary>Restores all resource amounts from a save snapshot.</summary>
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
}
