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
        InventorySystem.Instance.Add("gold", order.CoffeePrice);
        Debug.Log($"[Rewarder] Gold +{order.CoffeePrice} from {order.CoffeeName}");
    }
}
