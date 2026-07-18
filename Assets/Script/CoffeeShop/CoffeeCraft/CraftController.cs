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

        if (coffeeListGroup != null) coffeeListGroup.SetActive(false);
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);

        GameEvent.Emit("CraftViewChanged", "CoffeeMake");
        UpdateButtonStates();

        Debug.Log($"[CraftController] Start crafting: {coffee.coffeeName}, {currentSteps.Length} steps");
    }

    private void OnStepClicked(string stepId)
    {
        if (selectedCoffee == null || currentSteps == null) return;
        if (currentStepIndex >= currentSteps.Length) return;

        var step = currentSteps[currentStepIndex];
        if (step.id != stepId) return;

        if (!string.IsNullOrEmpty(step.resourceId) && step.amount > 0)
        {
            var inv = InventorySystem.Instance;
            if (inv == null || !inv.Spend(step.resourceId, step.amount))
            {
                Debug.Log($"[CraftController] Not enough {step.resourceId} for step {step.id}.");
                return;
            }
        }

        currentStepIndex++;
        Debug.Log($"[CraftController] Step {currentStepIndex}/{currentSteps.Length} ({step.id}) done");
        UpdateButtonStates();
    }

    /// <summary>只亮当前步骤对应的按钮，其他全灰</summary>
    private void UpdateButtonStates()
    {
        // 先全灰
        foreach (var kvp in _stepButtons)
        {
            if (kvp.Value != null)
                kvp.Value.interactable = false;
        }

        // 交付按钮：全步骤完成才亮
        if (deliverBtn != null)
            deliverBtn.interactable = (currentStepIndex >= currentSteps.Length);

        // 没完成时亮当前步骤对应的按钮
        if (currentStepIndex < currentSteps.Length)
        {
            var currentStepId = currentSteps[currentStepIndex].id;
            if (_stepButtons.TryGetValue(currentStepId, out var btn) && btn != null)
                btn.interactable = true;
        }
    }

    private void OnDeliverClicked()
    {
        if (selectedCoffee == null || currentSteps == null) return;
        if (currentStepIndex < currentSteps.Length) return;

        Debug.Log($"[CraftController] Deliver success: {selectedCoffee.coffeeName}");

        if (orderSystem == null)
            orderSystem = FindFirstObjectByType<OrderSystem>();
        if (orderSystem != null)
            orderSystem.TryServeCoffee(selectedCoffee);
        else
            GameEvent.Emit("CoffeeServed", selectedCoffee);

        BackToList();
    }

    private void BackToList()
    {
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (coffeeListGroup != null) coffeeListGroup.SetActive(true);
        selectedCoffee = null;
        currentSteps = null;
        currentStepIndex = 0;

        GameEvent.Emit("CraftViewChanged", "Menu");
    }

    private void OnBackClicked()
    {
        BackToList();
    }
}
