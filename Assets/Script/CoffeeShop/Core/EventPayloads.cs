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

    public OrderTicket(string orderId, string npcId, string npcName,
        string coffeeId, string coffeeName, int coffeePrice,
        Sprite ticketSprite, CustomerController owner)
    {
        OrderId = orderId;
        NpcId = npcId;
        NpcName = npcName;
        CoffeeId = coffeeId;
        CoffeeName = coffeeName;
        CoffeePrice = coffeePrice;
        TicketSprite = ticketSprite;
        Owner = owner;
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
/// DialogueRequested 事件的 payload。
/// </summary>
public class DialogueRequest
{
    public string DialogueId { get; }
    public Dictionary<string, string> Tokens { get; }
    public string SpeakerOverride { get; }
    public string Context { get; }

    public DialogueRequest(string dialogueId, string context,
        Dictionary<string, string> tokens = null, string speakerOverride = null)
    {
        DialogueId = dialogueId;
        Context = context;
        Tokens = tokens;
        SpeakerOverride = speakerOverride;
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
