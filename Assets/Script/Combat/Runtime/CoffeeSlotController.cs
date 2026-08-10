using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using KiKs.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 咖啡道具槽：挂在 Card 场景的 CoffeeButton1 / CoffeeButton2 上。
    /// 拖拽到玩家/敌人立绘上释放，使用对应咖啡效果。每场战斗每杯只能用一次。
    /// 图标从 CoffeeIconCache（Assembly-CSharp）通过反射获取。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoffeeSlotController : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private int slotIndex = 0;
        [SerializeField] private BattleController battleController;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        [Header("提示框")]
        [SerializeField] private TutorialController tutorialController;

        private static readonly Color UsedColor = new(0.3f, 0.3f, 0.3f, 0.4f);

        private void Start()
        {
            if (battleController == null)
            {
                var bcGo = GameObject.Find("BattleController");
                if (bcGo != null)
                    battleController = bcGo.GetComponent<BattleController>();
            }

            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (tutorialController != null)
                tutorialController.UnregisterJsonCallouts(this);
        }

        private bool _uiRefreshed;

        private void Update()
        {
            if (!_uiRefreshed && battleController != null && battleController.IsInitialized)
            {
                RefreshUI();
                _uiRefreshed = true;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (battleController == null || !battleController.IsInitialized)
            {
                WarningToast.Show("Battle is not ready.");
                return;
            }

            var coffeeId = battleController.GetCoffeeSlot(slotIndex);
            if (string.IsNullOrEmpty(coffeeId))
            {
                WarningToast.Show("This coffee has already been used.");
                return;
            }

            string targetId = null;

            var playerPortrait = GameObject.Find("PlayerPortrait");
            if (playerPortrait != null)
            {
                var playerRT = playerPortrait.GetComponent<RectTransform>();
                if (playerRT != null && RectTransformUtility.RectangleContainsScreenPoint(
                        playerRT, eventData.position, eventData.pressEventCamera))
                {
                    targetId = battleController.State.Player.Id;
                }
            }

            if (targetId == null)
            {
                var enemyPortrait = GameObject.Find("EnemyPortrait");
                if (enemyPortrait != null)
                {
                    var enemyRT = enemyPortrait.GetComponent<RectTransform>();
                    if (enemyRT != null && RectTransformUtility.RectangleContainsScreenPoint(
                            enemyRT, eventData.position, eventData.pressEventCamera))
                    {
                        var enemy = battleController.State.FindFirstLivingEnemy();
                        if (enemy != null) targetId = enemy.Id;
                    }
                }
            }

            if (targetId == null)
            {
                WarningToast.Show("Drag coffee onto a character portrait.");
                return;
            }

            var result = battleController.UseCoffee(slotIndex, targetId);
            if (result.Success)
            {
                Debug.Log("[CoffeeSlot] Used coffee on " + targetId);
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("[CoffeeSlot] Use failed: " + result.Message);
                WarningToast.Show(CombatWarningText.FromResult(result));
            }
        }

        /// <summary>CoffeeIconCache 在 Assembly-CSharp，用反射调 GetCoffeeSprite。</summary>
        private static Sprite GetIconForCoffee(string coffeeId)
        {
            var cacheType = Type.GetType("CoffeeIconCache, Assembly-CSharp");
            if (cacheType == null) return null;
            var instanceProp = cacheType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProp?.GetValue(null);
            if (instance == null) return null;
            var method = cacheType.GetMethod("GetCoffeeSprite", BindingFlags.Public | BindingFlags.Instance);
            return method?.Invoke(instance, new object[] { coffeeId }) as Sprite;
        }

        private void RefreshUI()
        {
            if (battleController == null || !battleController.IsInitialized) return;

            var coffeeId = battleController.GetCoffeeSlot(slotIndex);
            bool hasCoffee = !string.IsNullOrEmpty(coffeeId);

            if (label != null)
            {
                if (hasCoffee)
                {
                    label.text = CoffeeEffectRegistry.GetChineseDisplayName(coffeeId);
                    label.color = Color.white;
                }
                else
                {
                    label.text = "已用";
                    label.color = UsedColor;
                }
            }

            if (icon != null)
            {
                if (hasCoffee)
                {
                    icon.sprite = GetIconForCoffee(coffeeId);
                    icon.color = Color.white;
                    icon.enabled = true;
                }
                else
                {
                    icon.enabled = false;
                }
            }

            RefreshTooltip(coffeeId);
        }

        /// <summary>为战斗中的咖啡槽注册中文效果说明；咖啡用完后立即移除提示。</summary>
        private void RefreshTooltip(string coffeeId)
        {
            if (tutorialController == null)
                tutorialController = FindFirstObjectByType<TutorialController>();
            if (tutorialController == null)
                return;

            tutorialController.UnregisterJsonCallouts(this);
            if (string.IsNullOrEmpty(coffeeId))
                return;

            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
                return;

            var background = GetComponent<Graphic>();
            if (background != null)
                background.raycastTarget = true;

            tutorialController.RegisterJsonCallout(this, rectTransform, new TutorialHintJson
            {
                description = CoffeeEffectRegistry.BuildChineseTooltip(coffeeId),
                offsetY = 48f
            });
        }
    }
}
