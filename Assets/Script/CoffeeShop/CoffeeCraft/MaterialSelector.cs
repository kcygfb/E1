using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;
using KiKs.UI;

public class MaterialSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button cancelBtn;

    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorialController;

    [Header("Layout")]
    [SerializeField] private float itemWidth = 140f;
    [SerializeField] private float itemSpacing = 20f;

    private readonly List<ResourceJson> _materials = new();
    private readonly List<GameObject> _items = new();
    private int _selectedIndex;
    private bool _isSnapping;
    private float _totalItemWidth;
    private int _baseCount;

    public System.Action<string> OnConfirm;
    public System.Action OnCancel;

    private void Awake()
    {
        if (tutorialController == null)
            tutorialController = FindFirstObjectByType<TutorialController>();

        if (confirmBtn != null) confirmBtn.onClick.AddListener(ConfirmSelection);
        if (cancelBtn != null) cancelBtn.onClick.AddListener(CancelSelection);
    }

    private void OnDestroy()
    {
        if (scrollRect != null) scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        if (tutorialController != null)
            tutorialController.UnregisterJsonCallouts(this);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;
        if (_isSnapping) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.LeftArrow].wasPressedThisFrame)
            SnapToIndex(_selectedIndex - 1);
        else if (kb[Key.RightArrow].wasPressedThisFrame)
            SnapToIndex(_selectedIndex + 1);
    }

    public void Show()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(ConfirmSelection);
        }
        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(CancelSelection);
        }
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
        }

        LoadMaterials();
        BuildItems();
        gameObject.SetActive(true);

        _selectedIndex = 0;
        SnapToIndex(0, instant: true);
        UpdateNameLabel();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        ClearItems();
    }

    private void LoadMaterials()
    {
        _materials.Clear();
        if (ResourceDataLoader.Instance == null) return;

        foreach (var res in ResourceDataLoader.Instance.GetAllResources())
        {
            if (res.id == "gold") continue;
            _materials.Add(res);
        }

        if (_materials.Count == 0)
            _materials.Add(new ResourceJson { id = "CoffeeBean", displayName = "Coffee Bean", startingAmount = 0 });

        _baseCount = _materials.Count;
        _totalItemWidth = itemWidth + itemSpacing;
    }

    private void BuildItems()
    {
        ClearItems();
        if (content == null) return;

        // Build only one set of items — we'll loop by repositioning
        for (int i = 0; i < _baseCount; i++)
        {
            var mat = _materials[i];
            var go = new GameObject($"Item_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(content, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(itemWidth, itemWidth);
            // Position each item side by side starting from left
            rt.anchoredPosition = new Vector2(i * _totalItemWidth + itemWidth / 2f, 0f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.8f, 0.7f, 0.5f, 1f);
            img.raycastTarget = false;

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            var libFont = TMPro.TMP_Settings.defaultFontAsset;
            if (libFont != null) tmp.font = libFont;
            tmp.text = mat.displayName;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            _items.Add(go);

            if (tutorialController != null)
                tutorialController.RegisterJsonCallout(this, rt, mat.tutorial);
        }

        // Set content width to match total items width
        var contentRT = content as RectTransform;
        if (contentRT != null)
        {
            contentRT.sizeDelta = new Vector2(_baseCount * _totalItemWidth, contentRT.sizeDelta.y);
        }
    }

    private void ClearItems()
    {
        if (tutorialController != null)
            tutorialController.UnregisterJsonCallouts(this);

        foreach (var item in _items)
        {
            if (item != null) Destroy(item);
        }
        _items.Clear();
    }

    /// <summary>Get the centered X position for content given a logical index</summary>
    private float GetContentXForIndex(int index)
    {
        float viewportWidth = scrollRect.viewport.rect.width;
        return -(index * _totalItemWidth) + viewportWidth / 2f - _totalItemWidth / 2f;
    }

    private void OnScrollChanged(Vector2 pos)
    {
        if (_isSnapping || _items.Count == 0 || scrollRect == null) return;

        var contentRT = content as RectTransform;
        if (contentRT == null) return;

        float viewportWidth = scrollRect.viewport.rect.width;
        float totalWidth = _baseCount * _totalItemWidth;

        // Reposition items so they always fill the viewport
        float contentX = contentRT.anchoredPosition.x;
        
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] == null) continue;
            var itemRT = _items[i].GetComponent<RectTransform>();
            
            // Base position of this item
            float baseX = i * _totalItemWidth + _totalItemWidth / 2f;
            
            // World position relative to viewport center
            float relX = baseX + contentX - viewportWidth / 2f;
            
            // Wrap: if item is more than half the total width away from center, shift it by total width
            while (relX < -totalWidth / 2f)
                relX += totalWidth;
            while (relX > totalWidth / 2f)
                relX -= totalWidth;
            
            // Set item position relative to content
            itemRT.anchoredPosition = new Vector2(relX + viewportWidth / 2f - contentX, 0f);
        }

        // Calculate which item is closest to center
        float rawIndex = -(contentX - viewportWidth / 2f + _totalItemWidth / 2f) / _totalItemWidth;
        int nearestIndex = Mathf.RoundToInt(rawIndex);
        int logicalIndex = ((nearestIndex % _baseCount) + _baseCount) % _baseCount;

        if (logicalIndex != _selectedIndex)
        {
            _selectedIndex = logicalIndex;
            UpdateNameLabel();
        }
    }

    private void SnapToIndex(int index, bool instant = false)
    {
        if (_baseCount == 0) return;

        // Wrap index for infinite scrolling
        index = ((index % _baseCount) + _baseCount) % _baseCount;
        _selectedIndex = index;
        UpdateNameLabel();

        var contentRT = content as RectTransform;
        if (contentRT == null) return;

        float targetX = GetContentXForIndex(index);

        if (instant)
        {
            contentRT.anchoredPosition = new Vector2(targetX, contentRT.anchoredPosition.y);
        }
        else
        {
            _isSnapping = true;
            DOTween.To(
                () => contentRT.anchoredPosition.x,
                x => contentRT.anchoredPosition = new Vector2(x, contentRT.anchoredPosition.y),
                targetX, 0.25f).SetEase(Ease.OutCubic)
                .OnComplete(() => _isSnapping = false);
        }
    }

    private void UpdateNameLabel()
    {
        if (nameLabel != null && _selectedIndex >= 0 && _selectedIndex < _materials.Count)
            nameLabel.text = _materials[_selectedIndex].displayName;
    }

    private void ConfirmSelection()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _materials.Count) return;
        var selectedId = _materials[_selectedIndex].id;
        OnConfirm?.Invoke(selectedId);
        Hide();
    }

    private void CancelSelection()
    {
        OnCancel?.Invoke();
        Hide();
    }
}
