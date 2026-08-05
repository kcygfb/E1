using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StepDef
{
    public string id;
    public string displayName;
    public string resourceId;
    public int amount;
    public string qteType;
    public bool useMaterialSelector;
}

public class CraftController : MonoBehaviour
{
    [Header("UI Groups")]
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

    [Header("Reset")]
    [SerializeField] private Button resetBtn;

    [Header("System")]
    [SerializeField] private OrderSystem orderSystem;

    [Header("QTE Controllers")]
    [SerializeField] private RhythmTapQTE rhythmTapQTE;
    [SerializeField] private HoldReleaseQTE holdReleaseQTE;
    [SerializeField] private RapidTapQTE rapidTapQTE;
    [SerializeField] private RotationStopQTE rotationStopQTE;
    [SerializeField] private DropStopQTE dropStopQTE;

    [Header("Portafilter")]
    [SerializeField] private Portafilter portafilter;
    [SerializeField] private Transform cupSpawnParent;
    [SerializeField] private GameObject cupPrefab; // Cup 预制体

    private readonly List<string> _performedSteps = new();
    private readonly List<string> _usedMaterials = new();
    private QTEScoreResult _qteScore;
    private bool _waitingForQTE;
    private bool _isCrafting;
    private string _currentStepId;
    private string _currentMaterialId;
    private Portafilter _grindPortafilter;
    private CupContainer _currentCup;

    private readonly Dictionary<string, Button> _stepButtons = new();
    private readonly Dictionary<string, StepDef> _stepDefs = new();

    private void Awake()
    {
        _stepButtons["Grind"] = grindBtn;
        _stepButtons["PourOver"] = pourOverBtn;
        _stepButtons["Extract"] = extractBtn;
        _stepButtons["SteamMilk"] = steamMilkBtn;
        _stepButtons["AddWater"] = addWaterBtn;
        _stepButtons["AddMilk"] = addMilkBtn;
        _stepButtons["AddSugar"] = addSugarBtn;

        _stepDefs["Grind"] = new StepDef { id = "Grind", displayName = "研磨", resourceId = "", amount = 0, qteType = "RotationStop" };
        _stepDefs["PourOver"] = new StepDef { id = "PourOver", displayName = "手冲注水", resourceId = "", amount = 0, qteType = "HoldRelease" };
        _stepDefs["Extract"] = new StepDef { id = "Extract", displayName = "萃取浓缩", resourceId = "", amount = 0, qteType = "DropStop" };
        _stepDefs["SteamMilk"] = new StepDef { id = "SteamMilk", displayName = "打发奶泡", resourceId = "", amount = 0, qteType = "RhythmTap" };
        _stepDefs["AddWater"] = new StepDef { id = "AddWater", displayName = "加水稀释", resourceId = "", amount = 0, qteType = "" };
        _stepDefs["AddMilk"] = new StepDef { id = "AddMilk", displayName = "加入牛奶", resourceId = "", amount = 0, qteType = "" };
        _stepDefs["AddSugar"] = new StepDef { id = "AddSugar", displayName = "加入糖浆", resourceId = "", amount = 0, qteType = "" };

        // Deliver and Reset still use onClick
        if (deliverBtn != null)
            deliverBtn.onClick.AddListener(OnDeliverClicked);

        if (resetBtn != null)
            resetBtn.onClick.AddListener(ResetCraft);

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

    private void OnEnable()
    {
        GameEvent.On("OrderCreated", OnOrderCreated);
    }

    private void OnDisable()
    {
        GameEvent.Off("OrderCreated", OnOrderCreated);
    }

    private void OnOrderCreated(object payload)
    {
        StartFreeCraft();
    }

    private void StartFreeCraft()
    {
        _performedSteps.Clear();
        _usedMaterials.Clear();
        _qteScore = new QTEScoreResult();
        _waitingForQTE = false;
        _isCrafting = true;

        var coffeeList = GameObject.Find("CoffeeList");
        if (coffeeList != null) coffeeList.SetActive(false);

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.ShowDrag();
        GameEvent.Emit("CraftViewChanged", "CoffeeMake");
        UpdateButtonStates();

        Debug.Log("[CraftController] Free craft started");
    }

    /// <summary>从杯子堆拖出一个杯子 → 在工作区生成 CupContainer。</summary>
    public void OnCupDraggedOut(Vector2 screenPosition, Sprite cupSprite)
    {
        if (!_isCrafting) return;
        if (cupSpawnParent == null) cupSpawnParent = transform;

        // 从预制体实例化，没有预制体则代码生成 fallback
        GameObject cupGO;
        if (cupPrefab != null)
            cupGO = Instantiate(cupPrefab, cupSpawnParent, false);
        else
            cupGO = new GameObject("Cup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CupContainer));

        cupGO.transform.SetParent(cupSpawnParent, false);
        cupGO.name = "Cup";

        // 设置位置（用世界坐标避免 parent 偏移问题）
        cupGO.transform.position = screenPosition;

        // 设置 sprite
        var img = cupGO.GetComponent<Image>();
        if (img != null)
        {
            if (cupSprite != null) img.sprite = cupSprite;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;
        }

        // 确保有 CupContainer 组件
        if (cupGO.GetComponent<CupContainer>() == null)
            cupGO.AddComponent<CupContainer>();

        Debug.Log("[CraftController] Cup spawned in workspace");
    }

    /// <summary>手柄拖到步骤按钮（Grind/Extract 等）。</summary>
    public void OnPortafilterDroppedOnStep(string stepId, Portafilter pf)
    {
        if (!_isCrafting || _waitingForQTE) return;
        if (!_stepDefs.TryGetValue(stepId, out var def)) return;

        if (stepId == "Grind" && pf.CurrentState == Portafilter.State.HasMaterial)
        {
            _currentStepId = stepId;
            _currentMaterialId = pf.MaterialId;
            _grindPortafilter = pf;
            StartStepQTE(def);
        }
        else if (stepId == "Extract" && pf.CurrentState == Portafilter.State.Ground)
        {
            _currentStepId = stepId;
            _currentMaterialId = pf.MaterialId;
            _grindPortafilter = pf;
            StartStepQTE(def);
        }
        else
        {
            Debug.Log($"[CraftController] Portafilter state {pf.CurrentState} not valid for step {stepId}");
        }
    }

    /// <summary>手柄停靠到 PortafilterDock（磨豆机/萃取机停留区）。</summary>
    public void OnPortafilterDocked(string stepId, Portafilter pf)
    {
        if (!_isCrafting) return;
        if (!_stepDefs.TryGetValue(stepId, out var def)) return;

        // 检查手柄状态是否匹配该机器
        bool valid = (stepId == "Grind" && pf.CurrentState == Portafilter.State.HasMaterial)
                  || (stepId == "Extract" && pf.CurrentState == Portafilter.State.Ground);

        if (valid)
        {
            Debug.Log($"[CraftController] Portafilter docked at {stepId}, state={pf.CurrentState} — ready to activate");
            // TODO: 这里可以触发机器的"检测词条 on"视觉反馈
            // 例如：高亮机器、显示"就绪"文字等
        }
        else
        {
            Debug.Log($"[CraftController] Portafilter docked at {stepId} but state={pf.CurrentState} not valid");
        }
    }

    /// <summary>杯子拖到步骤按钮（Extract/PourOver 等）。</summary>
    public void OnCupDroppedOnStep(string stepId, CupContainer cup)
    {
        if (!_isCrafting || _waitingForQTE) return;
        if (!_stepDefs.TryGetValue(stepId, out var def)) return;

        _currentStepId = stepId;
        _currentMaterialId = cup.Contents.Count > 0 ? cup.Contents[0] : "";
        _currentCup = cup;
        StartStepQTE(def);
    }

    /// <summary>杯子拖到 Deliver 按钮 = 提交。</summary>
    public void OnCupDelivered(CupContainer cup)
    {
        if (!_isCrafting) return;
        OnDeliverClicked();
    }

    /// <summary>拖拽材料到步骤按钮时调用。</summary>
    public void OnMaterialDroppedOnStep(string stepId, string materialId)
    {
        if (!_isCrafting || _waitingForQTE) return;
        if (!_stepDefs.TryGetValue(stepId, out var def)) return;
        if (string.IsNullOrEmpty(materialId)) return;

        // Spend 1 from inventory
        var inv = InventorySystem.Instance;
        if (inv == null || !inv.Spend(materialId, 1))
        {
            Debug.Log($"[CraftController] Not enough {materialId} for step {stepId}.");
            return;
        }

        // Refresh tray UI counts
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.RefreshCounts();

        _currentStepId = stepId;
        _currentMaterialId = materialId;

        Debug.Log($"[CraftController] Material '{materialId}' dropped on step '{stepId}'");

        StartStepQTE(def);
    }

    private void StartStepQTE(StepDef def)
    {
        if (!string.IsNullOrEmpty(def.qteType))
        {
            _waitingForQTE = true;
            LaunchQTE(def.qteType, def.id, def.displayName);
        }
        else
        {
            CompleteStep(def.id);
        }
    }

    private void LaunchQTE(string qteType, string stepId, string displayName)
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
            CompleteStep(stepId);
            return;
        }

        Debug.Log($"[CraftController] Launch QTE: {qteType} for step {stepId} ({displayName})");
        qte.Show(stepId, displayName);
    }

    private void OnQTEComplete(QTERating rating)
    {
        if (!_waitingForQTE) return;

        if (_qteScore != null && !string.IsNullOrEmpty(_currentStepId))
            _qteScore.Record(_currentStepId, rating);
        Debug.Log($"[CraftController] QTE result for {_currentStepId}: {rating}");

        _waitingForQTE = false;

        // 研磨完成 → 手柄变 Ground
        if (_currentStepId == "Grind" && _grindPortafilter != null)
        {
            _grindPortafilter.SetGround();
            _grindPortafilter = null;
        }

        // 萃取完成 → 手柄清空
        if (_currentStepId == "Extract" && _grindPortafilter != null)
        {
            _grindPortafilter.Clear();
            _grindPortafilter = null;
        }

        CompleteStep(_currentStepId);
    }

    private void CompleteStep(string stepId)
    {
        _performedSteps.Add(stepId);
        _usedMaterials.Add(_currentMaterialId ?? "");
        Debug.Log($"[CraftController] Step done: {stepId} (total: {_performedSteps.Count})");
        _currentMaterialId = null;
        UpdateButtonStates();
    }

    /// <summary>检测已执行步骤的尾部是否匹配某个咖啡配方（允许前面有多余步骤）</summary>
    private CoffeeDataJson MatchCoffeeBySteps()
    {
        if (CoffeeDataLoader.Instance == null || !CoffeeDataLoader.Instance.IsLoaded) return null;

        foreach (var coffee in CoffeeDataLoader.Instance.GetAllCoffees())
        {
            if (coffee.steps == null || coffee.steps.Count == 0) continue;
            if (coffee.steps.Count > _performedSteps.Count) continue;

            int offset = _performedSteps.Count - coffee.steps.Count;
            bool match = true;
            for (int i = 0; i < coffee.steps.Count; i++)
            {
                if (coffee.steps[i].id != _performedSteps[offset + i])
                {
                    match = false;
                    break;
                }
            }

            if (match) return coffee;
        }

        return null;
    }

    private void UpdateButtonStates()
    {
        foreach (var kvp in _stepButtons)
        {
            if (kvp.Value != null)
                kvp.Value.interactable = _isCrafting && !_waitingForQTE;
        }

        if (deliverBtn != null)
            deliverBtn.interactable = _isCrafting && _performedSteps.Count > 0 && !_waitingForQTE;

        if (resetBtn != null)
            resetBtn.interactable = _isCrafting && _performedSteps.Count > 0 && !_waitingForQTE;
    }

    private void OnDeliverClicked()
    {
        var matched = MatchCoffeeBySteps();
        if (matched == null)
        {
            Debug.Log("[CraftController] No coffee recipe matches the performed steps.");
            return;
        }

        Debug.Log($"[CraftController] Crafted: {matched.coffeeName}");

        if (orderSystem == null)
            orderSystem = FindFirstObjectByType<OrderSystem>();

        if (orderSystem != null && orderSystem.HasActiveOrder)
        {
            var order = orderSystem.ActiveOrder;
            if (order != null && _qteScore != null)
                order.QTEScore = _qteScore;

            if (order != null && order.CoffeeId == matched.coffeeId)
            {
                var coffeeData = ScriptableObject.CreateInstance<CoffeeData>();
                coffeeData.ApplyJson(matched);
                orderSystem.TryServeCoffee(coffeeData);
                Debug.Log($"[CraftController] Correct coffee! Served {matched.coffeeName}");
                EndCraft();
            }
            else
            {
                Debug.Log($"[CraftController] Wrong coffee! Made {matched.coffeeName} but order wants {(order != null ? order.CoffeeName : "?")}");
                ResetCraft();
            }
        }
    }

    private void ResetCraft()
    {
        _performedSteps.Clear();
        _usedMaterials.Clear();
        _qteScore = new QTEScoreResult();
        _waitingForQTE = false;
        _currentStepId = null;
        _currentMaterialId = null;
        _grindPortafilter = null;
        _currentCup = null;
        UpdateButtonStates();
        Debug.Log("[CraftController] Craft reset — try again");
    }

    private void EndCraft()
    {
        _isCrafting = false;
        _performedSteps.Clear();
        _usedMaterials.Clear();
        _qteScore = null;
        _waitingForQTE = false;
        _currentStepId = null;
        _currentMaterialId = null;
        _grindPortafilter = null;
        _currentCup = null;

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.HideAll();
        GameEvent.Emit("CraftViewChanged", "Menu");
    }
}
