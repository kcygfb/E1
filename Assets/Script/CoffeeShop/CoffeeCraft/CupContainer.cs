using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>咖啡杯。纯可拖动 + 可接收材料。拖到 Deliver 提交，拖到步骤按钮触发逻辑。</summary>
[RequireComponent(typeof(Image))]
public class CupContainer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Sprites")]
    [SerializeField] private Sprite emptyCupSprite;
    [SerializeField] private Sprite filledCupSprite;

    [Header("State")]
    public List<string> Contents = new();
    public bool IsFilled => Contents.Count > 0;

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
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // 检测 Deliver
        foreach (var r in results)
        {
            if (r.gameObject.name != "Btn_Deliver") continue;
            var cc = FindFirstObjectByType<CraftController>();
            if (cc != null) cc.OnCupDelivered(this);
            transform.position = _dragStartWorldPos;
            return;
        }

        // 检测步骤按钮
        StepDropTarget target = null;
        foreach (var r in results)
        {
            target = r.gameObject.GetComponent<StepDropTarget>();
            if (target != null) break;
        }

        if (target != null)
        {
            var cc = FindFirstObjectByType<CraftController>();
            if (cc != null) cc.OnCupDroppedOnStep(target.stepId, this);
            transform.position = _dragStartWorldPos;
        }
        // 否则留在松手位置
    }

    // --- 接收材料 ---
    public void OnDrop(PointerEventData eventData)
    {
        string matId = null;
        var trayDrag = eventData.pointerDrag?.GetComponent<TraySlotDrag>();
        if (trayDrag != null) matId = trayDrag.MaterialId;
        if (string.IsNullOrEmpty(matId))
        {
            var item = eventData.pointerDrag?.GetComponent<MaterialPaletteItem>();
            if (item != null) matId = item.MaterialId;
        }
        if (string.IsNullOrEmpty(matId)) return;

        Contents.Add(matId);
        UpdateVisual();
    }

    // --- 外部 ---
    public void AddContent(string id) { Contents.Add(id); UpdateVisual(); }
    public void Clear() { Contents.Clear(); UpdateVisual(); }

    private void UpdateVisual()
    {
        if (_image == null) return;
        var sp = IsFilled ? (filledCupSprite ?? emptyCupSprite) : emptyCupSprite;
        if (sp != null) { _image.sprite = sp; _image.preserveAspect = true; _image.color = Color.white; }
        else _image.color = IsFilled ? new Color(0.4f,0.25f,0.1f) : new Color(0.7f,0.7f,0.7f,0.8f);
    }
}
