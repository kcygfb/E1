using UnityEngine;
using KiKs.Core;

public class CoffeeShopRewarder : MonoBehaviour
{

    private void OnEnable()
    {
        EventBus.OrderCompleted += Reward;
    }


    private void OnDisable()
    {
        EventBus.OrderCompleted -= Reward;
    }


    private void Reward(
        OrderRuntime order
    )
    {
        if (order == null)
            return;

        var inv = InventorySystem.Instance;

        if (inv == null)
            return;

        inv.Add(
            "gold",
            order.coffeePrice
        );

        Debug.Log(
            $"Gold +{order.coffeePrice} from {order.coffeeName}"
        );
    }

}
