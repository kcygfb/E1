using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoffeeMakeController : MonoBehaviour
{
    [Header("UI Groups")]
    [SerializeField] private GameObject coffeeListGroup;
    [SerializeField] private GameObject coffeeMakeGroup;

    [Header("Step Buttons")]
    [SerializeField] private Button[] stepButtons;

    [Header("Deliver")]
    [SerializeField] private GameObject deliverButton;
    [SerializeField] private Button deliverBtn;

    [Header("Back")]
    [SerializeField] private Button backButton;

    [Header("System")]
    [SerializeField] private OrderSystem orderSystem;
    [SerializeField] private CoffeeMachine coffeeMachine;

    private CoffeeData selectedCoffee;
    private readonly HashSet<int> completedSteps = new HashSet<int>();
    private int totalSteps;

    private void Start()
    {
        totalSteps = stepButtons != null ? stepButtons.Length : 0;

        if (stepButtons != null)
        {
            for (int i = 0; i < stepButtons.Length; i++)
            {
                int index = i;
                stepButtons[i].onClick.AddListener(() => OnStepClicked(index));
            }
        }

        if (deliverBtn != null)
            deliverBtn.onClick.AddListener(OnDeliverClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (deliverButton != null) deliverButton.SetActive(false);
        if (coffeeListGroup != null) coffeeListGroup.SetActive(true);
    }

    public void OnCoffeeSelected(CoffeeData coffee)
    {
        selectedCoffee = coffee;

        completedSteps.Clear();

        if (stepButtons != null)
        {
            foreach (var btn in stepButtons)
                btn.interactable = true;
        }

        if (deliverButton != null) deliverButton.SetActive(false);

        if (coffeeListGroup != null) coffeeListGroup.SetActive(false);
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);
    }

    private void OnStepClicked(int stepIndex)
    {
        if (completedSteps.Contains(stepIndex))
            return;

        completedSteps.Add(stepIndex);

        if (stepButtons != null && stepIndex < stepButtons.Length)
            stepButtons[stepIndex].interactable = false;

        if (completedSteps.Count >= totalSteps)
        {
            if (deliverButton != null) deliverButton.SetActive(true);
        }
    }

    private void OnDeliverClicked()
    {
        if (selectedCoffee == null)
            return;

        if (coffeeMachine != null)
        {
            coffeeMachine.MakeCoffee(selectedCoffee);
        }
        else
        {
            if (orderSystem == null)
                orderSystem = FindFirstObjectByType<OrderSystem>();
            if (orderSystem != null)
                orderSystem.TryServeCoffee(selectedCoffee);
        }

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (deliverButton != null) deliverButton.SetActive(false);
        if (coffeeListGroup != null) coffeeListGroup.SetActive(true);

        selectedCoffee = null;
        completedSteps.Clear();
    }

    private void OnBackClicked()
    {
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (deliverButton != null) deliverButton.SetActive(false);
        if (coffeeListGroup != null) coffeeListGroup.SetActive(true);

        selectedCoffee = null;
        completedSteps.Clear();
    }
}
