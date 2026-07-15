using System;
using UnityEngine;

[Serializable]
public class RecipeEntry
{
    public string resourceId;
    public int amount;
}

[Serializable]
public class CraftStep
{
    public string id;
    public string resourceId;
    public int amount;
}

[CreateAssetMenu(fileName = "CoffeeData", menuName = "Game/Coffee Data")]
public class CoffeeData : ScriptableObject
{
    public string coffeeId;
    public string coffeeName;
    public int sellPrice = 10;
    public Sprite orderTicket;

    [Header("Unlock")]
    [Tooltip("勾选 = 初始锁定，需要仓库拥有 unlockItem 才能永久解锁")]
    public bool locked = false;
    [Tooltip("解锁所需物品的 resourceId")]
    public string unlockItemId;
    public int unlockAmount = 1;

    [Header("Recipe (loaded from JSON at runtime)")]
    [Tooltip("运行时从 JSON 加载，Inspector 中只读")]
    [SerializeField] private RecipeEntry[] recipe = Array.Empty<RecipeEntry>();

    public RecipeEntry[] Recipe => recipe;

    [Header("Craft Steps (loaded from JSON at runtime)")]
    [SerializeField] private CraftStep[] steps = Array.Empty<CraftStep>();

    public CraftStep[] Steps => steps;

    /// <summary>运行时从 CoffeeDataLoader 加载 JSON 数据覆盖此 SO 实例</summary>
    public void ApplyJson(CoffeeDataJson json)
    {
        if (json == null) return;
        coffeeId = json.coffeeId;
        coffeeName = json.coffeeName;
        sellPrice = json.sellPrice;
        locked = json.locked;
        unlockItemId = json.unlockItemId ?? "";
        unlockAmount = json.unlockAmount;

        if (json.recipe != null)
        {
            recipe = new RecipeEntry[json.recipe.Count];
            for (int i = 0; i < json.recipe.Count; i++)
            {
                recipe[i] = new RecipeEntry
                {
                    resourceId = json.recipe[i].resourceId,
                    amount = json.recipe[i].amount
                };
            }
        }

        if (json.steps != null)
        {
            steps = new CraftStep[json.steps.Count];
            for (int i = 0; i < json.steps.Count; i++)
            {
                steps[i] = new CraftStep
                {
                    id = json.steps[i].id,
                    resourceId = json.steps[i].resourceId ?? "",
                    amount = json.steps[i].amount
                };
            }
        }
    }
}
