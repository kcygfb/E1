using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>材料槽。接收任意 MaterialIcon，支持多个堆叠，机器处理后清空全部。</summary>
[RequireComponent(typeof(Image))]
public class MaterialSlot : MonoBehaviour
{
    public List<MaterialIcon> Icons { get; } = new();
    public bool IsFilled => Icons.Count > 0;

    private Image _slotImage;
    private Text _hintLabel;

    private void Awake()
    {
        _slotImage = GetComponent<Image>();
        EnsureHintLabel();
    }

    private void EnsureHintLabel()
    {
        var labelT = transform.Find("HintLabel");
        if (labelT == null)
        {
            var go = new GameObject("HintLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = "放材料";
            _hintLabel = label;
        }
        else
        {
            _hintLabel = labelT.GetComponent<Text>();
        }
    }

    public bool Accept(MaterialIcon icon)
    {
        if (icon == null) return false;
        Icons.Add(icon);
        icon.transform.SetParent(transform, false);
        LayoutIcons();
        if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
        return true;
    }

    /// <summary>获取第一个 Icon 的 MaterialId（机器处理用）。</summary>
    public string GetMaterialId() => Icons.Count > 0 ? Icons[0].MaterialId : null;

    /// <summary>获取所有 Icon 的 MaterialId 列表。</summary>
    public List<string> GetAllMaterialIds()
    {
        var ids = new List<string>();
        foreach (var icon in Icons)
            if (icon != null) ids.Add(icon.MaterialId);
        return ids;
    }

    public void Remove(MaterialIcon icon)
    {
        Icons.Remove(icon);
        LayoutIcons();
        if (Icons.Count == 0 && _hintLabel != null)
            _hintLabel.gameObject.SetActive(true);
    }

    private void LayoutIcons()
    {
        for (int i = 0; i < Icons.Count; i++)
        {
            if (Icons[i] == null) continue;
            // 堆叠：稍微偏移，第1个居中，后续向右下错开
            float offset = (i - (Icons.Count - 1) * 0.5f) * 15f;
            Icons[i].transform.localPosition = new Vector3(offset, -offset * 0.5f, 0);
        }
    }

    public void Clear()
    {
        foreach (var icon in Icons)
            if (icon != null) Destroy(icon.gameObject);
        Icons.Clear();
        if (_hintLabel != null) _hintLabel.gameObject.SetActive(true);
    }
}
