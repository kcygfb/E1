using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>九宫格格子拖拽组件。
/// Shop 阶段：拖到 MaterialSlot 或 CupContainer/Kettle 触发对应逻辑。
/// MorningCheck 阶段：拖出格子（未拖到有效目标）= 取消选取，清空该格。</summary>
[RequireComponent(typeof(Image))]
public class TraySlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string MaterialId { get; set; }
    public int SlotIndex { get; set; }

    private GameObject _dragIcon;
    private Canvas _canvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(MaterialId)) return;

        if (TrayGridUI.Instance != null && TrayGridUI.Instance.IsDragMode)
        {
            if (InventorySystem.Instance == null || InventorySystem.Instance.GetAmount(MaterialId) <= 0) return;
        }

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return;

        _dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        var rt = _dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(108f, 108f);

        var img = _dragIcon.GetComponent<Image>();
        var sprite = MaterialDefinition.GetSprite(MaterialId);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            var def = MaterialDefinition.Get(MaterialId);
            img.color = def != null ? def.color : Color.gray;
        }
        img.raycastTarget = false;

        var cellImg = GetComponent<Image>();
        if (cellImg != null) cellImg.raycastTarget = false;

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var cellImg = GetComponent<Image>();
        if (cellImg != null) cellImg.raycastTarget = true;
        if (_dragIcon == null) return;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 1. MaterialSlot → 放入机器槽位
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<MaterialSlot>();
            if (slot != null)
            {
                // 花库存 + 生成 MaterialIcon 放入槽
                if (InventorySystem.Instance != null && InventorySystem.Instance.Spend(MaterialId, 1))
                {
                    if (TrayGridUI.Instance != null) TrayGridUI.Instance.RefreshCounts();
                    CreateIconInSlot(slot);
                }
                Destroy(_dragIcon);
                _dragIcon = null;
                return;
            }
        }

        // 2. CupContainer → 直接拖材料到杯子
        foreach (var r in results)
        {
            var cup = r.gameObject.GetComponent<CupContainer>();
            if (cup != null)
            {
                if (InventorySystem.Instance != null && InventorySystem.Instance.Spend(MaterialId, 1))
                {
                    if (TrayGridUI.Instance != null) TrayGridUI.Instance.RefreshCounts();
                    CreateIconInContainer(cup.transform, cup.gameObject);
                }
                Destroy(_dragIcon);
                _dragIcon = null;
                return;
            }
        }

        // 3. Kettle → 拖材料到壶
        foreach (var r in results)
        {
            var kettle = r.gameObject.GetComponent<Kettle>();
            if (kettle != null)
            {
                if (InventorySystem.Instance != null && InventorySystem.Instance.Spend(MaterialId, 1))
                {
                    if (TrayGridUI.Instance != null) TrayGridUI.Instance.RefreshCounts();
                    kettle.AddContent(MaterialId);
                }
                Destroy(_dragIcon);
                _dragIcon = null;
                return;
            }
        }

        // 4. MorningCheck 阶段：拖出未命中 = 清空格子
        if (TrayGridUI.Instance != null && !TrayGridUI.Instance.IsDragMode)
        {
            bool hitTarget = false;
            foreach (var r in results)
            {
                if (r.gameObject.GetComponent<MaterialSlot>() != null ||
                    r.gameObject.GetComponent<CupContainer>() != null ||
                    r.gameObject.GetComponent<Kettle>() != null ||
                    r.gameObject.GetComponent<TrayCellDrop>() != null)
                { hitTarget = true; break; }
            }
            if (!hitTarget) TrayGridUI.Instance.ClearCell(SlotIndex);
        }

        Destroy(_dragIcon);
        _dragIcon = null;
    }

    private void CreateIconInSlot(MaterialSlot slot)
    {
        var go = new GameObject("Icon_" + MaterialId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MaterialIcon));
        go.transform.SetParent(slot.transform, false);
        go.transform.localPosition = Vector3.zero;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(108, 108);
        var icon = go.GetComponent<MaterialIcon>();
        icon.Setup(MaterialId);
        // 用反射调用 Accept（因为 MaterialSlot.Accept 需要 MaterialIcon）
        slot.Accept(icon);
    }

    private void CreateIconInContainer(Transform parent, GameObject containerObj)
    {
        var go = new GameObject("Icon_" + MaterialId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MaterialIcon));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(72, 72);
        var icon = go.GetComponent<MaterialIcon>();
        icon.Setup(MaterialId);
        // 如果是 CupContainer，调用 AcceptIcon
        var cup = containerObj.GetComponent<CupContainer>();
        if (cup != null) cup.AcceptIcon(icon);
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (_dragIcon == null || _canvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, eventData.position,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPos);
        _dragIcon.GetComponent<RectTransform>().anchoredPosition = localPos;
    }
}
