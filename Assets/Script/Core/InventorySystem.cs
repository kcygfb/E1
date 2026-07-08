using System;
using System.Collections.Generic;
using UnityEngine;
using KiKs.Data;

namespace KiKs.Core
{
    /// <summary>
    /// 库存系统（单例）：管理运行时资源数量。
    /// 其他脚本通过此 API 读写资源值，并可订阅 OnResourceChanged 事件。
    /// Singleton that manages runtime resource amounts.
    /// Other scripts read/write values through this API and subscribe to OnResourceChanged.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        /// <summary>单例实例</summary>
        public static InventorySystem Instance { get; private set; }

        // 已注册的资源列表（在 Inspector 中配置）
        [SerializeField] private List<ResourceData> registeredResources = new();

        // 资源数量字典，以资源ID为键
        private readonly Dictionary<string, int> _amounts = new();

        /// <summary>资源数量变化时触发的事件（参数：资源ID, 新数量）</summary>
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

            // 初始化已注册资源的初始数量
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

        // ── 查询 ──────────────────────────────────────────────

        /// <summary>根据资源ID获取当前数量</summary>
        public int GetAmount(string resourceId)
        {
            return _amounts.TryGetValue(resourceId, out var amount) ? amount : 0;
        }

        /// <summary>根据 ResourceData 获取当前数量</summary>
        public int GetAmount(ResourceData resource)
        {
            return resource != null ? GetAmount(resource.ResourceId) : 0;
        }

        // ── 修改 ─────────────────────────────────────────────

        /// <summary>增加指定资源的数量</summary>
        public void Add(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount == 0) return;
            _amounts.TryGetValue(resourceId, out var current);
            _amounts[resourceId] = current + amount;
            OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
        }

        /// <summary>增加指定资源的数量（通过 ResourceData）</summary>
        public void Add(ResourceData resource, int amount)
        {
            if (resource != null) Add(resource.ResourceId, amount);
        }

        /// <summary>消费（扣除）指定数量的资源，如果不足则返回 false</summary>
        public bool Spend(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount <= 0) return false;
            if (!_amounts.TryGetValue(resourceId, out var current) || current < amount) return false;
            _amounts[resourceId] = current - amount;
            OnResourceChanged?.Invoke(resourceId, _amounts[resourceId]);
            return true;
        }

        /// <summary>消费指定数量的资源（通过 ResourceData）</summary>
        public bool Spend(ResourceData resource, int amount)
        {
            return resource != null && Spend(resource.ResourceId, amount);
        }

        /// <summary>直接设置指定资源的数量</summary>
        public void SetAmount(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId)) return;
            _amounts[resourceId] = amount;
            OnResourceChanged?.Invoke(resourceId, amount);
        }

        // ── 存档 / 读档 ────────────────────────────────────────

        /// <summary>返回所有资源数量的可序列化快照（用于存档）</summary>
        public Dictionary<string, int> GetSnapshot()
        {
            return new Dictionary<string, int>(_amounts);
        }

        /// <summary>从存档快照恢复所有资源数量（用于读档）</summary>
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
