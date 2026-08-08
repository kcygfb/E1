using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>制作总控。开始/重置/提交/内容物匹配。</summary>
public class CraftController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject coffeeMakeGroup;
    [SerializeField] private Button deliverBtn;
    [SerializeField] private Button resetBtn;
    [Header("System")]
    [SerializeField] private OrderSystem orderSystem;
    [SerializeField] private Transform cupSpawnParent;

    public bool IsProcessing { get; set; }
    public bool HasActiveCup { get; set; }

    private bool _isCrafting;

    private void Awake()
    {
        if (deliverBtn != null) deliverBtn.onClick.AddListener(() => OnDeliverBtnClicked());
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetCraft);
    }

    private void OnEnable()
    {
        GameEvent.On("OrderCreated", OnOrderCreated);
        GameEvent.On("DialogueRequested", OnDialogueRequested);
        GameEvent.On("DialogueEnded", OnDialogueEnded);
    }
    private void OnDisable()
    {
        GameEvent.Off("OrderCreated", OnOrderCreated);
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
        GameEvent.Off("DialogueEnded", OnDialogueEnded);
    }

    private void OnDialogueRequested(object payload)
    {
        if (!_isCrafting) return;
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.HideAll();
    }

    private void OnDialogueEnded(object payload)
    {
        if (payload is not string context) return;
        if (context != "wrong_coffee") return;
        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.ShowDrag();
        ResetCraft();
    }

    private void OnOrderCreated(object payload) => StartCraft();

    private void StartCraft()
    {
        IsProcessing = false;
        HasActiveCup = false;
        _isCrafting = true;

        var coffeeList = GameObject.Find("CoffeeList");
        if (coffeeList != null) coffeeList.SetActive(false);

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(true);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.ShowDrag();
        UpdateButtonStates();
    }

    // === 机器回调 ===
    public void OnMachineComplete(string machineId)
    {
        // 不再记录步骤序列，只用于互斥锁
        UpdateButtonStates();
    }

    // === 杯子相关 ===
    public void OnCupDraggedOut(Vector2 screenPosition, Sprite cupSprite, GameObject cupPrefab)
    {
        if (!_isCrafting || HasActiveCup) return;
        if (cupSpawnParent == null) cupSpawnParent = transform;

        GameObject cupGO;
        if (cupPrefab != null)
        {
            cupGO = Instantiate(cupPrefab, cupSpawnParent, false);
            var rt = cupGO.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(80 * 1.7f, 80 * 1.7f);
        }
        else
        {
            cupGO = new GameObject("Cup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CupContainer));
            cupGO.transform.SetParent(cupSpawnParent, false);
            var img = cupGO.GetComponent<Image>();
            if (cupSprite != null) { img.sprite = cupSprite; img.preserveAspect = true; img.color = Color.white; }
            img.raycastTarget = true;
            var rt = cupGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80 * 1.7f, 80 * 1.7f);
        }
        cupGO.name = "Cup";
        cupGO.transform.position = screenPosition;

        HasActiveCup = true;
    }

    public void OnCupDelivered(CupContainer cup)
    {
        if (!_isCrafting) return;
        Deliver(cup);
    }

    public void OnCupReturned(CupContainer cup)
    {
        if (cup == null) return;
        cup.ClearContents();
        Destroy(cup.gameObject);
        HasActiveCup = false;
    }

    private void OnDeliverBtnClicked()
    {
        if (!_isCrafting || !HasActiveCup) return;
        var cup = FindFirstObjectByType<CupContainer>();
        if (cup != null) Deliver(cup);
    }

    // === 判定 ===
    private void Deliver(CupContainer cup)
    {
        string coffeeId = cup.MergedCoffeeId;
        CoffeeDataJson matched = null;

        if (!string.IsNullOrEmpty(coffeeId))
            CoffeeDataLoader.Instance?.TryGetCoffee(coffeeId, out matched);
        else
            matched = MatchCoffeeByContents(cup.Contents);

        if (orderSystem == null) orderSystem = FindFirstObjectByType<OrderSystem>();

        if (orderSystem != null && orderSystem.HasActiveOrder)
        {
            var order = orderSystem.ActiveOrder;

            if (matched != null && order != null && order.CoffeeId == matched.coffeeId)
            {
                var coffeeData = ScriptableObject.CreateInstance<CoffeeData>();
                coffeeData.ApplyJson(matched);
                orderSystem.TryServeCoffee(coffeeData);
                EndCraft();
                return;
            }

            Debug.Log($"[CraftController] 做了 {matched?.coffeeId ?? "未知"}，订单要 {order?.CoffeeId}");
            TriggerWrongCoffeeFeedback(order);
            return;
        }

        if (matched == null)
            Debug.Log("[CraftController] 没有匹配的咖啡配方");
        ResetCraft();
    }

    private void TriggerWrongCoffeeFeedback(OrderTicket order)
    {
        string npcName = order?.NpcName ?? "Customer";
        Color speakerColor = Color.white;
        if (order?.Owner != null && order.Owner.NPCData != null)
            speakerColor = order.Owner.NPCData.speakerColor;

        var tokens = new Dictionary<string, string>
        {
            { "coffee", order?.CoffeeName ?? "coffee" }
        };

        GameEvent.Emit("DialogueRequested",
            new DialogueRequest("generic_wrongcoffee", "wrong_coffee", tokens, npcName, speakerColor));
    }

    private CoffeeDataJson MatchCoffeeByContents(List<string> contents)
    {
        if (CoffeeDataLoader.Instance == null || !CoffeeDataLoader.Instance.IsLoaded) return null;

        var cupSet = new HashSet<string>(contents);
        foreach (var coffee in CoffeeDataLoader.Instance.GetAllCoffees())
        {
            if (coffee.requiredMaterials == null || coffee.requiredMaterials.Count == 0) continue;
            var recipeSet = new HashSet<string>(coffee.requiredMaterials);
            if (cupSet.SetEquals(recipeSet)) return coffee;
        }
        return null;
    }

    // === 状态管理 ===
    private UnityEngine.UI.Image _deliverImage;

    private void Update()
    {
        if (deliverBtn == null) return;
        // 有杯子时不透明可点，没杯子时半透明不可点
        bool hasCup = _isCrafting && HasActiveCup;
        deliverBtn.interactable = hasCup;
        if (_deliverImage == null)
            _deliverImage = deliverBtn.GetComponent<UnityEngine.UI.Image>();
        if (_deliverImage != null)
            _deliverImage.color = hasCup ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.4f);
    }

    private void UpdateButtonStates()
    {
        if (resetBtn != null)
            resetBtn.interactable = _isCrafting;
    }

    public void ResetCraft()
    {
        IsProcessing = false;
        HasActiveCup = false;

        // 销毁杯子（含其子 MaterialIcon）
        foreach (var cup in FindObjectsByType<CupContainer>(FindObjectsSortMode.None))
        {
            cup.ClearContents();
            Destroy(cup.gameObject);
        }

        // 重置机器（清空 MaterialSlot 内的 icon）
        foreach (var machine in FindObjectsByType<CraftMachine>(FindObjectsSortMode.None))
            machine.ResetMachine();

        // 清空水壶
        foreach (var kettle in FindObjectsByType<Kettle>(FindObjectsSortMode.None))
        {
            kettle.Contents.Clear();
            foreach (var icon in kettle.Icons)
                if (icon != null) Destroy(icon.gameObject);
            kettle.Icons.Clear();
        }

        // 清空 KettleSlot 引用
        foreach (var slot in FindObjectsByType<KettleSlot>(FindObjectsSortMode.None))
            slot.Clear();

        // 销毁所有游离的 MaterialIcon（不在 Slot/Cup/Kettle/Canvas层级里的）
        var canvas = GameObject.Find("Canvas");
        foreach (var icon in FindObjectsByType<MaterialIcon>(FindObjectsSortMode.None))
        {
            if (icon == null) continue;
            var parent = icon.transform.parent;
            // 如果父级是 Canvas（拖出后游离）或 null，销毁
            if (parent == null || (canvas != null && parent == canvas.transform))
                Destroy(icon.gameObject);
        }

        UpdateButtonStates();
    }

    private void EndCraft()
    {
        _isCrafting = false;
        IsProcessing = false;
        HasActiveCup = false;

        foreach (var cup in FindObjectsByType<CupContainer>(FindObjectsSortMode.None))
        {
            cup.ClearContents();
            Destroy(cup.gameObject);
        }

        if (coffeeMakeGroup != null) coffeeMakeGroup.SetActive(false);
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.HideAll();
    }
}
