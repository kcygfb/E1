using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoffeeListPopulator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform content;
    [SerializeField] private CraftController craftController;
    [SerializeField] private Sprite coffeeIconSprite;

    [Header("Item Layout")]
    [SerializeField] private Vector2 itemSize = new(180, 180);
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Color lockedColor = new(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private void Start()
    {
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
        var go = new GameObject(coffeeJson.coffeeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = coffeeJson.coffeeName;
        text.fontSize = fontSize;
        text.color = locked ? lockedColor : normalColor;
        text.alignment = TextAnchor.MiddleCenter;
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
