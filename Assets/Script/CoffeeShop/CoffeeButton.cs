using UnityEngine;
using UnityEngine.UI;

public class CoffeeButton : MonoBehaviour
{
    public CoffeeData coffeeData;
    public OrderSystem orderSystem;
    public CoffeeMakeController coffeeMakeController;

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
        if (coffeeMakeController != null && coffeeData != null)
        {
            coffeeMakeController.OnCoffeeSelected(coffeeData);
            return;
        }

        if (orderSystem == null)
            orderSystem = FindFirstObjectByType<OrderSystem>();

        if (orderSystem != null && coffeeData != null)
            orderSystem.TryServeCoffee(coffeeData);
    }
}
