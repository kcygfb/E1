using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>水壶。可拖动容器，可放入KettleSlot，可接收MaterialIcon（兼做材料槽），
/// 拖到杯子倒入全部内容。</summary>
[RequireComponent(typeof(Image))]
public class Kettle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public List<string> Contents { get; } = new();
    public List<MaterialIcon> Icons { get; } = new();
    public bool IsFilled => Contents.Count > 0;

    private Image _image;
    private Canvas _canvas;
    private KettleSlot _currentSlot;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void EnsureCanvas()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    // === 兼做材料槽 ===
    public bool AcceptIcon(MaterialIcon icon)
    {
        if (icon == null) return false;
        Contents.Add(icon.MaterialId);
        Icons.Add(icon);
        icon.transform.SetParent(transform, false);
        icon.transform.localPosition = Vector3.zero;
        return true;
    }

    /// <summary>获取第一个材料的ID（机器处理用）。</summary>
    public string GetMaterialId() => Contents.Count > 0 ? Contents[0] : null;

    public void RemoveIcon(MaterialIcon icon)
    {
        Icons.Remove(icon);
        var idx = Contents.IndexOf(icon.MaterialId);
        if (idx >= 0) Contents.RemoveAt(idx);
    }

    /// <summary>消耗所有输入材料（销毁icon），机器处理时调用。</summary>
    public void ConsumeAll()
    {
        foreach (var icon in Icons)
            if (icon != null) Destroy(icon.gameObject);
        Icons.Clear();
        Contents.Clear();
    }

    public void AddContent(string materialId)
    {
        Contents.Add(materialId);
        var iconGO = new GameObject("Icon_" + materialId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MaterialIcon));
        iconGO.transform.SetParent(transform, false);
        iconGO.transform.localPosition = Vector3.zero;
        var rt = iconGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(72, 72);
        var icon = iconGO.GetComponent<MaterialIcon>();
        icon.Setup(materialId);
        Icons.Add(icon);
    }

    // === 拖动 ===
    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureCanvas();
        if (_currentSlot != null) { _currentSlot.Clear(); _currentSlot = null; }
        if (_canvas != null) transform.SetParent(_canvas.transform, true);
        transform.SetAsLastSibling();
        if (_image != null) _image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_image != null) _image.raycastTarget = true;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 1. CupContainer → 倒入全部
        foreach (var r in results)
        {
            var cup = r.gameObject.GetComponent<CupContainer>();
            if (cup != null && IsFilled)
            {
                foreach (var icon in Icons)
                    if (icon != null) cup.AcceptIcon(icon);
                Contents.Clear();
                Icons.Clear();
                return;
            }
        }

        // 2. KettleSlot → 放回槽位
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<KettleSlot>();
            if (slot != null && slot.Accept(this))
            {
                _currentSlot = slot;
                return;
            }
        }

        // 没命中 → 留在松手位置
    }

    public void SetSlot(KettleSlot slot) => _currentSlot = slot;
}
