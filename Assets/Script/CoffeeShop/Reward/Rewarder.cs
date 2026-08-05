using UnityEngine;
using KiKs.Combat;

public class Rewarder : MonoBehaviour
{
    [SerializeField, Min(0)] private int perfectCraftBonus = 250;

    private void OnEnable()
    {
        GameEvent.On("OrderCompleted", OnOrderCompleted);
    }

    private void OnDisable()
    {
        GameEvent.Off("OrderCompleted", OnOrderCompleted);
    }

    private void OnOrderCompleted(object payload)
    {
        if (payload is not OrderTicket order) return;
        if (InventorySystem.Instance == null) return;

        bool allPerfect = order.QTEScore != null && order.QTEScore.IsAllPerfect();
        int coffeeRevenue = Mathf.Max(0, order.CoffeePrice);
        int bonus = allPerfect ? perfectCraftBonus : 0;
        int total = coffeeRevenue + bonus;

        RuntimeGameRepository.AddGold(total);
        GameEvent.Emit(
            "RevenueAwarded",
            new RevenueAwardedPayload(
                order.OrderId,
                order.CoffeeName,
                coffeeRevenue,
                bonus,
                allPerfect));

        string bonusTag = allPerfect ? $" + {bonus} perfect bonus" : "";
        Debug.Log($"[Rewarder] Gold +{total} from {order.CoffeeName}{bonusTag}");
    }
}
