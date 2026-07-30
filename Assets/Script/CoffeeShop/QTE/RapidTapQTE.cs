using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 快速连点 QTE：在限定时间内快速点击，次数越多评级越高。
/// Perfect: ≥8 taps, Good: 5~7 taps, Miss: <5 taps
/// </summary>
public class RapidTapQTE : QTEBase, IPointerClickHandler
{
    [Header("快速连点设置")]
    [SerializeField] private float timeLimit = 2.5f;
    [SerializeField] private int perfectThreshold = 8;
    [SerializeField] private int goodThreshold = 5;

    [Header("图片资源 (留空用默认纯色)")]
    [SerializeField] private Sprite barSprite;
    [SerializeField] private Color barColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    [SerializeField] private Sprite fillSprite;
    [SerializeField] private Color fillColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);

    [SerializeField] private RectTransform _progressBar;
    [SerializeField] private RectTransform _progressFill;
    [SerializeField] private Image _fillImg;
    [SerializeField] private Text _countText;
    [SerializeField] private Text _timerText;
    private int _tapCount;
    private float _elapsed;
    private bool _timing;

    protected override void BuildSpecificUI(RectTransform panel)
    {
        if (_progressBar != null) return; // 已在 Inspector 赋值
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
        titleText.text = "快速连点!";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.color = Color.white;

        // 进度条背景
        var barObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
        barObj.transform.SetParent(panel, false);
        _progressBar = barObj.GetComponent<RectTransform>();
        _progressBar.sizeDelta = new Vector2(400, 40);
        _progressBar.anchoredPosition = new Vector2(0, 30);
        var barImg = barObj.GetComponent<Image>();
        if (barSprite != null) barImg.sprite = barSprite;
        barImg.color = barColor;
        barImg.raycastTarget = true;

        // 进度条填充
        var fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(_progressBar, false);
        _progressFill = fillObj.GetComponent<RectTransform>();
        _progressFill.anchorMin = Vector2.zero;
        _progressFill.anchorMax = new Vector2(0, 1);
        _progressFill.offsetMin = Vector2.zero;
        _progressFill.offsetMax = Vector2.zero;
        _progressFill.sizeDelta = new Vector2(0, 0);
        _fillImg = fillObj.GetComponent<Image>();
        if (fillSprite != null) _fillImg.sprite = fillSprite;
        _fillImg.color = fillColor;
        _fillImg.raycastTarget = false;

        // 计数文字
        var countObj = new GameObject("CountText", typeof(RectTransform), typeof(Text));
        countObj.transform.SetParent(panel, false);
        _countText = countObj.GetComponent<Text>();
        var countRect = countObj.GetComponent<RectTransform>();
        countRect.anchoredPosition = new Vector2(0, -20);
        countRect.sizeDelta = new Vector2(300, 60);
        _countText.text = "0";
        _countText.alignment = TextAnchor.MiddleCenter;
        _countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _countText.fontSize = 48;
        _countText.color = Color.white;
        _countText.raycastTarget = false;

        // 计时文字
        var timerObj = new GameObject("TimerText", typeof(RectTransform), typeof(Text));
        timerObj.transform.SetParent(panel, false);
        _timerText = timerObj.GetComponent<Text>();
        var timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0f);
        timerRect.anchorMax = new Vector2(0.5f, 0f);
        timerRect.pivot = new Vector2(0.5f, 0f);
        timerRect.anchoredPosition = new Vector2(0, 15);
        timerRect.sizeDelta = new Vector2(200, 25);
        _timerText.alignment = TextAnchor.MiddleCenter;
        _timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _timerText.fontSize = 20;
        _timerText.color = new Color(0.7f, 0.7f, 0.7f);
        _timerText.raycastTarget = false;
    }

    public override void Show(string stepId, string stepDisplayName = null)
    {
        base.Show(stepId, stepDisplayName);
        _tapCount = 0;
        _elapsed = 0f;
        _timing = true;
        UpdateVisual();
    }

    private void Update()
    {
        if (!_isActive || !_timing) return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= timeLimit)
        {
            _timing = false;
            EvaluateResult();
            return;
        }
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float remaining = Mathf.Max(0, timeLimit - _elapsed);
        _timerText.text = $"{remaining:F1}s";
        _countText.text = _tapCount.ToString();
        float progress = Mathf.Clamp01((float)_tapCount / perfectThreshold);
        _progressFill.sizeDelta = new Vector2(_progressBar.rect.width * progress, 0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActive || !_timing) return;
        _tapCount++;

        // 点击反馈：数字缩放动画
        _countText.transform.DOKill();
        _countText.transform.localScale = Vector3.one * 1.3f;
        _countText.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);

        UpdateVisual();
    }

    private void EvaluateResult()
    {
        QTERating rating;
        if (_tapCount >= perfectThreshold)
            rating = QTERating.Perfect;
        else if (_tapCount >= goodThreshold)
            rating = QTERating.Good;
        else
            rating = QTERating.Miss;

        Complete(rating);
    }
}
