using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>通用可拖动材料图标。机器产出/TrayGrid拖出/杯壶内显示。
/// 可拖到：MaterialSlot / CupContainer / Kettle / TrayCellDrop（存回格子）。</summary>
public class MaterialIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string MaterialId { get; private set; }

    private Image _image;
    private Canvas _canvas;
    private Transform _originalParent;
    private Vector3 _originalPos;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void EnsureCanvas()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(string materialId)
    {
        MaterialId = materialId;
        var sprite = MaterialDefinition.GetSprite(materialId);
        if (sprite != null)
        {
            _image.sprite = sprite;
            _image.preserveAspect = true;
            _image.color = Color.white;
        }
        else
        {
            var def = MaterialDefinition.Get(materialId);
            _image.color = def != null ? def.color : Color.gray;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureCanvas();
        _originalParent = transform.parent;
        _originalPos = transform.localPosition;

        // 从 MaterialSlot 里移除引用
        var slot = _originalParent?.GetComponent<MaterialSlot>();
        if (slot != null) slot.Remove(this);

        // 从 CupContainer 里移除引用
        var cup = _originalParent?.GetComponent<CupContainer>();
        if (cup != null) cup.RemoveIcon(this);

        // 从 Kettle 里移除引用
        var kettle = _originalParent?.GetComponent<Kettle>();
        if (kettle != null) kettle.RemoveIcon(this);

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

        // 1. MaterialSlot → 放入机器
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<MaterialSlot>();
            if (slot != null && slot.Accept(this))
            {
                // 放入槽位后设为不拦截raycast，避免挡住slot
                if (_image != null) _image.raycastTarget = false;
                return;
            }
        }

        // 2. CupContainer → 放入杯子
        foreach (var r in results)
        {
            var cup = r.gameObject.GetComponent<CupContainer>();
            if (cup != null && cup.AcceptIcon(this))
                return;
        }

        // 3. Kettle → 放入水壶
        foreach (var r in results)
        {
            var kettle = r.gameObject.GetComponent<Kettle>();
            if (kettle != null && kettle.AcceptIcon(this))
                return;
        }

        // 4. TrayCellDrop → 存回格子
        foreach (var r in results)
        {
            var cell = r.gameObject.GetComponent<TrayCellDrop>();
            if (cell != null)
            {
                // 只有原始材料才能存回格子
                var def = MaterialDefinition.Get(MaterialId);
                if (def != null && def.isRaw)
                {
                    if (InventorySystem.Instance != null)
                        InventorySystem.Instance.Add(MaterialId, 1);
                    if (TrayGridUI.Instance != null)
                        TrayGridUI.Instance.RefreshCounts();
                }
                Destroy(gameObject);
                return;
            }
        }

        // 没命中 → 回到原位
        transform.SetParent(_originalParent, true);
        transform.localPosition = _originalPos;
    }
}
