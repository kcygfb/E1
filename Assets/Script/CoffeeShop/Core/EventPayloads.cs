using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 订单数据。由 OrderSystem 创建，通过 GameEvent 传递。
/// </summary>
[Serializable]
public class OrderTicket
{
    public string OrderId { get; }
    public string NpcId { get; }
    public string NpcName { get; }
    public string CoffeeId { get; }
    public string CoffeeName { get; }
    public int CoffeePrice { get; }
    public Sprite TicketSprite { get; }
    public CustomerController Owner { get; }

    /// <summary>如果为 true，TryServeCoffee 跳过 CoffeeId 匹配检查，接受任何咖啡。</summary>
    public bool AcceptAnyCoffee { get; }

    /// <summary>QTE 评分结果，由 CraftController 在交付时填充</summary>
    public QTEScoreResult QTEScore { get; set; }

    public OrderTicket(string orderId, string npcId, string npcName,
        string coffeeId, string coffeeName, int coffeePrice,
        Sprite ticketSprite, CustomerController owner,
        bool acceptAnyCoffee = false)
    {
        OrderId = orderId;
        NpcId = npcId;
        NpcName = npcName;
        CoffeeId = coffeeId;
        CoffeeName = coffeeName;
        CoffeePrice = coffeePrice;
        TicketSprite = ticketSprite;
        Owner = owner;
        AcceptAnyCoffee = acceptAnyCoffee;
    }

    /// <summary>
    /// Resolves an accept-any order to the coffee that was actually served so
    /// downstream systems receive the real sale name and price.
    /// </summary>
    public OrderTicket CompleteWith(CoffeeData servedCoffee)
    {
        if (servedCoffee == null)
            throw new ArgumentNullException(nameof(servedCoffee));

        if (!AcceptAnyCoffee)
            return this;

        return new OrderTicket(
            OrderId,
            NpcId,
            NpcName,
            servedCoffee.coffeeId,
            servedCoffee.coffeeName,
            servedCoffee.sellPrice,
            servedCoffee.orderTicket,
            Owner,
            true)
        {
            QTEScore = QTEScore
        };
    }
}

/// <summary>
/// CustomerReadyToOrder 事件的 payload。
/// </summary>
public class OrderRequest
{
    public CustomerController Owner { get; }
    public NPCData NpcData { get; }
    public CoffeeData CoffeeData { get; }

    public OrderRequest(CustomerController owner, NPCData npcData, CoffeeData coffeeData)
    {
        Owner = owner;
        NpcData = npcData;
        CoffeeData = coffeeData;
    }
}

/// <summary>
/// OrderCancelled event payload. Order can be null when a customer is skipped
/// before their arrival dialogue has created an order ticket.
/// </summary>
public sealed class OrderCancelledPayload
{
    public CustomerController Owner { get; }
    public OrderTicket Order { get; }
    public bool HadActiveOrder => Order != null;

    public OrderCancelledPayload(CustomerController owner, OrderTicket order)
    {
        Owner = owner;
        Order = order;
    }
}

/// <summary>
/// A completed order's awarded revenue. Rewarder emits this after the gold has
/// been added so end-of-day UI does not duplicate the reward calculation.
/// </summary>
[Serializable]
public class RevenueAwardedPayload
{
    public string OrderId { get; }
    public string CoffeeName { get; }
    public int CoffeeRevenue { get; }
    public int PerfectBonus { get; }
    public int TotalRevenue { get; }
    public bool IsPerfect { get; }

    public RevenueAwardedPayload(
        string orderId,
        string coffeeName,
        int coffeeRevenue,
        int perfectBonus,
        bool isPerfect)
    {
        OrderId = orderId;
        CoffeeName = coffeeName;
        CoffeeRevenue = coffeeRevenue;
        PerfectBonus = perfectBonus;
        TotalRevenue = coffeeRevenue + perfectBonus;
        IsPerfect = isPerfect;
    }
}
/// <summary>
/// DialogueRequested event payload.
/// </summary>
public class DialogueRequest
{
    public string DialogueId { get; }
    public Dictionary<string, string> Tokens { get; }
    public string SpeakerOverride { get; }
    public Color SpeakerColor { get; }
    public string Context { get; }

    public DialogueRequest(string dialogueId, string context,
        Dictionary<string, string> tokens = null, string speakerOverride = null,
        Color speakerColor = default)
    {
        DialogueId = dialogueId;
        Context = context;
        Tokens = tokens;
        SpeakerOverride = speakerOverride;
        SpeakerColor = speakerColor == default ? Color.white : speakerColor;
    }
}

/// <summary>
/// PhaseChanged 事件的 payload。
/// </summary>
public struct PhaseChangedPayload
{
    public DayPhase Phase;
    public int Day;
}
