using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>咖啡手柄。纯可拖动 + 可接收材料。状态变化由外部触发。
/// 拖到 PortafilterDock 上时吸附到停靠点。</summary>
[RequireComponent(typeof(Image))]
public class Portafilter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public enum State { Empty, HasMaterial, Ground }

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite hasMaterialSprite;
    [SerializeField] private Sprite groundSprite;

    [Header("State")]
    public State CurrentState = State.Empty;
    public string MaterialId { get; private set; }

    /// <summary>当前停靠的 Dock（null = 未停靠）。</summary>
    public PortafilterDock CurrentDock { get; private set; }

    private Image _image;
    private Vector3 _dragStartWorldPos;

    private void Awake()
    {
        _image = GetComponent<Image>();
        UpdateVisual();
    }

    // --- 拖动 ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartWorldPos = transform.position;
        transform.SetAsLastSibling();

        // 通知当前停靠区手柄被拖走
        if (CurrentDock != null)
        {
            CurrentDock.OnPortafilterLeft();
            CurrentDock = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 优先检测 PortafilterDock
        PortafilterDock dock = null;
        foreach (var r in results)
        {
            dock = r.gameObject.GetComponent<PortafilterDock>();
            if (dock != null) break;
        }

        if (dock != null)
        {
            // 吸附到停靠点（OnDrop 会在 dock 侧处理）
            return;
        }

        // 其次检测 StepDropTarget（直接拖到步骤按钮）
        StepDropTarget target = null;
        foreach (var r in results)
        {
            target = r.gameObject.GetComponent<StepDropTarget>();
            if (target != null) break;
        }

        if (target != null)
        {
            var cc = FindFirstObjectByType<CraftController>();
            if (cc != null) cc.OnPortafilterDroppedOnStep(target.stepId, this);
            transform.position = _dragStartWorldPos;
        }
        // 否则留在松手位置
    }

    // --- 接收材料 ---
    public void OnDrop(PointerEventData eventData)
    {
        if (CurrentState != State.Empty) return;

        string matId = null;
        var trayDrag = eventData.pointerDrag?.GetComponent<TraySlotDrag>();
        if (trayDrag != null) matId = trayDrag.MaterialId;
        if (string.IsNullOrEmpty(matId))
        {
            var item = eventData.pointerDrag?.GetComponent<MaterialPaletteItem>();
            if (item != null) matId = item.MaterialId;
        }
        if (string.IsNullOrEmpty(matId)) return;

        if (InventorySystem.Instance == null || !InventorySystem.Instance.Spend(matId, 1)) return;
        if (TrayGridUI.Instance != null) TrayGridUI.Instance.RefreshCounts();

        MaterialId = matId;
        CurrentState = State.HasMaterial;
        UpdateVisual();
    }

    // --- 停靠 ---
    public void SetDock(PortafilterDock dock)
    {
        CurrentDock = dock;

        // 停靠在磨豆机且有材料 → 可以研磨
        // 停靠在萃取机且已研磨 → 可以萃取
        if (dock != null)
        {
            var cc = FindFirstObjectByType<CraftController>();
            if (cc != null)
                cc.OnPortafilterDocked(dock.StepId, this);
        }
    }

    // --- 外部状态变更 ---
    public void SetGround() { CurrentState = State.Ground; UpdateVisual(); }
    public void Clear() { CurrentState = State.Empty; MaterialId = null; UpdateVisual(); }

    private void UpdateVisual()
    {
        if (_image == null) return;
        var sp = CurrentState switch
        {
            State.HasMaterial => hasMaterialSprite ?? emptySprite,
            State.Ground => groundSprite ?? hasMaterialSprite ?? emptySprite,
            _ => emptySprite
        };
        if (sp != null) { _image.sprite = sp; _image.preserveAspect = true; _image.color = Color.white; }
        else _image.color = CurrentState == State.Empty ? new Color(0.5f,0.5f,0.5f,0.8f) : new Color(0.4f,0.25f,0.1f);
    }
}
