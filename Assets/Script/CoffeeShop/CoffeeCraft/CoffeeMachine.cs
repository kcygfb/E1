using System.Collections.Generic;
using UnityEngine;

public class CoffeeMachine : MonoBehaviour
{
    public OrderSystem orderSystem;

    public bool MakeCoffee(CoffeeData coffee)
    {
        Debug.Log($"Try make {coffee.coffeeName}");

        if (!CheckRecipe(coffee))
        {
            Debug.Log("Not enough ingredients!");
            return false;
        }

        bool success = ConsumeRecipe(coffee);
        if (success)
        {
            Debug.Log("Coffee Finished!");
            if (orderSystem != null)
                orderSystem.TryServeCoffee(coffee);
            else
                GameEvent.Emit("CoffeeServed", coffee);
            return true;
        }
        return false;
    }

    private bool CheckRecipe(CoffeeData coffee)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return false;
        foreach (var ingredient in coffee.recipe)
        {
            if (inv.GetAmount(ingredient.item.ResourceId) < ingredient.amount)
                return false;
        }
        return true;
    }

    private bool ConsumeRecipe(CoffeeData coffee)
    {
        var inv = InventorySystem.Instance;
        if (inv == null) return false;
        foreach (var ingredient in coffee.recipe)
        {
            if (!inv.Spend(ingredient.item.ResourceId, ingredient.amount))
                return false;
        }
        return true;
    }
}
