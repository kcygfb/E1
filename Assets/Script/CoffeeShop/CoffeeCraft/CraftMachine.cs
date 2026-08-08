using UnityEngine;
using UnityEngine.UI;
using KiKs.UI;

/// <summary>通用机器。1个MaterialSlot + 按钮 + 配方查找 → 在outputPoint生成MaterialIcon。
/// 子类 PourOverMachine 增加壶槽，产出进壶而非生成Icon。</summary>
public class CraftMachine : MonoBehaviour
{
    [Header("Config")]
    public string machineId;
    public string displayName;

    [Header("UI")]
    [SerializeField] protected Button startButton;
    [SerializeField] protected MaterialSlot materialSlot;
    [SerializeField] protected Transform outputPoint;
    [SerializeField] protected GameObject materialIconPrefab;

    protected CraftController craftController;
    private UnityEngine.UI.Image _buttonImage;
    private ButtonGlow _buttonGlow;
    private TutorialController _tutorialController;

    protected virtual void Awake()
    {
        craftController = FindFirstObjectByType<CraftController>();
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
        _buttonImage = startButton != null ? startButton.GetComponent<UnityEngine.UI.Image>() : null;
        _buttonGlow = startButton != null ? startButton.GetComponent<ButtonGlow>() : null;
    }

    protected virtual void Start()
    {
        RegisterTutorialCallout();
    }

    protected virtual void OnDestroy()
    {
        if (_tutorialController != null)
            _tutorialController.UnregisterJsonCallouts(this);
    }

    private void RegisterTutorialCallout()
    {
        if (startButton == null || string.IsNullOrWhiteSpace(machineId))
            return;

        _tutorialController = FindFirstObjectByType<TutorialController>();
        var loader = CoffeeDataLoader.Instance;
        if (_tutorialController == null || loader == null || !loader.IsLoaded)
            return;

        TutorialHintJson tutorial = null;
        foreach (var coffee in loader.GetAllCoffees())
        {
            if (coffee?.tutorial == null ||
                !string.Equals(coffee.tutorial.targetId, machineId,
                    System.StringComparison.OrdinalIgnoreCase))
                continue;

            tutorial = coffee.tutorial;
            break;
        }

        _tutorialController.RegisterJsonCallout(
            this, startButton.transform as RectTransform, tutorial);
    }

    protected virtual void Update()
    {
        if (startButton == null) return;
        bool can = CanStart();
        startButton.interactable = can;

        // 强制按钮在 disabled 状态下也不透明（覆盖 Button 的 disabledColor）
        var colors = startButton.colors;
        colors.disabledColor = new Color(1, 1, 1, 1); // disabled 也不透明
        startButton.colors = colors;

        if (_buttonGlow != null)
            _buttonGlow.SetOn(can); // 激活时开高光，不激活时关
    }

    protected virtual bool CanStart()
    {
        return materialSlot != null && materialSlot.IsFilled
            && craftController != null && !craftController.IsProcessing;
    }

    protected virtual void OnStartClicked()
    {
        if (!CanStart()) return;

        var allIds = materialSlot.GetAllMaterialIds();
        if (allIds == null || allIds.Count == 0) return;

        string outputId = null;

        // 先试单输入配方
        if (allIds.Count == 1 && MachineRecipeLibrary.TryGetOutput(machineId, allIds[0], out outputId))
        {
            // 单输入命中
        }
        // 再试多输入配方
        else if (!MachineRecipeLibrary.TryGetOutputMulti(machineId, allIds, out outputId))
        {
            outputId = "Unknown";
            Debug.Log($"[CraftMachine] {machineId} + [{string.Join(", ", allIds)}] 无配方，产出 Unknown");
        }

        // 消耗所有输入材料（销毁全部icon）
        materialSlot.Clear();

        ProduceOutput(outputId);
        craftController.OnMachineComplete(machineId);
    }

    /// <summary>产出材料。检测产出口是否有杯子，有杯子直接进杯子，没杯子放 outputPoint 生成可拖拽 icon。</summary>
    protected virtual void ProduceOutput(string outputMaterialId)
    {
        // 检测产出口附近是否有杯子
        var cup = FindCupAtOutput();
        if (cup != null)
        {
            if (materialIconPrefab == null)
                materialIconPrefab = CreateDefaultIconPrefab();

            var iconGO = Instantiate(materialIconPrefab, cup.transform, false);
            iconGO.transform.localPosition = Vector3.zero;
            var icon = iconGO.GetComponent<MaterialIcon>();
            if (icon == null) icon = iconGO.AddComponent<MaterialIcon>();
            icon.Setup(outputMaterialId);
            cup.AcceptIcon(icon);
            Debug.Log($"[CraftMachine] {machineId} 产出 {outputMaterialId} → 直接进杯子");
            return;
        }

        // 没杯子 → 在 outputPoint 生成可拖拽 icon
        if (outputPoint == null) outputPoint = transform;
        if (materialIconPrefab == null)
            materialIconPrefab = CreateDefaultIconPrefab();

        var freeIcon = Instantiate(materialIconPrefab, outputPoint, false);
        freeIcon.transform.localPosition = Vector3.zero;
        var freeMatIcon = freeIcon.GetComponent<MaterialIcon>();
        if (freeMatIcon == null) freeMatIcon = freeIcon.AddComponent<MaterialIcon>();
        freeMatIcon.Setup(outputMaterialId);
        Debug.Log($"[CraftMachine] {machineId} 产出: {outputMaterialId} (无杯子，放 outputPoint)");
    }

    /// <summary>检测产出口位置是否有杯子重叠。用 RectTransform bounds 相交判断。</summary>
    protected CupContainer FindCupAtOutput()
    {
        if (outputPoint == null) outputPoint = transform;
        var outputRT = outputPoint as RectTransform;
        if (outputRT == null) return null;

        var outputRect = GetWorldRect(outputRT);

        var cups = FindObjectsByType<CupContainer>(FindObjectsSortMode.None);
        foreach (var cup in cups)
        {
            if (cup == null) continue;
            var cupRT = cup.GetComponent<RectTransform>();
            if (cupRT == null) continue;
            var cupRect = GetWorldRect(cupRT);
            if (outputRect.Overlaps(cupRect))
                return cup;
        }
        return null;
    }

    private protected static Rect GetWorldRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = Mathf.Min(corners[0].x, corners[2].x);
        float maxX = Mathf.Max(corners[0].x, corners[2].x);
        float minY = Mathf.Min(corners[0].y, corners[2].y);
        float maxY = Mathf.Max(corners[0].y, corners[2].y);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private protected GameObject CreateDefaultIconPrefab()
    {
        var go = new GameObject("MaterialIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MaterialIcon));
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(108, 108);
        return go;
    }

    public virtual void ResetMachine()
    {
        materialSlot?.Clear();
    }
}
