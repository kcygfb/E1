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
    [Tooltip("咖啡按钮模板 Prefab，留空则用代码生成")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Tutorial")]
    [SerializeField] private TutorialController tutorialController;

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

    private void Awake()
    {
        if (tutorialController == null)
            tutorialController = FindFirstObjectByType<TutorialController>();
    }

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

        if (tutorialController != null)
            tutorialController.UnregisterJsonCallouts(this);

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        foreach (var coffeeJson in loader.GetAllCoffees())
            CreateCoffeeItem(coffeeJson);
    }

    private void CreateCoffeeItem(CoffeeDataJson coffeeJson)
    {
        GameObject go;
        if (itemPrefab != null)
        {
            go = Instantiate(itemPrefab, content, false);
            go.name = coffeeJson.coffeeId;
        }
        else
        {
            go = new GameObject(coffeeJson.coffeeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonFeedback));
            go.transform.SetParent(content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = itemSize;

            var img = go.GetComponent<Image>();
            img.sprite = coffeeIconSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(textAnchorX, textAnchorY);
            textRT.anchorMax = new Vector2(textAnchorX, textAnchorY);
            textRT.pivot = new Vector2(textAnchorX, textAnchorY);
            textRT.anchoredPosition = textOffset;
            textRT.sizeDelta = textSize;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.fontSize = fontSize;
            tmp.fontWeight = fontWeight;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

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

        var image = go.GetComponent<Image>();
        if (coffeeIconSprite != null) image.sprite = coffeeIconSprite;
        image.color = locked ? lockedColor : normalColor;

        var feedback = go.GetComponent<ButtonFeedback>();
        if (feedback != null) feedback.enabled = !locked;

        // 设置文字（Prefab 模式下找已有 Text 子物体，代码生成模式下在上面已创建）
        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = coffeeJson.coffeeName;
            text.color = locked ? lockedTextColor : textColor;
        }

        var button = go.GetComponent<Button>();
        if (!locked)
        {
            var captured = coffeeJson;
            button.onClick.AddListener(() => OnCoffeeClicked(captured));
        }
        else
        {
            button.interactable = false;
        }

        if (tutorialController != null)
            tutorialController.RegisterJsonCallout(
                this,
                go.GetComponent<RectTransform>(),
                coffeeJson.tutorial);
    }

    private void OnCoffeeClicked(CoffeeDataJson coffeeJson)
    {
        var coffeeData = ScriptableObject.CreateInstance<CoffeeData>();
        coffeeData.ApplyJson(coffeeJson);

        if (craftController != null)
        {
            // Free craft mode — no need to select coffee manually
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
