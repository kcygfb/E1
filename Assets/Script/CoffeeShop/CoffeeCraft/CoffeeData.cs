using System;
using UnityEngine;

[Serializable]
public class Ingredient
{
    public ResourceData item;
    public int amount;
}

[CreateAssetMenu(fileName = "CoffeeData", menuName = "Game/Coffee Data")]
public class CoffeeData : ScriptableObject
{
    public string coffeeId;
    public string coffeeName;
    public int sellPrice = 10;
    public Ingredient[] recipe;
    public Sprite orderTicket;

    [Header("Unlock")]
    [Tooltip("勾选 = 初始锁定，需要仓库拥有 unlockItem 才能永久解锁")]
    public bool locked = false;
    [Tooltip("解锁所需物品，null + locked = 永远锁定")]
    public ResourceData unlockItem;
    public int unlockAmount = 1;
}
