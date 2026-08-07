using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>杯子堆。拖出时在松手位置生成 CupContainer。</summary>
[RequireComponent(typeof(Image))]
public class CupStack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Sprite cupSprite;
    [SerializeField] private GameObject cupPrefab;

    private Canvas _canvas;
    private GameObject _dragIcon;
    private CraftController _craftController;
    private ButtonGlow _glow;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _craftController = FindFirstObjectByType<CraftController>();
        _glow = GetComponent<ButtonGlow>();
    }

    private void Update()
    {
        if (_glow == null || _craftController == null) return;
        _glow.SetOn(!_craftController.HasActiveCup);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_craftController != null && _craftController.HasActiveCup)
            return;

        _dragIcon = new GameObject("DragCup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _dragIcon.transform.SetParent(_canvas.transform, false);
        _dragIcon.transform.SetAsLastSibling();

        var rt = _dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);

        var img = _dragIcon.GetComponent<Image>();
        var sp = cupSprite ?? GetComponent<Image>()?.sprite;
        if (sp != null) { img.sprite = sp; img.preserveAspect = true; img.color = Color.white; }
        img.raycastTarget = false;
        _dragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null) _dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon == null) return;

        _craftController?.OnCupDraggedOut(eventData.position, cupSprite, cupPrefab);

        Destroy(_dragIcon);
        _dragIcon = null;
    }
}
