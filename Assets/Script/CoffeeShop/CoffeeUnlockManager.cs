using System.Collections.Generic;
using UnityEngine;
using KiKs.Core;

public class CoffeeUnlockManager : MonoBehaviour
{
    public static CoffeeUnlockManager Instance { get; private set; }

    private readonly HashSet<string> unlockedIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsUnlocked(CoffeeData coffee)
    {
        if (coffee == null) return false;

        // 未标记锁定 = 始终解锁
        if (!coffee.locked) return true;

        // 已解锁过 = 永久解锁
        if (unlockedIds.Contains(coffee.coffeeId)) return true;

        // 检查仓库是否拥有解锁物品
        if (coffee.unlockItem != null)
        {
            var inv = InventorySystem.Instance;
            if (inv != null && inv.GetAmount(coffee.unlockItem.ResourceId) >= coffee.unlockAmount)
            {
                unlockedIds.Add(coffee.coffeeId);
                Debug.Log($"[CoffeeUnlockManager] Item unlocked: {coffee.coffeeName}");
                return true;
            }
        }

        return false;
    }

    public void Unlock(CoffeeData coffee)
    {
        if (coffee != null && unlockedIds.Add(coffee.coffeeId))
            Debug.Log($"[CoffeeUnlockManager] Manually unlocked: {coffee.coffeeName}");
    }

    public void Lock(CoffeeData coffee)
    {
        if (coffee != null && unlockedIds.Remove(coffee.coffeeId))
            Debug.Log($"[CoffeeUnlockManager] Manually locked: {coffee.coffeeName}");
    }
}
