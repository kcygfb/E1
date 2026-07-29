using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KiKs.UI;

public class CoffeeListPopulator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform content;
    [SerializeField] private CraftController craftController;
    [SerializeField] private Sprite coffeeIconSprite;

    [Header("Item Layout")]
    [SerializeField] private Vector2 itemSize = new(180, 180);
    [Tooltip("按钮之间的间距")]
    [SerializeField] private float itemSpacing = 60f;
    [SerializeField] private Color lockedColor = new(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    [Header("文字")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float fontSize = 24f;
    [Tooltip("字体粗细")]
    [SerializeField] private FontWeight fontWeight = FontWeight.Regular;
    [Tooltip("文字框大小")]
    [SerializeField] private Vector2 textSize = new(200, 60);
    [Tooltip("文字相对于图标右下角的偏移")]
    [SerializeField] private Vector2 textOffset = new(-10f, 10f);
    [Tooltip("文字锚点：1=右下角，0=居中")]
    [Range(0f, 1f)]
    [SerializeField] private float textAnchorX = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float textAnchorY = 0f;
    [Tooltip("未锁定状态的文字颜色")]
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("锁定状态的文字颜色")]
    [SerializeField] private Color lockedTextColor = new(0.4f, 0.4f, 0.4f, 1f);

    private void Start()
    {
        if (content != null)
        {
            var hlg = content.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.spacing = itemSpacing;
        }
        Populate();
    }

    private void Populate()
    {
        if (content == null) { Debug.LogError("[CoffeeListPopulator] content not assigned"); return; }

        var loader = CoffeeDataLoader.Instance;
        if (loader == null || !loader.IsLoaded)
        {
            Debug.LogError("[CoffeeListPopulator] CoffeeDataLoader not ready");
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (var coffeeJson in loader.GetAllCoffees())
            CreateCoffeeItem(coffeeJson);
    }

    private void CreateCoffeeItem(CoffeeDataJson coffeeJson)
    {
        var go = new GameObject(coffeeJson.coffeeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonFeedback));
        go.transform.SetParent(content, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = itemSize;

        var image = go.GetComponent<Image>();
        image.sprite = coffeeIconSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        var button = go.GetComponent<Button>();

        bool locked = coffeeJson.locked;
        if (locked && UnlockManager.Instance != null)
        {
            var tempData = ScriptableObject.CreateInstance<CoffeeData>();
            tempData.coffeeId = coffeeJson.coffeeId;
            tempData.locked = coffeeJson.locked;
            tempData.unlockItemId = coffeeJson.unlockItemId;
            tempData.unlockAmount = coffeeJson.unlockAmount;
            locked = !UnlockManager.Instance.IsUnlocked(tempData);
            Destroy(tempData);
        }

        image.color = locked ? lockedColor : normalColor;

        // 锁定按钮禁用动效
        var feedback = go.GetComponent<ButtonFeedback>();
        if (feedback != null) feedback.enabled = !locked;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textGo.transform.SetParent(go.transform, false);
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(textAnchorX, textAnchorY);
        textRT.anchorMax = new Vector2(textAnchorX, textAnchorY);
        textRT.pivot = new Vector2(textAnchorX, textAnchorY);
        textRT.anchoredPosition = textOffset;
        textRT.sizeDelta = textSize;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) text.font = fontAsset;
        text.text = coffeeJson.coffeeName;
        text.fontSize = fontSize;
        text.fontWeight = fontWeight;
        text.enableAutoSizing = false;
        text.color = locked ? lockedTextColor : textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        if (!locked)
        {
            var captured = coffeeJson;
            button.onClick.AddListener(() => OnCoffeeClicked(captured));
        }
        else
        {
            button.interactable = false;
        }
    }

    private void OnCoffeeClicked(CoffeeDataJson coffeeJson)
    {
        var coffeeData = ScriptableObject.CreateInstance<CoffeeData>();
        coffeeData.ApplyJson(coffeeJson);

        if (craftController != null)
        {
            craftController.OnCoffeeSelected(coffeeData);
        }
        else
        {
            var orderSystem = FindFirstObjectByType<OrderSystem>();
            if (orderSystem != null)
                orderSystem.TryServeCoffee(coffeeData);
            else
                GameEvent.Emit("CoffeeServed", coffeeData);
        }
    }
}
