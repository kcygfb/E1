using UnityEngine;
using UnityEngine.UI;
using KiKs.UI;
using System.Collections.Generic;

/// <summary>材料列表面板。MorningCheck 阶段显示 7 种可选材料，可拖拽到九宫格。
/// 面板尺寸/位置由 Inspector 决定，物品用 GridLayoutGroup 自动排版成网格。</summary>
[RequireComponent(typeof(RectTransform))]
public class MaterialPalette : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorialController;

    [SerializeField] private Vector2 cellSize = new Vector2(90f, 90f);
    [SerializeField] private Vector2 spacing = new Vector2(10f, 10f);
    [SerializeField] private float labelHeight = 24f;
    [SerializeField] private int constraintCount = 5;

    private readonly List<GameObject> generatedItems = new();

    private void Awake()
    {
        if (tutorialController == null)
            tutorialController = FindFirstObjectByType<TutorialController>();
    }

    private void Start()
    {
        BuildItems();

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;

        if (tutorialController != null)
            tutorialController.UnregisterJsonCallouts(this);
    }

    private void HandleResourceChanged(string resourceId, int newAmount)
    {
        var material = MaterialDefinition.Get(resourceId);
        if (material == null || !material.isRaw) return;
        BuildItems();
    }

    private void BuildItems()
    {
        if (tutorialController != null)
            tutorialController.UnregisterJsonCallouts(this);

        foreach (var item in generatedItems)
        {
            if (item == null) continue;
            item.SetActive(false);
            Destroy(item);
        }
        generatedItems.Clear();

        // 只显示原始材料（isRaw=true），不显示机器产出
        // Definitions describe what can exist; the palette itself is a live view
        // of the dynamic warehouse and must hide zero-owned monster materials.
        var materials = new List<MaterialDefinition.MatInfo>();
        foreach (var m in MaterialDefinition.All)
            if (m.isRaw && InventorySystem.Instance != null && InventorySystem.Instance.GetAmount(m.id) > 0)
                materials.Add(m);
        if (materials.Count == 0) return;

        // Ensure GridLayoutGroup (不覆盖面板尺寸)
        var glg = GetComponent<GridLayoutGroup>();
        if (glg == null)
        {
            var oldVlg = GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null) Destroy(oldVlg);
            var oldHlg = GetComponent<HorizontalLayoutGroup>();
            if (oldHlg != null) Destroy(oldHlg);
            glg = gameObject.AddComponent<GridLayoutGroup>();
        }
        glg.cellSize = new Vector2(cellSize.x, cellSize.y + labelHeight);
        glg.spacing = spacing;
        glg.padding = new RectOffset(5, 5, 5, 5);
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        glg.constraintCount = constraintCount;
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;

        // 排除非材料子物体（如背景图"架子"）不受 LayoutGroup 排版影响
        for (int c = 0; c < transform.childCount; c++)
        {
            var child = transform.GetChild(c);
            if (child.name.StartsWith("Mat_")) continue;

            // 加 LayoutElement.ignoreLayout 使其不被排版
            var le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // 移到最前面（渲染在材料图标后面）
            child.SetAsFirstSibling();
        }

        for (int i = 0; i < materials.Count; i++)
        {
            var mat = materials[i];

            var itemGO = new GameObject($"Mat_{mat.id}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            itemGO.transform.SetParent(transform, false);
            generatedItems.Add(itemGO);

            var img = itemGO.GetComponent<Image>();
            var sprite = MaterialDefinition.GetSprite(mat.id);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = mat.color;
            }
            img.raycastTarget = true;

            // Label at bottom
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGO.transform.SetParent(itemGO.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 0f);
            labelRT.pivot = new Vector2(0.5f, 0f);
            labelRT.sizeDelta = new Vector2(0f, labelHeight);
            labelRT.anchoredPosition = Vector2.zero;
            var label = labelGO.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.LowerCenter;
            label.color = Color.white;
            label.text = mat.displayName;
            label.raycastTarget = false;

            var paletteItem = itemGO.AddComponent<MaterialPaletteItem>();
            paletteItem.Setup(mat.id);

            var tutorial = ResourceDataLoader.Instance?.GetResource(mat.id)?.tutorial;
            if (tutorialController != null)
                tutorialController.RegisterJsonCallout(this, itemGO.GetComponent<RectTransform>(), tutorial);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
}