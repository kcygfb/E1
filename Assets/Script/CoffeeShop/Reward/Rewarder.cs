using UnityEngine;

public class Rewarder : MonoBehaviour
{
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

        float multiplier = 1f;
        bool allPerfect = false;

        if (order.QTEScore != null)
        {
            multiplier = order.QTEScore.GetMultiplier();
            allPerfect = order.QTEScore.IsAllPerfect();
            if (allPerfect) multiplier *= 1.5f;
        }

        int gold = Mathf.RoundToInt(order.CoffeePrice * multiplier);
        InventorySystem.Instance.Add("gold", gold);

        string bonusTag = allPerfect ? " [ALL PERFECT!]" : "";
        Debug.Log($"[Rewarder] Gold +{gold} ({multiplier:F1}x) from {order.CoffeeName}{bonusTag}");
    }
}
