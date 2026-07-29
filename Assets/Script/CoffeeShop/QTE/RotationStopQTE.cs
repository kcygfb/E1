using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 旋转停止 QTE：指针绕圆心旋转，点击时停下，落在目标弧形区域内判定。
/// Perfect: 目标区中心 40%, Good: 目标区外侧, Miss: 区域外
/// </summary>
public class RotationStopQTE : QTEBase, IPointerClickHandler
{
    [Header("旋转停止设置")]
    [SerializeField] private float rotationDuration = 1.5f;
    [SerializeField] private float targetZoneCenter = 0f;        // 目标区中心角度 (0=顶部, 0~1=0~360度)
    [SerializeField] private float targetZoneHalfWidth = 0.12f;   // 目标区半宽 (0~1, 0.12≈43度)

    private RectTransform _circle;
    private RectTransform _pointer;
    private Image _targetZoneImg;
    private float _currentAngle; // 0~1 对应 0~360度
    private bool _rotating;

    protected override void BuildSpecificUI(RectTransform panel)
    {
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

        // 圆形背景
        var circleObj = new GameObject("Circle", typeof(RectTransform), typeof(Image));
        circleObj.transform.SetParent(panel, false);
        _circle = circleObj.GetComponent<RectTransform>();
        _circle.sizeDelta = new Vector2(200, 200);
        _circle.anchoredPosition = new Vector2(0, 10);
        var circleImg = circleObj.GetComponent<Image>();
        circleImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        circleImg.raycastTarget = true;

        // 目标区域弧形 (用绿色扇形图片简化为圆形扇区)
        var zoneObj = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
        zoneObj.transform.SetParent(_circle, false);
        _targetZoneImg = zoneObj.GetComponent<Image>();
        _targetZoneImg.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        _targetZoneImg.raycastTarget = false;
        var zoneRect = zoneObj.GetComponent<RectTransform>();
        zoneRect.sizeDelta = new Vector2(40, 100);
        zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        zoneRect.pivot = new Vector2(0.5f, 0f);
        zoneRect.anchoredPosition = new Vector2(0, 0);
        // 旋转到目标中心角度
        zoneRect.localEulerAngles = new Vector3(0, 0, targetZoneCenter * 360f);

        // Perfect 区域标记 (更窄的金色)
        var perfectObj = new GameObject("PerfectZone", typeof(RectTransform), typeof(Image));
        perfectObj.transform.SetParent(_circle, false);
        var perfectImg = perfectObj.GetComponent<Image>();
        perfectImg.color = new Color(1f, 0.84f, 0f, 0.6f);
        perfectImg.raycastTarget = false;
        var perfectRect = perfectObj.GetComponent<RectTransform>();
        perfectRect.sizeDelta = new Vector2(20, 100);
        perfectRect.anchorMin = new Vector2(0.5f, 0.5f);
        perfectRect.anchorMax = new Vector2(0.5f, 0.5f);
        perfectRect.pivot = new Vector2(0.5f, 0f);
        perfectRect.anchoredPosition = new Vector2(0, 0);
        perfectRect.localEulerAngles = new Vector3(0, 0, targetZoneCenter * 360f);

        // 指针
        var ptrObj = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
        ptrObj.transform.SetParent(_circle, false);
        _pointer = ptrObj.GetComponent<RectTransform>();
        _pointer.sizeDelta = new Vector2(6, 90);
        _pointer.anchorMin = new Vector2(0.5f, 0.5f);
        _pointer.anchorMax = new Vector2(0.5f, 0.5f);
        _pointer.pivot = new Vector2(0.5f, 0f);
        _pointer.anchoredPosition = new Vector2(0, 0);
        var ptrImg = ptrObj.GetComponent<Image>();
        ptrImg.color = Color.cyan;
        ptrImg.raycastTarget = false;
    }

    public override void Show(string stepId, string stepDisplayName = null)
    {
        base.Show(stepId, stepDisplayName);
        _currentAngle = 0f;
        _rotating = true;
        StartRotation();
    }

    private void StartRotation()
    {
        _pointer.localEulerAngles = Vector3.zero;

        // DOTween 无限旋转
        DOTween.To(
            () => _currentAngle,
            x =>
            {
                _currentAngle = x;
                _pointer.localEulerAngles = new Vector3(0, 0, x * 360f);
            },
            1f, rotationDuration
        )
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart)
        .SetId(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isActive || !_rotating) return;

        _rotating = false;
        DOTween.Kill(this);

        // _currentAngle 是 0~1, 取小数部分
        float angle = _currentAngle % 1f;
        if (angle < 0f) angle += 1f;

        // 计算与目标中心的距离 (环形距离)
        float diff = Mathf.Abs(angle - targetZoneCenter);
        if (diff > 0.5f) diff = 1f - diff;

        QTERating rating;
        if (diff <= targetZoneHalfWidth * 0.4f)
            rating = QTERating.Perfect;
        else if (diff <= targetZoneHalfWidth)
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
