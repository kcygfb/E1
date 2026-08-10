using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsUnlocked(CoffeeData coffee)
    {
        return coffee != null && KiKs.Combat.RuntimeGameRepository.IsRecipeUnlocked(coffee.coffeeId);
    }

    public void Unlock(CoffeeData coffee)
    {
        if (coffee != null && KiKs.Combat.RuntimeGameRepository.UnlockRecipe(coffee.coffeeId))
            Debug.Log($"[UnlockManager] Manually unlocked: {coffee.coffeeName}");
    }

    public void Lock(CoffeeData coffee)
    {
        if (coffee != null && KiKs.Combat.RuntimeGameRepository.LockRecipe(coffee.coffeeId))
            Debug.Log($"[UnlockManager] Manually locked: {coffee.coffeeName}");
    }
}
