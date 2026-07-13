using System;
using UnityEngine;
using KiKs.Data;


[Serializable]
public class Ingredient
{
    public ResourceData item;

    public int amount;
}


[CreateAssetMenu(
    fileName = "CoffeeData",
    menuName = "Game/Coffee Data"
)]
public class CoffeeData : ScriptableObject
{
    public string coffeeId;

    public string coffeeName;


    public int sellPrice = 10;


    public Ingredient[] recipe;

    public Sprite orderTicket;
}