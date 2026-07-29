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

    [Header("QTE Controllers (拖到 Canvas 下的 QTE GameObject)")]
    [SerializeField] private RhythmTapQTE rhythmTapQTE;
    [SerializeField] private HoldReleaseQTE holdReleaseQTE;
    [SerializeField] private RapidTapQTE rapidTapQTE;
    [SerializeField] private RotationStopQTE rotationStopQTE;
    [SerializeField] private DropStopQTE dropStopQTE;

    private CoffeeData selectedCoffee;
    private CraftStep[] currentSteps;
    private int currentStepIndex;
    private QTEScoreResult _qteScore;
    private bool _waitingForQTE;

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

        // 注册 QTE 完成回调
        if (rhythmTapQTE != null)
            rhythmTapQTE.OnQTEDone.AddListener(r => OnQTEComplete(r));
        if (holdReleaseQTE != null)
            holdReleaseQTE.OnQTEDone.AddListener(r => OnQTEComplete(r));
        if (rapidTapQTE != null)
            rapidTapQTE.OnQTEDone.AddListener(r => OnQTEComplete(r));
        if (rotationStopQTE != null)
            rotationStopQTE.OnQTEDone.AddListener(r => OnQTEComplete(r));
        if (dropStopQTE != null)
            dropStopQTE.OnQTEDone.AddListener(r => OnQTEComplete(r));
    }

    public void OnCoffeeSelected(CoffeeData coffee)
    {
        if (!IsCraftingAllowed())
        {
            Debug.Log("[CraftController] Cannot enter crafting: shop not open or no active order");
            return;
        }

        selectedCoffee = coffee;
        currentStepIndex = 0;
        currentSteps = coffee.Steps;
        _qteScore = new QTEScoreResult();
        _waitingForQTE = false;

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
        if (_waitingForQTE) return;

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

        // 启动 QTE（如果有配置）
        if (!string.IsNullOrEmpty(step.qteType))
        {
            _waitingForQTE = true;
            LaunchQTE(step.qteType, step.id);
        }
        else
        {
            // 无 QTE 的步骤直接推进
            AdvanceStep();
        }
    }

    private void LaunchQTE(string qteType, string stepId)
    {
        QTEBase qte = qteType switch
        {
            "RhythmTap" => rhythmTapQTE,
            "HoldRelease" => holdReleaseQTE,
            "RapidTap" => rapidTapQTE,
            "RotationStop" => rotationStopQTE,
            "DropStop" => dropStopQTE,
            _ => null
        };

        if (qte == null)
        {
            Debug.LogWarning($"[CraftController] QTE type '{qteType}' not assigned, skipping.");
            AdvanceStep();
            return;
        }

        string displayName = currentSteps[currentStepIndex].displayName;
        Debug.Log($"[CraftController] Launch QTE: {qteType} for step {stepId} ({displayName})");
        qte.Show(stepId, displayName);
    }

    private void OnQTEComplete(QTERating rating)
    {
        if (!_waitingForQTE || selectedCoffee == null || currentSteps == null) return;

        var step = currentSteps[currentStepIndex];
        _qteScore.Record(step.id, rating);
        Debug.Log($"[CraftController] QTE result for {step.id}: {rating}");

        _waitingForQTE = false;
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        currentStepIndex++;
        Debug.Log($"[CraftController] Step {currentStepIndex}/{currentSteps.Length} done");
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

        // QTE 进行中不亮任何按钮
        if (_waitingForQTE) return;

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

        if (orderSystem != null && orderSystem.HasActiveOrder)
        {
            // 将 QTE 评分附加到订单
            var order = orderSystem.ActiveOrder;
            if (order != null && _qteScore != null)
                order.QTEScore = _qteScore;
        }

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
        _qteScore = null;
        _waitingForQTE = false;

        GameEvent.Emit("CraftViewChanged", "Menu");
    }

    private void OnBackClicked()
    {
        BackToList();
    }

    private bool IsCraftingAllowed()
    {
        var timeSystem = FindFirstObjectByType<TimeSystem>();
        if (timeSystem != null && timeSystem.CurrentPhase != DayPhase.Shop)
            return false;

        if (orderSystem == null)
            orderSystem = FindFirstObjectByType<OrderSystem>();
        if (orderSystem != null && !orderSystem.HasActiveOrder)
            return false;

        return true;
    }
}
