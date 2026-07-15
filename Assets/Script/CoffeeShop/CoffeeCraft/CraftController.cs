using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftController : MonoBehaviour
{
    [Header("UI Groups")]
    [SerializeField] private GameObject coffeeListGroup;
    [SerializeField] private GameObject coffeeMakeGroup;

    [Header("Step Buttons")]
    [SerializeField] private Button grindBtn;
    [SerializeField] private Button pourOverBtn;
    [SerializeField] private Button extractBtn;
    [SerializeField] private Button steamMilkBtn;
    [SerializeField] private Button addWaterBtn;
    [SerializeField] private Button addMilkBtn;
    [SerializeField] private Button addSugarBtn;

    [Header("Deliver")]
    [SerializeField] private Button deliverBtn;

    [Header("Back")]
    [SerializeField] private Button backButton;

    [Header("System")]
    [SerializeField] private OrderSystem orderSystem;
    [SerializeField] private CoffeeMachine coffeeMachine;

    private CoffeeData selectedCoffee;
    private CraftStep[] currentSteps;
    private int currentStepIndex;
    private bool _craftFailed;

    private readonly Dictionary<string, Button> _stepButtons = new();

    private void Awake()
    {
        _stepButtons["Grind"] = grindBtn;
        _stepButtons["PourOver"] = pourOverBtn;
        _stepButtons["Extract"] = extractBtn;
        _stepButtons["SteamMilk"] = steamMilkBtn;
        _stepButtons["AddWater"] = addWaterBtn;
        _stepButtons["AddMilk"] = addMilkBtn;
        _stepButtons["AddSugar"] = addSugarBtn;

        foreach (var kvp in _stepButtons)
        {
            if (kvp.Value != null)
            {
                var stepId = kvp.Key;
                kvp.Value.onClick.AddListener(() => OnStepClicked(stepId));
            }
        }

        if (deliverBtn != null)
            deliverBtn.onClick.AddListener(OnDeliverClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    public void OnCoffeeSelected(CoffeeData coffee)
    {
        selectedCoffee = coffee;
        currentStepIndex = 0;
        currentSteps = coffee.Steps;
        _craftFailed = false;

        SetAllButtonsInteractable(true);

        if (coffeeListGroup != null) coffeeListGroup.SetActive(false);
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);

        Debug.Log($"[CraftController] Start crafting: {coffee.coffeeName}, {currentSteps.Length} steps");
    }

    private void OnStepClicked(string stepId)
    {
        if (selectedCoffee == null || currentSteps == null) return;
        if (currentStepIndex >= currentSteps.Length) return;

        var step = currentSteps[currentStepIndex];

        if (step.id != stepId)
        {
            _craftFailed = true;
            Debug.Log($"[CraftController] Wrong step! Expected '{step.id}', got '{stepId}'.");
            return;
        }

        if (!string.IsNullOrEmpty(step.resourceId) && step.amount > 0)
        {
            var inv = InventorySystem.Instance;
            if (inv == null || !inv.Spend(step.resourceId, step.amount))
            {
                _craftFailed = true;
                Debug.Log($"[CraftController] Not enough {step.resourceId} for step {step.id}.");
                return;
            }
        }

        currentStepIndex++;
        Debug.Log($"[CraftController] Step {currentStepIndex}/{currentSteps.Length} ({step.id}) done");
    }

    private void OnDeliverClicked()
    {
        if (selectedCoffee == null || currentSteps == null) return;

        if (!_craftFailed && currentStepIndex >= currentSteps.Length)
        {
            Debug.Log($"[CraftController] Deliver success: {selectedCoffee.coffeeName}");

            if (orderSystem == null)
                orderSystem = FindFirstObjectByType<OrderSystem>();
            if (orderSystem != null)
                orderSystem.TryServeCoffee(selectedCoffee);
            else
                GameEvent.Emit("CoffeeServed", selectedCoffee);

            BackToList();
        }
        else
        {
            Debug.Log($"[CraftController] Deliver failed: {(_craftFailed ? "wrong steps" : "incomplete")}");
            BackToList();
        }
    }

    private void BackToList()
    {
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (coffeeListGroup != null) coffeeListGroup.SetActive(true);
        selectedCoffee = null;
        currentSteps = null;
        currentStepIndex = 0;
        _craftFailed = false;
    }

    private void OnBackClicked()
    {
        BackToList();
    }

    private void SetAllButtonsInteractable(bool value)
    {
        foreach (var kvp in _stepButtons)
        {
            if (kvp.Value != null)
                kvp.Value.interactable = value;
        }
    }
}
