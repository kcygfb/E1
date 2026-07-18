using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderSystem : MonoBehaviour
{
    public static string ORDER_CREATED = "OrderCreated";
    public static string ORDER_COMPLETED = "OrderCompleted";

    private OrderTicket activeOrder;

    public bool HasActiveOrder => activeOrder != null;
    public OrderTicket ActiveOrder => activeOrder;

    private void OnEnable()
    {
        GameEvent.On("CustomerReadyToOrder", OnCustomerReady);
        GameEvent.On("CoffeeServed", OnCoffeeServed);
    }

    private void OnDisable()
    {
        GameEvent.Off("CustomerReadyToOrder", OnCustomerReady);
        GameEvent.Off("CoffeeServed", OnCoffeeServed);
    }

    private void OnCustomerReady(object payload)
    {
        if (payload is not OrderRequest req) return;
        CreateOrder(req.Owner, req.NpcData, req.CoffeeData);
    }

    private void OnCoffeeServed(object payload)
    {
        if (payload is not CoffeeData coffee) return;
        TryServeCoffee(coffee);
    }

    public bool CreateOrder(CustomerController owner, NPCData npcData, CoffeeData coffeeData)
    {
        if (HasActiveOrder)
        {
            Debug.LogWarning("[OrderSystem] Already have active order");
            return false;
        }

        activeOrder = new OrderTicket(
            Guid.NewGuid().ToString(),
            npcData.npcId,
            npcData.npcName,
            coffeeData.coffeeId,
            coffeeData.coffeeName,
            coffeeData.sellPrice,
            coffeeData.orderTicket,
            owner
        );

        Debug.Log($"[OrderSystem] Created: {activeOrder.NpcName} wants {activeOrder.CoffeeName}");
        GameEvent.Emit("OrderCreated", activeOrder);
        return true;
    }

    public bool TryServeCoffee(CoffeeData coffee)
    {
        if (activeOrder == null)
        {
            Debug.LogWarning("[OrderSystem] No active order");
            return false;
        }

        if (activeOrder.CoffeeId != coffee.coffeeId)
        {
            Debug.Log($"[OrderSystem] Wrong coffee! Need {activeOrder.CoffeeName}");
            return false;
        }

        var completed = activeOrder;
        activeOrder = null;
        Debug.Log($"[OrderSystem] Completed: {completed.CoffeeName}");
        GameEvent.Emit("OrderCompleted", completed);
        return true;
    }

    public void ClearActiveOrder()
    {
        activeOrder = null;
    }
}
