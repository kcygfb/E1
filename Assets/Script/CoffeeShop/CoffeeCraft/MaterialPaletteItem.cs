using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>材料列表项。从 MaterialPalette 拖出，放到 TrayGrid 的格子里。</summary>
[RequireComponent(typeof(Image))]
public class MaterialPaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string MaterialId { get; private set; }

    private GameObject _dragIcon;
    private Canvas _canvas;

    public void Setup(string materialId)
    {
        MaterialId = materialId;
        var def = MaterialDefinition.Get(materialId);
        var img = GetComponent<Image>();
        if (img != null)
        {
            var sprite = MaterialDefinition.GetSprite(materialId);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (def != null)
            {
                img.color = def.color;
            }
        }

        var label = transform.Find("Label")?.GetComponent<Text>();
        if (label != null && def != null)
            label.text = def.displayName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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

        // Disable self raycast during drag
        var selfImg = GetComponent<Image>();
        if (selfImg != null) selfImg.raycastTarget = false;

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var selfImg = GetComponent<Image>();
        if (selfImg != null) selfImg.raycastTarget = true;

        if (_dragIcon == null) return;

        // Raycast for drop target (TrayCellDrop)
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        TrayCellDrop cellDrop = null;
        foreach (var r in results)
        {
            cellDrop = r.gameObject.GetComponent<TrayCellDrop>();
            if (cellDrop != null) break;
        }

        // If dropped on a cell, OnDrop will be called by the cell's IDropHandler
        // via eventData.pointerDrag reference. But since we created a non-raycast icon,
        // the actual drop target is detected here.
        if (cellDrop != null && TrayGridUI.Instance != null)
            TrayGridUI.Instance.OnMaterialDroppedOnCell(cellDrop.cellIndex, MaterialId);

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
