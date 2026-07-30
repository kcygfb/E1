using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 节奏点击 QTE：指针在条上从左到右往返移动，点击时落在目标区域内判定。
/// Perfect: 目标区中心 40%, Good: 目标区外侧, Miss: 区域外
/// </summary>
public class RhythmTapQTE : QTEBase, IPointerClickHandler
{
    [Header("节奏点击设置")]
    [SerializeField] private float sweepDuration = 1.2f;
    [SerializeField] private float targetZoneMin = 0.35f;
    [SerializeField] private float targetZoneMax = 0.55f;

    [Header("图片资源 (留空用默认纯色)")]
    [SerializeField] private Sprite barSprite;
    [SerializeField] private Color barColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    [SerializeField] private Sprite pointerSprite;
    [SerializeField] private Color pointerColor = Color.cyan;
    [SerializeField] private Sprite targetZoneSprite;
    [SerializeField] private Color targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    [SerializeField] private Sprite perfectZoneSprite;
    [SerializeField] private Color perfectZoneColor = new Color(1f, 0.84f, 0f, 0.7f);

    [SerializeField] private RectTransform _bar;
    [SerializeField] private RectTransform _pointer;
    private float _pointerPos; // 0~1, 0=最左, 1=最右
    private float _barHalfWidth;

    protected override void BuildSpecificUI(RectTransform panel)
    {
        if (_bar != null) return; // 已在 Inspector 赋值
        // 标题
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(panel, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(500, 40);
        var titleText = titleObj.GetComponent<Text>();
        titleText.text = "点击!";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.color = Color.white;

        // 指针轨道条
        var barObj = new GameObject("Bar", typeof(RectTransform), typeof(Image));
        barObj.transform.SetParent(panel, false);
        _bar = barObj.GetComponent<RectTransform>();
        _bar.sizeDelta = new Vector2(500, 50);
        _bar.anchoredPosition = new Vector2(0, 20);
        var barImg = barObj.GetComponent<Image>();
        if (barSprite != null) barImg.sprite = barSprite;
        barImg.color = barColor;
        barImg.raycastTarget = true;

        // Good 区域 (绿色, 完整目标区)
        var zoneObj = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(_bar, false);
        var zoneImg = zoneObj.GetComponent<Image>();
        if (targetZoneSprite != null) zoneImg.sprite = targetZoneSprite;
        zoneImg.color = targetZoneColor;
        zoneImg.raycastTarget = false;
        var zoneRect = zoneObj.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(targetZoneMin, 0);
        zoneRect.anchorMax = new Vector2(targetZoneMax, 1);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        // Perfect 区域 (金色, 目标区中心 40%)
        float perfectCenter = (targetZoneMin + targetZoneMax) / 2f;
        float perfectHalf = (targetZoneMax - targetZoneMin) * 0.2f;
        var perfectObj = new GameObject("PerfectZone", typeof(RectTransform), typeof(Image));
        perfectObj.transform.SetParent(_bar, false);
        var perfectImg = perfectObj.GetComponent<Image>();
        if (perfectZoneSprite != null) perfectImg.sprite = perfectZoneSprite;
        perfectImg.color = perfectZoneColor;
        perfectImg.raycastTarget = false;
        var perfectRect = perfectObj.GetComponent<RectTransform>();
        perfectRect.anchorMin = new Vector2(perfectCenter - perfectHalf, 0);
        perfectRect.anchorMax = new Vector2(perfectCenter + perfectHalf, 1);
        perfectRect.offsetMin = Vector2.zero;
        perfectRect.offsetMax = Vector2.zero;

        // 指针
        var ptrObj = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
        ptrObj.transform.SetParent(_bar, false);
        _pointer = ptrObj.GetComponent<RectTransform>();
        _pointer.sizeDelta = new Vector2(8, 60);
        _pointer.anchorMin = new Vector2(0.5f, 0.5f);
        _pointer.anchorMax = new Vector2(0.5f, 0.5f);
        _pointer.pivot = new Vector2(0.5f, 0.5f);
        _pointer.anchoredPosition = Vector2.zero;
        var ptrImg = ptrObj.GetComponent<Image>();
        if (pointerSprite != null) ptrImg.sprite = pointerSprite;
        ptrImg.color = pointerColor;
        ptrImg.raycastTarget = false;
    }

    public override void Show(string stepId, string stepDisplayName = null)
    {
        base.Show(stepId, stepDisplayName);
        _pointerPos = 0f;
        _barHalfWidth = _bar.rect.width / 2f;
        SweepPointer();
    }

    private void SweepPointer()
    {
        _pointerPos = 0f;

        // 用单一 tween 驱动 _pointerPos，视觉位置同步更新
        DOTween.To(
            () => _pointerPos,
            x =>
            {
                _pointerPos = x;
                // 0=最左(-halfWidth), 1=最右(+halfWidth)
                _pointer.anchoredPosition = new Vector2((x - 0.5f) * 2f * _barHalfWidth, 0);
            },
            1f, sweepDuration
        )
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Yoyo)
        .SetId(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActive) return;

        DOTween.Kill(this);

        float center = (targetZoneMin + targetZoneMax) / 2f;
        float halfWidth = (targetZoneMax - targetZoneMin) / 2f;
        float dist = Mathf.Abs(_pointerPos - center);

        QTERating rating;
        if (dist <= halfWidth * 0.4f)
            rating = QTERating.Perfect;
        else if (dist <= halfWidth)
            rating = QTERating.Good;
        else
            rating = QTERating.Miss;

        Complete(rating);
    }

    public override void Hide()
    {
        DOTween.Kill(this);
        base.Hide();
    }
}
