using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates the terminal, non-dismissible ending overlay after the final cafe summary.
/// Day-specific story content remains outside this presenter.
/// </summary>
public static class StoryEndingPresenter
{
    private const string RootName = "StoryEndingOverlay";

    public static void Show()
    {
        if (GameObject.Find(RootName) != null) return;

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        var blocker = root.GetComponent<CanvasGroup>();
        blocker.alpha = 1f;
        blocker.interactable = true;
        blocker.blocksRaycasts = true;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.025f, 0.02f, 0.035f, 0.97f);

        var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(root.transform, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.38f);
        titleRect.anchorMax = new Vector2(0.9f, 0.62f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = "故事结束\n<size=55%><color=#C9B9A8>试玩流程已完成</color></size>";
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 72f;
        title.color = new Color(0.97f, 0.88f, 0.72f, 1f);
        title.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
