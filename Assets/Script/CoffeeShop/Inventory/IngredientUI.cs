using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class IngredientUI : MonoBehaviour
{
    [SerializeField] private string[] excludedResourceIds = { "gold" };
    [SerializeField] private float itemHeight = 32f;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color bgColor = new(0.15f, 0.1f, 0.05f, 0.8f);

    private const int Copies = 3;

    private ScrollRect _scrollRect;
    private RectTransform _contentRT;
    private readonly List<(string id, string displayName)> _resources = new();
    private readonly Dictionary<string, int> _amounts = new();
    private readonly List<Text> _itemTexts = new();
    private int _realCount;
    private float _oneSetHeight;

    private void Start()
    {
        SetupUI();
        RefreshData();
        Populate();

        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnResourceChanged += HandleResourceChanged;
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnResourceChanged -= HandleResourceChanged;
    }

    private void LateUpdate()
    {
        if (_scrollRect == null || _contentRT == null || _realCount == 0) return;

        float y = _contentRT.anchoredPosition.y;
        if (y >= _oneSetHeight * 2)
            _contentRT.anchoredPosition -= new Vector2(0, _oneSetHeight);
        else if (y < _oneSetHeight)
            _contentRT.anchoredPosition += new Vector2(0, _oneSetHeight);
    }

    private void SetupUI()
    {
        var bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = bgColor;

        _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null) _scrollRect = gameObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        _scrollRect.scrollSensitivity = 20f;

        // Viewport
        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(transform, false);
        var vpRT = viewportGo.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vpRT.pivot = new(0.5f, 0.5f);
        var vpImage = viewportGo.GetComponent<Image>();
        vpImage.color = Color.white;
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        // Content
        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer));
        contentGo.transform.SetParent(viewportGo.transform, false);
        _contentRT = contentGo.GetComponent<RectTransform>();
        _contentRT.anchorMin = new(0, 1); _contentRT.anchorMax = new(1, 1);
        _contentRT.pivot = new(0.5f, 1);
        _contentRT.offsetMin = Vector2.zero; _contentRT.offsetMax = Vector2.zero;

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 0;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.content = _contentRT;
        _scrollRect.viewport = vpRT;

        var oldText = transform.Find("Text");
        if (oldText != null) Destroy(oldText.gameObject);
    }

    private void RefreshData()
    {
        _resources.Clear();
        _amounts.Clear();

        if (InventorySystem.Instance == null) return;

        foreach (var kvp in InventorySystem.Instance.GetSnapshot())
        {
            if (IsExcluded(kvp.Key)) continue;
            _amounts[kvp.Key] = kvp.Value;
        }

        if (ResourceDataLoader.Instance != null)
        {
            foreach (var res in ResourceDataLoader.Instance.GetAllResources())
            {
                if (IsExcluded(res.id)) continue;
                if (!_resources.Exists(r => r.id == res.id))
                    _resources.Add((res.id, res.displayName));
            }
        }

        foreach (var kvp in _amounts)
        {
            if (!_resources.Exists(r => r.id == kvp.Key))
                _resources.Add((kvp.Key, kvp.Key));
        }

        _realCount = _resources.Count;
        _oneSetHeight = _realCount * itemHeight;
    }

    private void Populate()
    {
        for (int i = _contentRT.childCount - 1; i >= 0; i--)
            Destroy(_contentRT.GetChild(i).gameObject);
        _itemTexts.Clear();

        for (int c = 0; c < Copies; c++)
        {
            for (int i = 0; i < _realCount; i++)
            {
                var go = new GameObject($"Item_{i}_{c}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(_contentRT, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new(0, itemHeight);

                var text = go.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = fontSize;
                text.color = textColor;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Truncate;

                UpdateItemText(text, i);
                _itemTexts.Add(text);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRT);
        _contentRT.anchoredPosition = new Vector2(0, _oneSetHeight);
    }

    private void UpdateItemText(Text text, int resourceIndex)
    {
        if (resourceIndex < 0 || resourceIndex >= _resources.Count) return;
        var (id, displayName) = _resources[resourceIndex];
        int amount = _amounts.GetValueOrDefault(id, 0);
        text.text = $"{displayName}: {amount}";
    }

    private void HandleResourceChanged(string resourceId, int newAmount)
    {
        if (IsExcluded(resourceId)) return;
        _amounts[resourceId] = newAmount;

        for (int i = 0; i < _resources.Count; i++)
        {
            if (_resources[i].id == resourceId)
            {
                for (int c = 0; c < Copies; c++)
                {
                    int idx = c * _realCount + i;
                    if (idx < _itemTexts.Count)
                        UpdateItemText(_itemTexts[idx], i);
                }
            }
        }
    }

    private bool IsExcluded(string resourceId)
    {
        if (excludedResourceIds == null) return false;
        foreach (var id in excludedResourceIds)
            if (id == resourceId) return true;
        return false;
    }
}
