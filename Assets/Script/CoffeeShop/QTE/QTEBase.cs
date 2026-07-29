using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// QTE 基类：负责创建通用 UI（背景遮罩、面板、结果文字），
/// 子类实现具体的 QTE 玩法逻辑。
/// 挂到 Canvas 下的一个 GameObject 上即可，无需预制体。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class QTEBase : MonoBehaviour
{
    [Header("通用设置")]
    [SerializeField] protected float panelFadeDuration = 0.15f;

    /// <summary>QTE 完成时触发，参数为评级</summary>
    public UnityEvent<QTERating> OnQTEDone { get; } = new();

    protected GameObject _dimOverlay;
    protected RectTransform _panel;
    protected Text _stepTitleText;
    protected Text _resultText;
    protected bool _isActive;
    protected bool _uiBuilt;

    /// <summary>是否正在运行 QTE</summary>
    public bool IsActive => _isActive;

    protected virtual void Awake()
    {
        EnsureUIBuilt();
        // 初始隐藏，但不覆盖 Show() 已设置的 _isActive
        if (!_isActive)
            HideImmediate();
    }

    /// <summary>懒加载：GameObject inactive 时 Awake 不调用，首次 Show 时确保 UI 已构建</summary>
    protected void EnsureUIBuilt()
    {
        if (_uiBuilt) return;
        _uiBuilt = true;
        BuildCommonUI();
    }

    protected virtual void OnDestroy()
    {
        DOTween.Kill(this);
    }

    /// <summary>显示 QTE 面板并开始。stepDisplayName 显示在面板顶部作为当前步骤说明。</summary>
    public virtual void Show(string stepId, string stepDisplayName = null)
    {
        EnsureUIBuilt();
        _isActive = true;
        gameObject.SetActive(true);
        _dimOverlay.SetActive(true);
        _panel.gameObject.SetActive(true);
        _resultText.text = "";

        // 显示步骤名称
        if (_stepTitleText != null)
            _stepTitleText.text = string.IsNullOrEmpty(stepDisplayName) ? stepId : stepDisplayName;

        var cg = _panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.DOFade(1f, panelFadeDuration).SetId(this);
        }
        else
        {
            // 没有 CanvasGroup 时直接显示
            _panel.gameObject.SetActive(true);
        }
    }

    /// <summary>隐藏 QTE 面板</summary>
    public virtual void Hide()
    {
        _isActive = false;
        var cg = _panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOFade(0f, panelFadeDuration).SetId(this).OnComplete(() =>
            {
                _dimOverlay.SetActive(false);
                _panel.gameObject.SetActive(false);
            });
        }
        else
        {
            HideImmediate();
        }
    }

    protected void HideImmediate()
    {
        _isActive = false;
        if (_dimOverlay != null) _dimOverlay.SetActive(false);
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    /// <summary>子类调用：完成 QTE，触发事件并隐藏</summary>
    protected void Complete(QTERating rating)
    {
        if (!_isActive) return;

        string label = rating switch
        {
            QTERating.Perfect => "<color=#FFD700>Perfect!</color>",
            QTERating.Good => "<color=#90EE90>Good</color>",
            QTERating.Miss => "<color=#FF6B6B>Miss...</color>",
            _ => ""
        };
        _resultText.text = label;

        // 短暂展示结果后隐藏
        DOVirtual.DelayedCall(0.5f, () =>
        {
            Hide();
            OnQTEDone?.Invoke(rating);
        }).SetId(this);
    }

    /// <summary>构建通用 UI：背景遮罩 + 居中面板 + 结果文字</summary>
    protected virtual void BuildCommonUI()
    {
        var rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 背景遮罩
        _dimOverlay = new GameObject("QTE_Dim", typeof(Image));
        _dimOverlay.transform.SetParent(transform, false);
        var dimRect = _dimOverlay.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        var dimImg = _dimOverlay.GetComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.4f);
        dimImg.raycastTarget = true;

        // 主面板
        _panel = new GameObject("QTE_Panel", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        _panel.SetParent(transform, false);
        _panel.sizeDelta = new Vector2(600, 300);
        _panel.anchoredPosition = Vector2.zero;
        var panelImg = _panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        panelImg.raycastTarget = true;

        // 步骤标题文字 (面板顶部)
        var stepTitleObj = new GameObject("StepTitle", typeof(RectTransform), typeof(Text));
        stepTitleObj.transform.SetParent(_panel, false);
        _stepTitleText = stepTitleObj.GetComponent<Text>();
        var stepTitleRect = stepTitleObj.GetComponent<RectTransform>();
        stepTitleRect.anchorMin = new Vector2(0.5f, 1f);
        stepTitleRect.anchorMax = new Vector2(0.5f, 1f);
        stepTitleRect.pivot = new Vector2(0.5f, 1f);
        stepTitleRect.anchoredPosition = new Vector2(0, -10);
        stepTitleRect.sizeDelta = new Vector2(500, 30);
        _stepTitleText.alignment = TextAnchor.MiddleCenter;
        _stepTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _stepTitleText.fontSize = 22;
        _stepTitleText.color = new Color(0.8f, 0.8f, 0.9f);
        _stepTitleText.raycastTarget = false;

        // 结果文字
        var resultObj = new GameObject("ResultText", typeof(RectTransform), typeof(Text));
        resultObj.transform.SetParent(_panel, false);
        _resultText = resultObj.GetComponent<Text>();
        var resultRect = resultObj.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0f);
        resultRect.anchorMax = new Vector2(0.5f, 0f);
        resultRect.pivot = new Vector2(0.5f, 0f);
        resultRect.anchoredPosition = new Vector2(0, 20);
        resultRect.sizeDelta = new Vector2(400, 60);
        _resultText.alignment = TextAnchor.MiddleCenter;
        _resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _resultText.fontSize = 36;
        _resultText.raycastTarget = false;
        _resultText.supportRichText = true;

        BuildSpecificUI(_panel);
    }

    /// <summary>子类实现：在面板内创建 QTE 专属 UI 元素</summary>
    protected abstract void BuildSpecificUI(RectTransform panel);
}
