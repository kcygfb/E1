using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    private readonly HashSet<string> unlockedIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsUnlocked(CoffeeData coffee)
    {
        if (coffee == null) return false;
        if (!coffee.locked) return true;
        if (unlockedIds.Contains(coffee.coffeeId)) return true;

        if (coffee.unlockItem != null)
        {
            var inv = InventorySystem.Instance;
            if (inv != null && inv.GetAmount(coffee.unlockItem.ResourceId) >= coffee.unlockAmount)
            {
                unlockedIds.Add(coffee.coffeeId);
                Debug.Log($"[UnlockManager] Item unlocked: {coffee.coffeeName}");
                return true;
            }
        }
        return false;
    }

    public void Unlock(CoffeeData coffee)
    {
        if (coffee != null && unlockedIds.Add(coffee.coffeeId))
            Debug.Log($"[UnlockManager] Manually unlocked: {coffee.coffeeName}");
    }

    public void Lock(CoffeeData coffee)
    {
        if (coffee != null && unlockedIds.Remove(coffee.coffeeId))
            Debug.Log($"[UnlockManager] Manually locked: {coffee.coffeeName}");
    }
}
