using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>九宫格格子拖拽组件。
/// Shop 阶段：拖到步骤按钮上消耗材料触发 QTE。
/// MorningCheck 阶段：拖出格子（未拖到步骤按钮）= 取消选取，清空该格。</summary>
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

        // Shop 阶段需要检查库存
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
        rt.sizeDelta = new Vector2(80f, 80f);

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

        // 检测是否拖到步骤按钮
        StepDropTarget stepTarget = null;
        foreach (var r in results)
        {
            stepTarget = r.gameObject.GetComponent<StepDropTarget>();
            if (stepTarget != null) break;
        }

        if (stepTarget != null)
        {
            // Shop 阶段：消耗材料触发 QTE
            stepTarget.OnDropMaterial(MaterialId);
        }
        else if (TrayGridUI.Instance != null && !TrayGridUI.Instance.IsDragMode)
        {
            // MorningCheck 阶段：拖出格子且未拖到步骤按钮 = 取消选取
            TrayGridUI.Instance.ClearCell(SlotIndex);
        }

        Destroy(_dragIcon);
        _dragIcon = null;
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (_dragIcon == null || _canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPos);

        _dragIcon.GetComponent<RectTransform>().anchoredPosition = localPos;
    }
}
