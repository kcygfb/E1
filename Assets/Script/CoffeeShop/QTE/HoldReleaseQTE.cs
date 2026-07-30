using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 长按松手 QTE：按住按钮蓄力条上升，松手时判定落在目标区间内。
/// Perfect: 0.78~0.88, Good: 0.65~0.95 (排除Perfect), Miss: 其他
/// 超时未松手 = Miss
/// </summary>
public class HoldReleaseQTE : QTEBase, IPointerDownHandler, IPointerUpHandler
{
    [Header("长按蓄力设置")]
    [SerializeField] private float fillDuration = 1.5f;
    [SerializeField] private float perfectMin = 0.78f;
    [SerializeField] private float perfectMax = 0.88f;
    [SerializeField] private float goodMin = 0.65f;
    [SerializeField] private float goodMax = 0.95f;
    [SerializeField] private float timeoutDuration = 2.5f;

    [Header("图片资源 (留空用默认纯色)")]
    [SerializeField] private Sprite barSprite;
    [SerializeField] private Color barColor = new Color(0.15f, 0.15f, 0.2f, 1f);
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Color fillColor = new Color(0.3f, 0.6f, 1f, 0.8f);
    [SerializeField] private Sprite targetZoneSprite;
    [SerializeField] private Color targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.35f);
    [SerializeField] private Sprite perfectZoneSprite;
    [SerializeField] private Color perfectZoneColor = new Color(1f, 0.84f, 0f, 0.5f);

    [SerializeField] private RectTransform _fillBar;
    [SerializeField] private RectTransform _fill;
    [SerializeField] private Image _fillImg;
    [SerializeField] private Image _targetZone;
    [SerializeField] private Text _hintText;
    private float _fillAmount;
    private bool _isHolding;
    private float _timer;

    protected override void BuildSpecificUI(RectTransform panel)
    {
        if (_fillBar != null) return; // 已在 Inspector 赋值
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
        titleText.text = "按住蓄力，松手!";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.color = Color.white;

        // 蓄力条容器
        var barObj = new GameObject("FillBar", typeof(RectTransform), typeof(Image));
        barObj.transform.SetParent(panel, false);
        _fillBar = barObj.GetComponent<RectTransform>();
        _fillBar.sizeDelta = new Vector2(60, 180);
        _fillBar.anchoredPosition = new Vector2(0, 10);
        var barImg = barObj.GetComponent<Image>();
        if (barSprite != null) barImg.sprite = barSprite;
        barImg.color = barColor;
        barImg.raycastTarget = true;

        // 目标区间标记
        var zoneObj = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(_fillBar, false);
        _targetZone = zoneObj.GetComponent<Image>();
        if (targetZoneSprite != null) _targetZone.sprite = targetZoneSprite;
        _targetZone.color = targetZoneColor;
        _targetZone.raycastTarget = false;
        var zoneRect = zoneObj.GetComponent<RectTransform>();
        zoneRect.anchorMin = new Vector2(0, goodMin);
        zoneRect.anchorMax = new Vector2(1, goodMax);
        zoneRect.offsetMin = Vector2.zero;
        zoneRect.offsetMax = Vector2.zero;

        // Perfect 区间标记
        var perfectObj = new GameObject("PerfectZone", typeof(RectTransform), typeof(Image));
        perfectObj.transform.SetParent(_fillBar, false);
        var perfectImg = perfectObj.GetComponent<Image>();
        if (perfectZoneSprite != null) perfectImg.sprite = perfectZoneSprite;
        perfectImg.color = perfectZoneColor;
        perfectImg.raycastTarget = false;
        var perfectRect = perfectObj.GetComponent<RectTransform>();
        perfectRect.anchorMin = new Vector2(0, perfectMin);
        perfectRect.anchorMax = new Vector2(1, perfectMax);
        perfectRect.offsetMin = Vector2.zero;
        perfectRect.offsetMax = Vector2.zero;

        // 填充条
        var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(_fillBar, false);
        _fill = fillObj.GetComponent<RectTransform>();
        _fill.anchorMin = new Vector2(0, 0);
        _fill.anchorMax = new Vector2(1, 0);
        _fill.pivot = new Vector2(0.5f, 0f);
        _fill.anchoredPosition = new Vector2(0, 0);
        _fill.sizeDelta = new Vector2(0, 0);
        _fillImg = fillObj.GetComponent<Image>();
        if (fillSprite != null) _fillImg.sprite = fillSprite;
        _fillImg.color = fillColor;
        _fillImg.raycastTarget = false;

        // 提示文字
        var hintObj = new GameObject("Hint", typeof(RectTransform), typeof(Text));
        hintObj.transform.SetParent(panel, false);
        _hintText = hintObj.GetComponent<Text>();
        var hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0, 20);
        hintRect.sizeDelta = new Vector2(400, 30);
        _hintText.text = "按住蓄力...";
        _hintText.alignment = TextAnchor.MiddleCenter;
        _hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _hintText.fontSize = 20;
        _hintText.color = new Color(0.7f, 0.7f, 0.7f);
        _hintText.raycastTarget = false;
    }

    public override void Show(string stepId, string stepDisplayName = null)
    {
        base.Show(stepId, stepDisplayName);
        _fillAmount = 0f;
        _isHolding = false;
        _timer = 0f;
        UpdateFillVisual();
    }

    private void Update()
    {
        if (!_isActive) return;

        _timer += Time.deltaTime;
        if (_timer >= timeoutDuration && !_isHolding)
        {
            Complete(QTERating.Miss);
            return;
        }

        if (_isHolding)
        {
            _fillAmount += Time.deltaTime / fillDuration;
            if (_fillAmount >= 1f)
            {
                _fillAmount = 1f;
                _isHolding = false;
                // 到顶不松手 = Miss
                Complete(QTERating.Miss);
                return;
            }
            UpdateFillVisual();
        }
    }

    private void UpdateFillVisual()
    {
        _fill.sizeDelta = new Vector2(0, _fillBar.rect.height * _fillAmount);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_isActive) return;
        _isHolding = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isActive || !_isHolding) return;
        _isHolding = false;

        QTERating rating;
        if (_fillAmount >= perfectMin && _fillAmount <= perfectMax)
            rating = QTERating.Perfect;
        else if (_fillAmount >= goodMin && _fillAmount <= goodMax)
            rating = QTERating.Good;
        else
            rating = QTERating.Miss;

        Complete(rating);
    }
}
