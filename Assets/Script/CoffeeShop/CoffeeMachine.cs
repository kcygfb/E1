using UnityEngine;
using KiKs.Core;


public class CoffeeMachine : MonoBehaviour
{

    public OrderSystem orderSystem;



    public bool MakeCoffee(
        CoffeeData coffee
    )
    {

        Debug.Log(
            $"Try make {coffee.coffeeName}"
        );



        if (
            !CheckRecipe(coffee)
        )
        {

            Debug.Log(
                "Not enough ingredients!"
            );

            return false;

        }




        bool success =
            ConsumeRecipe(
                coffee
            );



        if (success)
        {

            Debug.Log(
                "Coffee Finished!"
            );


            orderSystem.TryServeCoffee(
                coffee
            );

            return true;

        }



        return false;

    }



    private bool CheckRecipe(
        CoffeeData coffee
    )
    {

        var inv = InventorySystem.Instance;

        if (inv == null)
            return false;


        foreach (
            Ingredient ingredient
            in coffee.recipe
        )
        {

            if (
                inv.GetAmount(ingredient.item.ResourceId)
                < ingredient.amount
            )
            {
                return false;
            }

        }


        return true;

    }



    private bool ConsumeRecipe(
        CoffeeData coffee
    )
    {

        var inv = InventorySystem.Instance;

        if (inv == null)
            return false;


        foreach (
            Ingredient ingredient
            in coffee.recipe
        )
        {

            if (
                !inv.Spend(
                    ingredient.item.ResourceId,
                    ingredient.amount
                )
            )
            {
                return false;
            }

        }


        return true;

    }

}
