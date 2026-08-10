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

        bool anyCoffee = owner != null && owner.AcceptAny;

        activeOrder = new OrderTicket(
            Guid.NewGuid().ToString(),
            npcData.npcId,
            npcData.npcName,
            anyCoffee ? "" : (coffeeData?.coffeeId ?? ""),
            anyCoffee ? "任意咖啡" : (coffeeData?.coffeeName ?? "咖啡"),
            anyCoffee ? 0 : (coffeeData?.sellPrice ?? 0),
            anyCoffee ? null : coffeeData?.orderTicket,
            owner,
            anyCoffee
        );

        Debug.Log($"[OrderSystem] Created: {activeOrder.NpcName} wants {(anyCoffee ? "ANY coffee" : activeOrder.CoffeeName)}");
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

        if (!activeOrder.AcceptAnyCoffee && activeOrder.CoffeeId != coffee.coffeeId)
        {
            Debug.Log($"[OrderSystem] Wrong coffee! Need {activeOrder.CoffeeName}");
            return false;
        }

        var completed = activeOrder;
        activeOrder = null;
        Debug.Log($"[OrderSystem] Completed: {completed.CoffeeName}");
        KiKs.Combat.RuntimeGameRepository.AddCraftedCoffee(coffee.coffeeId);
        // 玩家摸索做出的咖啡自动解锁菜谱（解锁后可在菜单/战备界面看到）
        if (KiKs.Combat.RuntimeGameRepository.UnlockRecipe(coffee.coffeeId))
            Debug.Log($"[OrderSystem] 解锁新菜谱: {completed.CoffeeName}");
        GameEvent.Emit("OrderCompleted", completed);
        return true;
    }

    public void ClearActiveOrder()
    {
        activeOrder = null;
    }
}
