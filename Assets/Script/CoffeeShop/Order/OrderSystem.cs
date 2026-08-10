using System;
using System.Collections.Generic;
using UnityEngine;

public class OrderSystem : MonoBehaviour
{
    public const string ORDER_CREATED = "OrderCreated";
    public const string ORDER_COMPLETED = "OrderCompleted";
    public const string ORDER_CANCELLED = "OrderCancelled";

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
        GameEvent.Emit(ORDER_CREATED, activeOrder);
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
        // 菜谱解锁不在这里做——由 CraftController.Deliver 在"搓出合法咖啡"时统一解锁，
        // 保证做错的咖啡（非订单所需）也能解锁。
        GameEvent.Emit(ORDER_COMPLETED, completed);
        return true;
    }

    /// <summary>
    /// Cancels the current customer's order, or records a skip before the order
    /// ticket exists. Refuses to clear an order owned by a different customer.
    /// </summary>
    public bool TryCancelCustomerOrder(CustomerController owner, out OrderTicket cancelledOrder)
    {
        cancelledOrder = activeOrder;
        if (cancelledOrder != null && owner != null && cancelledOrder.Owner != owner)
        {
            Debug.LogError("[OrderSystem] Refused to cancel an order owned by another customer.", this);
            cancelledOrder = null;
            return false;
        }

        activeOrder = null;
        GameEvent.Emit(ORDER_CANCELLED, new OrderCancelledPayload(owner, cancelledOrder));
        Debug.Log($"[OrderSystem] Customer skipped: {owner?.NPCData?.npcName ?? "unknown"}");
        return true;
    }

    public void ClearActiveOrder()
    {
        activeOrder = null;
    }
}
