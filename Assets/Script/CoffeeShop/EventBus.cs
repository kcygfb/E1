using System;
using UnityEngine;

public static class EventBus
{
    public static event Action<int> DayStarted;
    public static event Action<int> DayEnded;
    public static event Action<OrderRuntime> OrderCreated;
    public static event Action<OrderRuntime> OrderCompleted;

    public static void PublishDayStarted(int day)
    {
        Debug.Log($"[EventBus] DayStarted -> Day {day}");
        DayStarted?.Invoke(day);
    }

    public static void PublishDayEnded(int day)
    {
        Debug.Log($"[EventBus] DayEnded -> Day {day}");
        DayEnded?.Invoke(day);
    }

    public static void PublishOrderCreated(OrderRuntime order)
    {
        Debug.Log($"[EventBus] OrderCreated -> {order.npcName} wants {order.coffeeName}");
        OrderCreated?.Invoke(order);
    }

    public static void PublishOrderCompleted(OrderRuntime order)
    {
        Debug.Log($"[EventBus] OrderCompleted -> {order.npcName} got {order.coffeeName}");
        OrderCompleted?.Invoke(order);
    }
}