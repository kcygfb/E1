using UnityEngine;
using UnityEngine.UI;

/// <summary>九宫格小料台 UI。使用场景中预制的 9 个 cell，MorningCheck 拖拽选材，Shop 拖拽出料。</summary>
public class TrayGridUI : MonoBehaviour
{
    public static TrayGridUI Instance { get; private set; }
    public bool IsDragMode => _isDragMode;

    [Header("Refs")]
    [SerializeField] private Button startShopBtn;
    [SerializeField] private GameObject materialPalette;
    [SerializeField] private RectTransform[] cells = new RectTransform[9];

    private RectTransform _panel;
    private Vector2 _inspectorPos;
    private bool _isDragMode;
    private bool _firstEnable = true;
    private PendingMode _pendingMode = PendingMode.None;

    private enum PendingMode { None, Selection, Drag }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _panel = GetComponent<RectTransform>();
        _inspectorPos = _panel.anchoredPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        // 防止 SetActive(true) 触发 OnEnable 时把模式重置回 Selection
        if (_firstEnable)
        {
            _firstEnable = false;
            ShowSelection();
        }
        else if (_pendingMode == PendingMode.Drag)
        {
            _pendingMode = PendingMode.None;
            ShowDragInternal();
        }
        else if (_pendingMode == PendingMode.Selection)
        {
            _pendingMode = PendingMode.None;
            ShowSelectionInternal();
        }
    }

    /// <summary>MorningCheck 阶段：显示 TrayGrid + MaterialPalette，格子可接收拖拽选材。</summary>
    public void ShowSelection()
    {
        if (!gameObject.activeSelf)
        {
            _pendingMode = PendingMode.Selection;
            gameObject.SetActive(true);
            return;
        }
        ShowSelectionInternal();
    }

    private void ShowSelectionInternal()
    {
        _isDragMode = false;
        IngredientTray.SetDefaults();

        gameObject.SetActive(true);
        if (materialPalette != null)
            materialPalette.SetActive(true);

        for (int i = 0; i < 9; i++)
        {
            if (cells[i] == null) continue;
            string matId = IngredientTray.GetSlot(i);
            SetCellMaterial(i, matId);
            SetCellCount(i, matId != null && InventorySystem.Instance != null
                ? InventorySystem.Instance.GetAmount(matId).ToString() : "");
            var drop = cells[i].GetComponent<TrayCellDrop>();
            if (drop != null) drop.enabled = true;
            // Enable drag so cell can be dragged out to clear
            var drag = cells[i].GetComponent<TraySlotDrag>();
            if (drag == null)
                drag = cells[i].gameObject.AddComponent<TraySlotDrag>();
            drag.SlotIndex = i;
            drag.MaterialId = matId;
            drag.enabled = true;
        }

        UpdateStartShopButton();
    }

    /// <summary>对话/空闲阶段：隐藏两个面板。</summary>
    public void HideAll()
    {
        gameObject.SetActive(false);
        if (materialPalette != null)
            materialPalette.SetActive(false);
    }

    /// <summary>制作阶段：只显示 TrayGrid，隐藏 MaterialPalette，格子可拖出材料到步骤按钮。</summary>
    public void ShowDrag()
    {
        if (!gameObject.activeSelf)
        {
            _pendingMode = PendingMode.Drag;
            gameObject.SetActive(true);
            return;
        }
        ShowDragInternal();
    }

    private void ShowDragInternal()
    {
        _isDragMode = true;

        gameObject.SetActive(true);
        if (materialPalette != null)
            materialPalette.SetActive(false);

        for (int i = 0; i < 9; i++)
        {
            if (cells[i] == null) continue;

            string matId = IngredientTray.GetSlot(i);
            SetCellMaterial(i, matId);

            int count = (!string.IsNullOrEmpty(matId) && InventorySystem.Instance != null) ? InventorySystem.Instance.GetAmount(matId) : 0;
            SetCellCount(i, count > 0 ? count.ToString() : "0");

            var drop = cells[i].GetComponent<TrayCellDrop>();
            if (drop != null) drop.enabled = false;

            var drag = cells[i].GetComponent<TraySlotDrag>();
            if (drag == null)
                drag = cells[i].gameObject.AddComponent<TraySlotDrag>();
            drag.MaterialId = matId;
            drag.SlotIndex = i;
            drag.enabled = true;
        }
    }

    public void RefreshCounts()
    {
        if (!_isDragMode) return;

        for (int i = 0; i < 9; i++)
        {
            if (cells[i] == null) continue;
            string matId = IngredientTray.GetSlot(i);
            int count = (!string.IsNullOrEmpty(matId) && InventorySystem.Instance != null) ? InventorySystem.Instance.GetAmount(matId) : 0;
            SetCellCount(i, count > 0 ? count.ToString() : "0");

            if (count <= 0)
            {
                var icon = cells[i].Find("Icon")?.GetComponent<Image>();
                if (icon != null) icon.color = new Color(0.15f, 0.15f, 0.15f, 0.3f);
            }
        }
    }

    /// <summary>MorningCheck 阶段：材料拖入格子时调用。同一种材料只能放入一个格子。</summary>
    public void OnMaterialDroppedOnCell(int cellIndex, string materialId)
    {
        if (_isDragMode || cellIndex < 0 || cellIndex >= 9) return;
        if (string.IsNullOrEmpty(materialId)) return;

        // 检查是否已在其他格子
        for (int i = 0; i < 9; i++)
        {
            if (i == cellIndex) continue;
            if (IngredientTray.GetSlot(i) == materialId)
            {
                Debug.Log($"[TrayGridUI] Material '{materialId}' already in slot {i}, cannot duplicate.");
                return;
            }
        }

        IngredientTray.SetSlot(cellIndex, materialId);
        SetCellMaterial(cellIndex, materialId);

        // Update drag component so it can be dragged out to clear
        var drag = cells[cellIndex].GetComponent<TraySlotDrag>();
        if (drag != null) drag.MaterialId = materialId;

        int count = (!string.IsNullOrEmpty(materialId) && InventorySystem.Instance != null) ? InventorySystem.Instance.GetAmount(materialId) : 0;
        SetCellCount(cellIndex, count > 0 ? count.ToString() : "0");

        UpdateStartShopButton();
    }

    /// <summary>MorningCheck 阶段：清空格子。</summary>
    public void ClearCell(int cellIndex)
    {
        if (cellIndex < 0 || cellIndex >= 9) return;
        IngredientTray.SetSlot(cellIndex, null);
        SetCellMaterial(cellIndex, null);
        SetCellCount(cellIndex, "");
        var drag = cells[cellIndex].GetComponent<TraySlotDrag>();
        if (drag != null) drag.MaterialId = null;
        UpdateStartShopButton();
    }

    private void SetCellMaterial(int index, string materialId)
    {
        if (cells[index] == null) return;
        var icon = cells[index].Find("Icon")?.GetComponent<Image>();
        if (icon == null) return;

        if (string.IsNullOrEmpty(materialId))
        {
            icon.sprite = null;
            icon.color = new Color(0.4f, 0.4f, 0.4f, 0.3f);
            return;
        }

        var sprite = MaterialDefinition.GetSpriteAll(materialId);
        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.color = Color.white;
        }
        else
        {
            var def = MaterialDefinition.Get(materialId);
            icon.color = def != null ? def.color : Color.gray;
        }
    }

    private void SetCellCount(int index, string text)
    {
        if (cells[index] == null) return;
        var count = cells[index].Find("Count")?.GetComponent<Text>();
        if (count != null) count.text = text;
    }

    private void UpdateStartShopButton()
    {
        if (startShopBtn != null)
            startShopBtn.interactable = IngredientTray.HasAny;
    }
}
