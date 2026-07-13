using System;
using UnityEngine;


[Serializable]
public class OrderRuntime
{

    public string orderId;


    public string npcId;

    public string npcName;



    public string coffeeId;

    public string coffeeName;


    // ⭐ 新增：咖啡售价
    public int coffeePrice;

    // 订单图片
    public Sprite orderTicket;



    // 对应NPC
    public NPCController Owner { get; private set; }




    public OrderRuntime(
        string orderId,

        string npcId,

        string npcName,


        string coffeeId,

        string coffeeName,


        int coffeePrice,


        Sprite orderTicket,


        NPCController owner
    )
    {

        this.orderId = orderId;


        this.npcId = npcId;

        this.npcName = npcName;



        this.coffeeId = coffeeId;

        this.coffeeName = coffeeName;



        this.coffeePrice = coffeePrice;

        this.orderTicket = orderTicket;



        this.Owner = owner;

    }

}







public class OrderSystem : MonoBehaviour
{

    public static string ORDER_CREATED = "ORDER_CREATED";

    public static string ORDER_COMPLETED = "ORDER_COMPLETED";



    private OrderRuntime activeOrder;



    public bool HasActiveOrder
    {
        get
        {
            return activeOrder != null;
        }
    }



    public OrderRuntime ActiveOrder
    {
        get
        {
            return activeOrder;
        }
    }






    //==============================
    // 创建订单
    //==============================

    public bool CreateOrder(
        NPCController owner,

        NPCData npcData,

        CoffeeData coffeeData
    )
    {

        if (HasActiveOrder)
        {

            Debug.LogWarning(
                "[OrderSystem] Already have active order"
            );


            return false;

        }




        activeOrder =
            new OrderRuntime(

                Guid.NewGuid().ToString(),


                npcData.npcId,

                npcData.npcName,



                coffeeData.coffeeId,

                coffeeData.coffeeName,


                // ⭐ 这里记录咖啡价格
                coffeeData.sellPrice,

                coffeeData.orderTicket,



                owner

            );





        Debug.Log(
            $"Order Created: {activeOrder.npcName} wants {activeOrder.coffeeName} price {activeOrder.coffeePrice}"
        );



        EventBus.PublishOrderCreated(activeOrder);



        return true;

    }







    //==============================
    // 玩家提交咖啡
    //==============================

    public bool TryServeCoffee(
        CoffeeData coffee
    )
    {

        if (activeOrder == null)
        {

            Debug.LogWarning(
                "[OrderSystem] No active order"
            );


            return false;

        }




        if (activeOrder.coffeeId != coffee.coffeeId)
        {

            Debug.Log(
                $"Wrong coffee! Need {activeOrder.coffeeName}"
            );


            return false;

        }





        OrderRuntime completedOrder =
            activeOrder;



        activeOrder = null;




        Debug.Log(
            $"Order Completed: {completedOrder.coffeeName}"
        );



        EventBus.PublishOrderCompleted(
            completedOrder
        );



        return true;

    }






    //==============================
    // 测试用
    //==============================

    public void CompleteAllOrders()
    {

        if (activeOrder == null)
        {
            return;
        }



        OrderRuntime completedOrder =
            activeOrder;



        activeOrder = null;



        EventBus.PublishOrderCompleted(
            completedOrder
        );

    }

}