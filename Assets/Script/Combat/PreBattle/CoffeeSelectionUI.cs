using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using KiKs.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// PreBattle 咖啡选择 UI：弹窗横向列表 + 底部栏位。
    /// 列表来源：当天在咖啡店实际制作并成功交付过的咖啡种类（RuntimeGameRepository.CraftedCoffeeIds）。
    /// 仅显示有战斗效果的咖啡（CoffeeEffectRegistry.HasEffect）。
    /// 如果当天没有做过任何有战斗效果的咖啡，回退到 PourOver/BloodGarment 防止卡流程。
    /// </summary>
    public class CoffeeSelectionUI : MonoBehaviour
    {
        private static readonly string[] FallbackCoffeeIds = { "PourOver", "BloodGarment" };

        [System.Serializable]
        private sealed class CoffeeTutorialJson
        {
            public string coffeeId;
            public TutorialHintJson tutorial;
        }

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

        [Header("咖啡图标尺寸")]
        [SerializeField] private Vector2 iconSize = new(120, 120);

        [Header("栏位")]
        [SerializeField] private Transform coffeeSlotContent;
        [SerializeField] private Text coffeeCountLabel;

        [Header("按钮")]
        [SerializeField] private Button openPopupButton;
        [SerializeField] private Button openPopupButton2;

        [Header("Tutorial")]
        [SerializeField] private TutorialController tutorialController;
        [SerializeField] private string coffeeTutorialDirectory = "CoffeeData";

        private const int MaxCoffees = 2;
        private readonly List<string> selectedCoffeeIds = new();
        private readonly Dictionary<string, TutorialHintJson> coffeeTutorials = new();
        private List<string> availableCoffeeIds = new();

        /// <summary>CoffeeIconCache 在 Assembly-CSharp，KiKs.Combat 不能直接引用，用反射调 GetCoffeeSprite。</summary>
        private static Sprite GetCachedCoffeeSprite(string coffeeId)
        {
            var cacheType = Type.GetType("CoffeeIconCache, Assembly-CSharp");
            if (cacheType == null) return null;
            var instanceProp = cacheType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProp?.GetValue(null);
            if (instance == null) return null;
            var method = cacheType.GetMethod("GetCoffeeSprite", BindingFlags.Public | BindingFlags.Instance);
            return method?.Invoke(instance, new object[] { coffeeId }) as Sprite;
        }

        private Sprite GetIconForCoffee(string coffeeId) => GetCachedCoffeeSprite(coffeeId);

        public bool IsSelectionComplete => selectedCoffeeIds.Count == MaxCoffees;

        private void Start()
        {
            CoffeeEffectRegistry.Load();

            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();

            if (openPopupButton != null)
                openPopupButton.onClick.AddListener(OpenPopup);
            if (openPopupButton2 != null)
                openPopupButton2.onClick.AddListener(OpenPopup);

            BindCloseButton();

            BuildAvailableCoffeeIds();
            EnsureDefaultPreselection();

            PopulateCoffeeList();
            RefreshUI();
        }

        /// <summary>
        /// 从 RuntimeGameRepository.CraftedCoffeeIds 构建可用列表，仅保留有战斗效果的。
        /// 如果列表为空，回退到 FallbackCoffeeIds。
        /// </summary>
        private void BuildAvailableCoffeeIds()
        {
            availableCoffeeIds.Clear();

            if (RuntimeGameRepository.HasCraftedCoffees)
            {
                foreach (var coffeeId in RuntimeGameRepository.CraftedCoffeeIds)
                {
                    if (CoffeeEffectRegistry.HasEffect(coffeeId))
                        availableCoffeeIds.Add(coffeeId);
                }
            }

            if (availableCoffeeIds.Count == 0)
                availableCoffeeIds.AddRange(FallbackCoffeeIds);
        }

        /// <summary>
        /// 进入战备界面时自动预选默认咖啡（玩家仍可在弹窗里随意更改）。
        /// </summary>
        private void EnsureDefaultPreselection()
        {
            if (selectedCoffeeIds.Count > 0) return;

            for (int i = 0; i < availableCoffeeIds.Count && selectedCoffeeIds.Count < MaxCoffees; i++)
                selectedCoffeeIds.Add(availableCoffeeIds[i]);
        }

        private void OnDestroy()
        {
            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);
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

            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);

            LoadCoffeeTutorials();

            for (int i = coffeeListContent.childCount - 1; i >= 0; i--)
                Destroy(coffeeListContent.GetChild(i).gameObject);

            foreach (var coffeeId in availableCoffeeIds)
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

                var tutorial = GetCoffeeTutorial(coffeeId);
                if (tutorialController != null)
                    tutorialController.RegisterJsonCallout(
                        this,
                        go.GetComponent<RectTransform>(),
                        tutorial);
            }
        }

        private void LoadCoffeeTutorials()
        {
            coffeeTutorials.Clear();
            if (string.IsNullOrWhiteSpace(coffeeTutorialDirectory)) return;

            var directory = Path.Combine(Application.streamingAssetsPath, coffeeTutorialDirectory);
            if (!Directory.Exists(directory)) return;

            foreach (var filePath in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var data = JsonUtility.FromJson<CoffeeTutorialJson>(File.ReadAllText(filePath));
                    if (data != null && !string.IsNullOrWhiteSpace(data.coffeeId))
                        coffeeTutorials[data.coffeeId] = data.tutorial;
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"[CoffeeSelectionUI] Cannot read tutorial data from {Path.GetFileName(filePath)}: {exception.Message}", this);
                }
            }
        }

        private TutorialHintJson GetCoffeeTutorial(string coffeeId)
        {
            return coffeeTutorials.TryGetValue(coffeeId, out var tutorial) ? tutorial : null;
        }

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
                CoffeeEffectType.Bleed => $"流血 {effect.Amount} 回合 ({target})",
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
            else
            {
                WarningToast.Show(string.Format("Coffee limit reached: {0}.", MaxCoffees));
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            UpdateCoffeeCountLabel();
            UpdateCoffeeSlots();
            UpdateListHighlights();

            var cardUI = FindFirstObjectByType<CardSelectionUI>();
            if (cardUI != null)
                cardUI.RefreshSelectionUI();
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
                var slotIcon = GetOrCreateSlotIcon(slot);

                if (i < selectedCoffeeIds.Count)
                {
                    var coffeeId = selectedCoffeeIds[i];
                    if (label != null)
                        label.text = "";
                    if (img != null)
                        img.color = new Color(0.2f, 0.18f, 0.14f, 1);

                    // 显示咖啡图标
                    if (slotIcon != null)
                    {
                        var sprite = GetIconForCoffee(coffeeId);
                        if (sprite != null)
                        {
                            slotIcon.sprite = sprite;
                            slotIcon.enabled = true;
                        }
                        else
                        {
                            slotIcon.enabled = false;
                        }
                    }
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

                    if (slotIcon != null)
                        slotIcon.enabled = false;
                }
            }
        }

        /// <summary>获取或创建 slot 下的 Icon Image（用于显示咖啡图标）。</summary>
        private Image GetOrCreateSlotIcon(Transform slot)
        {
            var existing = slot.Find("SlotIcon");
            if (existing != null)
                return existing.GetComponent<Image>();

            var go = new GameObject("SlotIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slot, false);
            go.transform.SetAsFirstSibling(); // 图标在背景之上、文字之下

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4, 20);   // 底部留空给文字
            rt.offsetMax = new Vector2(-4, -4);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = false;

            return img;
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
            // 进游戏前兜底补满默认咖啡，保证无论玩家如何更改，出战前始终带满 2 杯
            if (selectedCoffeeIds.Count < MaxCoffees)
            {
                foreach (var coffeeId in availableCoffeeIds)
                {
                    if (selectedCoffeeIds.Count >= MaxCoffees) break;
                    if (!selectedCoffeeIds.Contains(coffeeId))
                        selectedCoffeeIds.Add(coffeeId);
                }
            }

            if (selectedCoffeeIds.Count != MaxCoffees) return;
            RuntimeGameRepository.SetSelectedCoffees(selectedCoffeeIds);
        }
    }
}
