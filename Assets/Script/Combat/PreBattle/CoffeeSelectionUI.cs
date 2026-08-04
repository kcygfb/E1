using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// PreBattle 咖啡选择 UI：弹窗横向列表 + 底部栏位，类似 CardSelectionUI 的选牌模式。
    /// 点击咖啡弹窗里的项 → 加入底部栏位（最多 2 杯）。
    /// </summary>
    public class CoffeeSelectionUI : MonoBehaviour
    {
        private static readonly string[] AvailableCoffeeIds = { "PourOver", "BloodGarment" };

        private static Font _uiFont;
        private static Font UIFont
        {
            get
            {
                if (_uiFont == null)
                    _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _uiFont;
            }
        }

        private static Text CreateText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            if (UIFont != null) txt.font = UIFont;
            txt.raycastTarget = false;
            return txt;
        }

        [Header("弹窗")]
        [SerializeField] private GameObject coffeePopup;
        [SerializeField] private Transform coffeeListContent;
        [SerializeField] private GameObject coffeeItemPrefab;

        [Header("咖啡图标（手动指定）")]
        [SerializeField] private Sprite pourOverIcon;
        [SerializeField] private Sprite bloodGarmentIcon;
        [SerializeField] private Vector2 iconSize = new(120, 120);

        [Header("栏位")]
        [SerializeField] private Transform coffeeSlotContent;
        [SerializeField] private Text coffeeCountLabel;

        [Header("按钮")]
        [SerializeField] private Button openPopupButton;
        [SerializeField] private Button beginButton;

        private const int MaxCoffees = 2;
        private readonly List<string> selectedCoffeeIds = new();

        private Sprite GetIconForCoffee(string coffeeId)
        {
            return coffeeId switch
            {
                "PourOver" => pourOverIcon,
                "BloodGarment" => bloodGarmentIcon,
                _ => null,
            };
        }

        private void Start()
        {
            if (openPopupButton != null)
                openPopupButton.onClick.AddListener(OpenPopup);
            if (beginButton != null)
                beginButton.interactable = false;

            BindCloseButton();

            PopulateCoffeeList();
            RefreshUI();
        }

        private void OpenPopup()
        {
            if (coffeePopup != null)
                coffeePopup.SetActive(true);
        }

        private void BindCloseButton()
        {
            if (coffeePopup == null) return;
            var closeBtn = coffeePopup.transform.Find("CloseBtn");
            if (closeBtn != null)
            {
                var btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => coffeePopup.SetActive(false));
            }
        }

        // ─── 弹窗列表 ───

        private void PopulateCoffeeList()
        {
            if (coffeeListContent == null) return;

            for (int i = coffeeListContent.childCount - 1; i >= 0; i--)
                Destroy(coffeeListContent.GetChild(i).gameObject);

            foreach (var coffeeId in AvailableCoffeeIds)
            {
                GameObject go;
                if (coffeeItemPrefab != null)
                {
                    go = Instantiate(coffeeItemPrefab, coffeeListContent);
                    ApplyCoffeeDataToItem(go, coffeeId);
                }
                else
                    go = CreateDefaultItem(coffeeId);

                go.name = coffeeId;

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                var id = coffeeId;
                btn.onClick.AddListener(() => OnCoffeeClicked(id));

                UpdateItemHighlight(go, coffeeId);
            }
        }

        /// <summary>预制体实例化后，按 coffeeId 设置图标/名字/描述。</summary>
        private void ApplyCoffeeDataToItem(GameObject go, string coffeeId)
        {
            var icon = go.transform.Find("Icon");
            if (icon != null)
            {
                var img = icon.GetComponent<Image>();
                if (img != null)
                {
                    var sprite = GetIconForCoffee(coffeeId);
                    if (sprite != null) img.sprite = sprite;
                }
            }
            var nameT = go.transform.Find("Name");
            if (nameT != null)
            {
                var txt = nameT.GetComponent<Text>();
                if (txt != null) txt.text = CoffeeEffectRegistry.GetDisplayName(coffeeId);
            }
            var descT = go.transform.Find("Desc");
            if (descT != null)
            {
                var txt = descT.GetComponent<Text>();
                if (txt != null) txt.text = GetCoffeeDescription(coffeeId);
            }
        }

        private GameObject CreateDefaultItem(string coffeeId)
        {
            // 根容器
            var go = new GameObject(coffeeId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(coffeeListContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 200);
            go.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.14f, 1);

            // 咖啡图标
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRT = iconGo.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1f);
            iconRT.anchorMax = new Vector2(0.5f, 1f);
            iconRT.pivot = new Vector2(0.5f, 1f);
            iconRT.anchoredPosition = new Vector2(0f, -5f);
            iconRT.sizeDelta = iconSize;
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var icon = GetIconForCoffee(coffeeId);
            if (icon != null) iconImg.sprite = icon;

            // 名字
            var nameTxt = CreateText("Name", go.transform);
            var nameRT = nameTxt.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.28f);
            nameRT.anchorMax = new Vector2(1f, 0.45f);
            nameRT.offsetMin = new Vector2(2f, 0f);
            nameRT.offsetMax = new Vector2(-2f, 0f);
            nameTxt.text = CoffeeEffectRegistry.GetDisplayName(coffeeId);
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.fontSize = 16;
            nameTxt.color = new Color(0.9f, 0.85f, 0.7f, 1);
            nameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // 描述
            var descTxt = CreateText("Desc", go.transform);
            var descRT = descTxt.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0f, 0f);
            descRT.anchorMax = new Vector2(1f, 0.28f);
            descRT.offsetMin = new Vector2(2f, 3f);
            descRT.offsetMax = new Vector2(-2f, 0f);
            descTxt.text = GetCoffeeDescription(coffeeId);
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.fontSize = 12;
            descTxt.color = new Color(0.6f, 0.6f, 0.6f, 1);
            descTxt.raycastTarget = false;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            return go;
        }

        private static string GetCoffeeDescription(string coffeeId)
        {
            if (!CoffeeEffectRegistry.TryGet(coffeeId, out var effect)) return "";
            var target = effect.Target == CoffeeTarget.Self ? "自身" : "敌人";
            return effect.Type switch
            {
                CoffeeEffectType.Heal => $"回复 {effect.Amount} HP ({target})",
                CoffeeEffectType.Bleed => $"流血 {effect.Amount} 层 ({target})",
                CoffeeEffectType.Block => $"护盾 {effect.Amount} ({target})",
                CoffeeEffectType.Damage => $"伤害 {effect.Amount} ({target})",
                _ => effect.Type.ToString(),
            };
        }

        // ─── 点击 ───

        private void OnCoffeeClicked(string coffeeId)
        {
            if (selectedCoffeeIds.Contains(coffeeId))
            {
                selectedCoffeeIds.Remove(coffeeId);
            }
            else if (selectedCoffeeIds.Count < MaxCoffees)
            {
                selectedCoffeeIds.Add(coffeeId);
            }
            RefreshUI();
        }

        // ─── UI 刷新 ───

        private void RefreshUI()
        {
            UpdateCoffeeCountLabel();
            UpdateCoffeeSlots();
            UpdateListHighlights();

            if (beginButton != null)
                beginButton.interactable = selectedCoffeeIds.Count == MaxCoffees;
        }

        private void UpdateCoffeeCountLabel()
        {
            if (coffeeCountLabel != null)
                coffeeCountLabel.text = $"咖啡 ({selectedCoffeeIds.Count}/{MaxCoffees})";
        }

        private void UpdateCoffeeSlots()
        {
            if (coffeeSlotContent == null) return;

            for (int i = 0; i < coffeeSlotContent.childCount; i++)
            {
                var slot = coffeeSlotContent.GetChild(i);
                if (i >= MaxCoffees) { slot.gameObject.SetActive(false); continue; }
                slot.gameObject.SetActive(true);

                var img = slot.GetComponent<Image>();
                var label = slot.GetComponentInChildren<Text>(true);

                if (i < selectedCoffeeIds.Count)
                {
                    var coffeeId = selectedCoffeeIds[i];
                    if (label != null)
                    {
                        label.text = CoffeeEffectRegistry.GetDisplayName(coffeeId);
                        label.fontSize = 14;
                        label.color = new Color(0.9f, 0.85f, 0.7f, 1);
                    }
                    if (img != null)
                        img.color = new Color(0.2f, 0.18f, 0.14f, 1);
                }
                else
                {
                    if (label != null)
                    {
                        label.text = "+";
                        label.fontSize = 24;
                        label.color = new Color(0.3f, 0.3f, 0.35f, 1);
                    }
                    if (img != null)
                        img.color = new Color(0.12f, 0.12f, 0.15f, 1);
                }
            }
        }

        private void UpdateListHighlights()
        {
            if (coffeeListContent == null) return;
            for (int i = 0; i < coffeeListContent.childCount; i++)
            {
                var child = coffeeListContent.GetChild(i);
                UpdateItemHighlight(child.gameObject, child.name);
            }
        }

        private void UpdateItemHighlight(GameObject go, string coffeeId)
        {
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.color = selectedCoffeeIds.Contains(coffeeId)
                ? new Color(0.3f, 0.25f, 0.15f, 1)
                : new Color(0.18f, 0.16f, 0.14f, 1);
        }

        /// <summary>由 CardSelectionUI.OnBeginClicked 调用。</summary>
        public void ConfirmSelection()
        {
            if (selectedCoffeeIds.Count != MaxCoffees) return;
            BattleSession.SetSelectedCoffees(selectedCoffeeIds);
        }
    }
}
