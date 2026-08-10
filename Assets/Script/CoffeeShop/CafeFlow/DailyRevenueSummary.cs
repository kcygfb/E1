using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks one cafe day's guests and revenue, then builds a functional summary
/// from solid-color UI. Art can replace these Images later without changing flow.
/// </summary>
[DisallowMultipleComponent]
public sealed class DailyRevenueSummary : MonoBehaviour
{
    private readonly List<RevenueAwardedPayload> revenues = new();
    private readonly HashSet<string> recordedOrderIds = new();

    private TimeSystem timeSystem;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image dimmer;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text guestText;
    [SerializeField] private TMP_Text revenueListText;
    [SerializeField] private TMP_Text perfectText;
    [SerializeField] private TMP_Text totalText;
    [SerializeField] private Button confirmButton;

    private int guestCount;
    private bool isShowing;
    private bool isTransitioning;

    private static readonly Color DimColor = new Color(0.015f, 0.012f, 0.02f, 0.82f);
    private static readonly Color PanelColor = new Color(0.105f, 0.075f, 0.06f, 0.98f);
    private static readonly Color AccentColor = new Color(0.91f, 0.66f, 0.25f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.91f, 0.82f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.66f, 0.58f, 1f);

    private void Awake()
    {
        timeSystem = GetComponent<TimeSystem>();
        // Do NOT build placeholder UI in Cafe scene — the summary is meant for the
        // night/map transition, not for the cafe itself.  If we are in the Cafe
        // scene, skip building entirely so the panel never appears.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Cafe")
            return;

        if (popupRoot == null) BuildPlaceholderUI();
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnEnable()
    {
        GameEvent.On("DayStarted", OnDayStarted);
        GameEvent.On("CustomerArrived", OnCustomerArrived);
        GameEvent.On("RevenueAwarded", OnRevenueAwarded);
        GameEvent.On("PhaseChanged", OnPhaseChanged);
    }

    private void OnDisable()
    {
        GameEvent.Off("DayStarted", OnDayStarted);
        GameEvent.Off("CustomerArrived", OnCustomerArrived);
        GameEvent.Off("RevenueAwarded", OnRevenueAwarded);
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
    }

    private void OnDayStarted(object payload)
    {
        guestCount = 0;
        revenues.Clear();
        recordedOrderIds.Clear();
        isShowing = false;
        isTransitioning = false;
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void OnCustomerArrived(object payload)
    {
        guestCount++;
    }

    private void OnRevenueAwarded(object payload)
    {
        if (payload is not RevenueAwardedPayload revenue) return;
        if (!string.IsNullOrEmpty(revenue.OrderId) && !recordedOrderIds.Add(revenue.OrderId))
            return;
        revenues.Add(revenue);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        if (p.Phase != DayPhase.Settlement) return;

        // In Cafe scene, skip the summary popup — go straight to night.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Cafe")
        {
            if (timeSystem != null)
                timeSystem.NotifySettlementComplete();
            return;
        }
        ShowSummary();
    }

    /// <summary>
    /// Returns true if this component has taken ownership of the end-shop flow.
    /// The existing end-day button calls this as a manual fallback.
    /// </summary>
    public bool ShowSummary()
    {
        if (isTransitioning) return true;
        // In Cafe scene, never show the summary — skip straight to end-of-day flow.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Cafe")
            return false;
        if (popupRoot == null) BuildPlaceholderUI();
        if (popupRoot == null) return false;
        if (isShowing) return true;

        isShowing = true;
        RefreshText();
        popupRoot.SetActive(true);
        popupRoot.transform.SetAsLastSibling();
        if (popupCanvasGroup != null)
            StartCoroutine(ShowRoutine());
        else
            confirmButton.interactable = true;
        return true;
    }

    private void RefreshText()
    {
        int perfectCount = 0;
        int perfectBonusTotal = 0;
        int grandTotal = 0;
        var builder = new StringBuilder();

        if (revenues.Count == 0)
        {
            builder.Append("<color=#9F968B>No coffee orders completed</color>");
        }
        else
        {
            for (int i = 0; i < revenues.Count; i++)
            {
                RevenueAwardedPayload item = revenues[i];
                builder.Append(i + 1)
                    .Append(".  ")
                    .Append(string.IsNullOrWhiteSpace(item.CoffeeName) ? "Coffee" : item.CoffeeName)
                    .Append("    <color=#E8B35A>")
                    .Append(item.CoffeeRevenue)
                    .Append(" C</color>");

                if (item.IsPerfect)
                {
                    builder.Append("   <color=#FFD36A>PERFECT</color>");
                    perfectCount++;
                    perfectBonusTotal += item.PerfectBonus;
                }

                if (i < revenues.Count - 1) builder.AppendLine();
                grandTotal += item.TotalRevenue;
            }
        }

        guestText.text = $"GUESTS SERVED    <color=#E8B35A>{guestCount}</color>";
        revenueListText.text = builder.ToString();
        perfectText.text =
            $"PERFECT CUPS    {perfectCount}    " +
            $"<color=#E8B35A>+{perfectBonusTotal} C</color>";
        totalText.text = $"TOTAL    <color=#FFD36A>{grandTotal} C</color>";
    }

    private IEnumerator ShowRoutine()
    {
        confirmButton.interactable = false;
        popupCanvasGroup.alpha = 0f;
        panel.localScale = new Vector3(0.9f, 0.9f, 1f);
        panelCanvasGroup.alpha = 1f;
        dimmer.color = new Color(DimColor.r, DimColor.g, DimColor.b, 0f);

        const float duration = 0.28f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            popupCanvasGroup.alpha = eased;
            panel.localScale = Vector3.LerpUnclamped(
                new Vector3(0.9f, 0.9f, 1f),
                Vector3.one,
                eased);
            dimmer.color = new Color(DimColor.r, DimColor.g, DimColor.b, DimColor.a * eased);
            yield return null;
        }

        popupCanvasGroup.alpha = 1f;
        panel.localScale = Vector3.one;
        dimmer.color = DimColor;
        confirmButton.interactable = true;
    }

    private void OnConfirmClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        confirmButton.interactable = false;
        StartCoroutine(TransitionToMapRoutine());
    }

    private IEnumerator TransitionToMapRoutine()
    {
        const float duration = 0.38f;
        float elapsed = 0f;
        Color startDim = dimmer.color;
        Vector3 startScale = panel.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t;
            panel.localScale = Vector3.LerpUnclamped(
                startScale,
                new Vector3(0.94f, 0.94f, 1f),
                eased);
            panelCanvasGroup.alpha = 1f - Mathf.Clamp01((t - 0.25f) / 0.75f);
            dimmer.color = Color.Lerp(startDim, Color.black, eased);
            yield return null;
        }

        if (timeSystem != null)
            timeSystem.NotifySettlementComplete();
    }

    private void BuildPlaceholderUI()
    {
        if (popupRoot != null) return;

        Canvas canvas = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas candidate in canvases)
        {
            if (candidate.gameObject.scene == gameObject.scene && candidate.isRootCanvas)
            {
                canvas = candidate;
                break;
            }
        }
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[DailyRevenueSummary] Cannot create summary UI because no Canvas exists.");
            return;
        }

        popupRoot = CreateUIObject(
            "DailyRevenueSummary_Placeholder",
            canvas.transform,
            typeof(CanvasGroup));
        Stretch(popupRoot.GetComponent<RectTransform>());
        popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        popupCanvasGroup.blocksRaycasts = true;
        popupCanvasGroup.interactable = true;

        GameObject dimObject = CreateUIObject("Dimmer", popupRoot.transform, typeof(Image));
        Stretch(dimObject.GetComponent<RectTransform>());
        dimmer = dimObject.GetComponent<Image>();
        dimmer.color = DimColor;

        GameObject panelObject = CreateUIObject(
            "RevenuePanel_Placeholder",
            popupRoot.transform,
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup));
        panel = panelObject.GetComponent<RectTransform>();
        panelCanvasGroup = panelObject.GetComponent<CanvasGroup>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(960f, 720f);
        panel.anchoredPosition = Vector2.zero;
        panelObject.GetComponent<Image>().color = PanelColor;
        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);

        CreateBar(panel, "TopAccent", new Vector2(0f, 351f), new Vector2(900f, 6f));
        CreateText(
            panel, "Title", "TODAY'S REVENUE", 54f, FontStyles.Bold,
            AccentColor, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.97f));

        guestText = CreateText(
            panel, "GuestCount", string.Empty, 28f, FontStyles.Bold,
            TextColor, TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f));

        CreateText(
            panel, "SalesLabel", "COFFEE SALES", 22f, FontStyles.Bold,
            MutedTextColor, TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.72f));

        revenueListText = CreateText(
            panel, "RevenueList", string.Empty, 27f, FontStyles.Normal,
            TextColor, TextAlignmentOptions.TopLeft,
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f));
        revenueListText.lineSpacing = 18f;

        perfectText = CreateText(
            panel, "PerfectCount", string.Empty, 25f, FontStyles.Bold,
            TextColor, TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.35f));

        CreateBar(panel, "TotalDivider", new Vector2(0f, -174f), new Vector2(780f, 2f));
        totalText = CreateText(
            panel, "Total", string.Empty, 38f, FontStyles.Bold,
            TextColor, TextAlignmentOptions.Left,
            new Vector2(0.1f, 0.08f), new Vector2(0.62f, 0.23f));

        GameObject buttonObject = CreateUIObject(
            "ConfirmButton_Placeholder",
            panel,
            typeof(Image),
            typeof(Button),
            typeof(Outline));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.68f, 0.07f);
        buttonRect.anchorMax = new Vector2(0.9f, 0.2f);
        buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = AccentColor;
        confirmButton = buttonObject.GetComponent<Button>();
        confirmButton.targetGraphic = buttonImage;
        ColorBlock colors = confirmButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.7f, 1f);
        colors.pressedColor = new Color(0.78f, 0.65f, 0.5f, 1f);
        colors.disabledColor = new Color(0.45f, 0.4f, 0.35f, 0.7f);
        confirmButton.colors = colors;
        confirmButton.onClick.AddListener(OnConfirmClicked);

        Outline buttonOutline = buttonObject.GetComponent<Outline>();
        buttonOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        buttonOutline.effectDistance = new Vector2(3f, -3f);

        CreateText(
            buttonRect, "Label", "OK", 34f, FontStyles.Bold,
            new Color(0.16f, 0.1f, 0.06f, 1f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one);

        popupRoot.SetActive(false);
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent,
        params System.Type[] extraComponents)
    {
        var components = new List<System.Type>
        {
            typeof(RectTransform),
            typeof(CanvasRenderer)
        };
        components.AddRange(extraComponents);
        var result = new GameObject(name, components.ToArray());
        result.layer = 5;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateBar(
        RectTransform parent,
        string name,
        Vector2 position,
        Vector2 size)
    {
        GameObject barObject = CreateUIObject(name, parent, typeof(Image));
        RectTransform rect = barObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        barObject.GetComponent<Image>().color = AccentColor;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
