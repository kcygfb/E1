#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 编辑器预览工具：在编辑模式下模拟 CoffeeListPopulator 的生成效果。
/// 通过菜单 Tools/CoffeeShop/Preview Coffee List 调用。
/// 预览按钮直接放在 Content 下（带 __Preview_ 前缀），利用 Content 的 LayoutGroup 自动排列。
/// </summary>
public static class CoffeeListPreviewEditor
{
    private const string PreviewPrefix = "__Preview_";

    [MenuItem("Tools/CoffeeShop/Preview Coffee List")]
    public static void PreviewCoffeeList()
    {
        var populator = Object.FindFirstObjectByType<CoffeeListPopulator>();
        if (populator == null)
        {
            Debug.LogError("[CoffeeListPreview] No CoffeeListPopulator found in scene.");
            return;
        }

        var contentField = typeof(CoffeeListPopulator)
            .GetField("content", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var content = contentField?.GetValue(populator) as Transform;
        if (content == null)
        {
            Debug.LogError("[CoffeeListPreview] content field not found or not assigned.");
            return;
        }

        var coffees = LoadCoffeeData();
        if (coffees == null || coffees.Count == 0)
        {
            Debug.LogError("[CoffeeListPreview] No coffee data loaded. Check StreamingAssets/CoffeeData/*.json");
            return;
        }

        ClearPreview(content);

        var itemSize = GetFieldValue<Vector2>(populator, "itemSize");
        var fontSize = GetFieldValue<float>(populator, "fontSize");
        var lockedColor = GetFieldValue<Color>(populator, "lockedColor");
        var normalColor = GetFieldValue<Color>(populator, "normalColor");
        var coffeeIconSprite = GetFieldValue<Sprite>(populator, "coffeeIconSprite");
        var fontAsset = GetFieldValue<TMP_FontAsset>(populator, "fontAsset");
        var textOffset = GetFieldValue<Vector2>(populator, "textOffset");
        var textAnchorX = GetFieldValue<float>(populator, "textAnchorX");
        var textAnchorY = GetFieldValue<float>(populator, "textAnchorY");

        foreach (var coffee in coffees)
        {
            CreatePreviewItem(content, coffee, itemSize, fontSize, lockedColor, normalColor,
                coffeeIconSprite, fontAsset, textOffset, textAnchorX, textAnchorY);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[CoffeeListPreview] Generated {coffees.Count} preview items under Content.");
    }

    [MenuItem("Tools/CoffeeShop/Clear Coffee List Preview")]
    public static void ClearPreviewMenu()
    {
        var populator = Object.FindFirstObjectByType<CoffeeListPopulator>();
        if (populator == null) return;

        var contentField = typeof(CoffeeListPopulator)
            .GetField("content", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var content = contentField?.GetValue(populator) as Transform;
        if (content == null) return;

        ClearPreview(content);
        Debug.Log("[CoffeeListPreview] Preview cleared.");
    }

    private static void ClearPreview(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (child.name.StartsWith(PreviewPrefix))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void CreatePreviewItem(Transform parent, CoffeeDataJson coffee,
        Vector2 itemSize, float fontSize, Color lockedColor, Color normalColor, Sprite iconSprite,
        TMP_FontAsset fontAsset, Vector2 textOffset, float textAnchorX, float textAnchorY)
    {
        var go = new GameObject(PreviewPrefix + coffee.coffeeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = itemSize;

        var image = go.GetComponent<Image>();
        image.sprite = iconSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = coffee.locked ? lockedColor : normalColor;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textGo.transform.SetParent(go.transform, false);
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(textAnchorX, textAnchorY);
        textRT.anchorMax = new Vector2(textAnchorX, textAnchorY);
        textRT.pivot = new Vector2(textAnchorX, textAnchorY);
        textRT.anchoredPosition = textOffset;
        textRT.sizeDelta = new Vector2(itemSize.x, fontSize * 1.5f);

        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) text.font = fontAsset;
        text.text = coffee.coffeeName;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.color = coffee.locked ? lockedColor : normalColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static List<CoffeeDataJson> LoadCoffeeData()
    {
        var dir = Path.Combine(Application.streamingAssetsPath, "CoffeeData");
        if (!Directory.Exists(dir))
        {
            Debug.LogError("[CoffeeListPreview] Directory not found: " + dir);
            return null;
        }

        var files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
        var result = new List<CoffeeDataJson>();

        foreach (var filePath in files)
        {
            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<CoffeeDataJson>(json);
            if (data != null && !string.IsNullOrEmpty(data.coffeeId))
                result.Add(data);
        }

        return result;
    }

    private static T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = typeof(CoffeeListPopulator)
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (T)field.GetValue(obj);
        return default;
    }
}
#endif
