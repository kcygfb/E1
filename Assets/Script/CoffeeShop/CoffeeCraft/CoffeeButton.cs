using UnityEngine;
using UnityEngine.UI;

public class CoffeeButton : MonoBehaviour
{
    public CoffeeData coffeeData;
    public OrderSystem orderSystem;
    public CraftController craftController;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        if (coffeeData == null) return;

        if (UnlockManager.Instance != null && !UnlockManager.Instance.IsUnlocked(coffeeData))
        {
            Debug.Log($"[CoffeeButton] {coffeeData.coffeeName} is locked");
            return;
        }

        if (craftController != null)
        {
            craftController.OnCoffeeSelected(coffeeData);
            return;
        }

        if (orderSystem == null)
            orderSystem = FindFirstObjectByType<OrderSystem>();

        if (orderSystem != null)
            orderSystem.TryServeCoffee(coffeeData);
    }
}
