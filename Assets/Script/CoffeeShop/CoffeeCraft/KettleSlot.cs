using UnityEngine;
using UnityEngine.UI;

/// <summary>壶槽。手冲机器专用，接收 Kettle。</summary>
[RequireComponent(typeof(Image))]
public class KettleSlot : MonoBehaviour
{
    [Tooltip("场景中默认放在槽里的壶（可选）")]
    [SerializeField] private Kettle initialKettle;

    public Kettle Current { get; private set; }
    public bool IsFilled => Current != null;

    private Text _hintLabel;

    private void Awake()
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
            label.text = "放手冲壶";
            _hintLabel = label;
        }
        else
        {
            _hintLabel = labelT.GetComponent<Text>();
        }

        // 场景中预设的壶 → 启动时自动注册
        if (initialKettle != null)
        {
            Current = initialKettle;
            initialKettle.SetSlot(this);
            if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
        }
    }

    public bool Accept(Kettle kettle)
    {
        if (IsFilled) return false;
        Current = kettle;
        kettle.transform.SetParent(transform, false);
        kettle.transform.localPosition = Vector3.zero;
        if (_hintLabel != null) _hintLabel.gameObject.SetActive(false);
        return true;
    }

    public void Clear()
    {
        Current = null;
        if (_hintLabel != null) _hintLabel.gameObject.SetActive(true);
    }
}
