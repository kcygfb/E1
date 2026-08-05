using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>杯子堆。不动，拖动时在松手位置生成一个 CupContainer。</summary>
[RequireComponent(typeof(Image))]
public class CupStack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Sprite cupSprite;

    private Canvas _canvas;
    private GameObject _dragIcon;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return;

        // 创建跟随鼠标的临时图标（本体不动）
        _dragIcon = new GameObject("DragCup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        var rt = _dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);

        var img = _dragIcon.GetComponent<Image>();
        var sp = cupSprite;
        if (sp == null) sp = GetComponent<Image>()?.sprite;
        if (sp != null) { img.sprite = sp; img.preserveAspect = true; img.color = Color.white; }
        img.raycastTarget = false;

        MoveDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;
        MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;

        // 在松手位置生成 CupContainer
        var cc = FindFirstObjectByType<CraftController>();
        if (cc != null)
            cc.OnCupDraggedOut(eventData.position, cupSprite);

        Destroy(_dragIcon);
        _dragIcon = null;
    }

    private void MoveDragIcon(PointerEventData eventData)
    {
        _dragIcon.transform.position = eventData.position;
    }
}
