using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 下落停止 QTE：指针从顶部匀速下落，点击时停下，落在目标区域内判定。
/// 到底部仍未点击 = Miss。
/// Perfect: 目标区中心 40%, Good: 目标区外侧, Miss: 区域外或超时
/// </summary>
public class DropStopQTE : QTEBase, IPointerClickHandler
{
    [Header("下落停止设置")]
    [SerializeField] private float dropDuration = 1.8f;
    [SerializeField] private float targetZoneMin = 0.55f;  // 目标区上界 (0=顶, 1=底)
    [SerializeField] private float targetZoneMax = 0.72f;  // 目标区下界

    [Header("图片资源 (留空用默认纯色)")]
    [SerializeField] private Sprite trackSprite;
    [SerializeField] private Color trackColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    [SerializeField] private Sprite pointerSprite;
    [SerializeField] private Color pointerColor = Color.cyan;
    [SerializeField] private Sprite targetZoneSprite;
    [SerializeField] private Color targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    [SerializeField] private Sprite perfectZoneSprite;
    [SerializeField] private Color perfectZoneColor = new Color(1f, 0.84f, 0f, 0.6f);

    [SerializeField] private RectTransform _track;
    [SerializeField] private RectTransform _pointer;
    [SerializeField] private Image _targetZoneImg;
    private float _pointerPos; // 0=顶, 1=底
    private bool _dropping;

    protected override void BuildSpecificUI(RectTransform panel)
    {
        if (_track != null) return; // 已在 Inspector 赋值
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
        titleText.text = "点击停下!";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.color = Color.white;

        // 轨道 (竖直条)
        var trackObj = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackObj.transform.SetParent(panel, false);
        _track = trackObj.GetComponent<RectTransform>();
        _track.sizeDelta = new Vector2(60, 200);
        _track.anchoredPosition = new Vector2(0, 10);
        var trackImg = trackObj.GetComponent<Image>();
        if (trackSprite != null) trackImg.sprite = trackSprite;
        trackImg.color = trackColor;
        trackImg.raycastTarget = true;

        // 目标区域 (绿色)
        var zoneObj = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(_track, false);
        _targetZoneImg = zoneObj.GetComponent<Image>();
        if (targetZoneSprite != null) _targetZoneImg.sprite = targetZoneSprite;
        _targetZoneImg.color = targetZoneColor;
        _targetZoneImg.raycastTarget = false;
        var zoneRect = zoneObj.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(0, 1f - targetZoneMax);
        zoneRect.anchorMax = new Vector2(1, 1f - targetZoneMin);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        // Perfect 区域 (金色, 更窄)
        float perfectCenter = (targetZoneMin + targetZoneMax) / 2f;
        float perfectHalf = (targetZoneMax - targetZoneMin) * 0.2f;
        var perfectObj = new GameObject("PerfectZone", typeof(RectTransform), typeof(Image));
        perfectObj.transform.SetParent(_track, false);
        var perfectImg = perfectObj.GetComponent<Image>();
        if (perfectZoneSprite != null) perfectImg.sprite = perfectZoneSprite;
        perfectImg.color = perfectZoneColor;
        perfectImg.raycastTarget = false;
        var perfectRect = perfectObj.GetComponent<RectTransform>();
        perfectRect.anchorMin = new Vector2(0, 1f - (perfectCenter + perfectHalf));
        perfectRect.anchorMax = new Vector2(1, 1f - (perfectCenter - perfectHalf));
        perfectRect.offsetMin = Vector2.zero;
        perfectRect.offsetMax = Vector2.zero;

        // 指针 (横条)
        var ptrObj = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
        ptrObj.transform.SetParent(_track, false);
        _pointer = ptrObj.GetComponent<RectTransform>();
        _pointer.sizeDelta = new Vector2(56, 8);
        _pointer.anchorMin = new Vector2(0.5f, 1f);
        _pointer.anchorMax = new Vector2(0.5f, 1f);
        _pointer.pivot = new Vector2(0.5f, 0.5f);
        _pointer.anchoredPosition = new Vector2(0, 0);
        var ptrImg = ptrObj.GetComponent<Image>();
        if (pointerSprite != null) ptrImg.sprite = pointerSprite;
        ptrImg.color = pointerColor;
        ptrImg.raycastTarget = false;
    }

    public override void Show(string stepId, string stepDisplayName = null)
    {
        base.Show(stepId, stepDisplayName);
        _pointerPos = 0f;
        _dropping = true;

        // 指针从顶 (anchor=1, y=0) 向下移动到轨道底部
        float trackHeight = _track.rect.height;
        _pointer.anchoredPosition = new Vector2(0, 0);

        DOTween.To(
            () => _pointerPos,
            x =>
            {
                _pointerPos = x;
                // 0=顶(anchor 1, offset 0), 1=底(anchor 1, offset -trackHeight)
                _pointer.anchoredPosition = new Vector2(0, -trackHeight * x);
            },
            1f, dropDuration
        )
        .SetEase(Ease.Linear)
        .SetId(this)
        .OnComplete(() =>
        {
            // 到底仍未点击
            if (_dropping)
            {
                _dropping = false;
                Complete(QTERating.Miss);
            }
        });
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActive || !_dropping) return;

        _dropping = false;
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
