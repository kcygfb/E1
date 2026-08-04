using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace KiKs.Combat
{
    /// <summary>
    /// 咖啡道具槽：挂在 Card 场景的 CoffeeButton1 / CoffeeButton2 上。
    /// 拖拽到玩家/敌人立绘上释放，使用对应咖啡效果。每场战斗每杯只能用一次。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoffeeSlotController : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private int slotIndex = 0;
        [SerializeField] private BattleController battleController;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        private static readonly Color UsedColor = new(0.3f, 0.3f, 0.3f, 0.4f);

        [Header("咖啡图标（手动指定）")]
        [SerializeField] private Sprite pourOverIcon;
        [SerializeField] private Sprite bloodGarmentIcon;

        private void Start()
        {
            if (battleController == null)
            {
                var bcGo = GameObject.Find("BattleController");
                if (bcGo != null)
                    battleController = bcGo.GetComponent<BattleController>();
            }

            RefreshUI();
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
            if (battleController == null || !battleController.IsInitialized) return;

            var coffeeId = battleController.GetCoffeeSlot(slotIndex);
            if (string.IsNullOrEmpty(coffeeId)) return;

            // 检测释放目标
            string targetId = null;

            // 尝试命中 PlayerPortrait
            var playerPortrait = GameObject.Find("PlayerPortrait");
            if (playerPortrait != null)
            {
                var playerRT = playerPortrait.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        playerRT, eventData.position, eventData.pressEventCamera))
                {
                    targetId = battleController.State.Player.Id;
                }
            }

            // 尝试命中 EnemyPortrait
            if (targetId == null)
            {
                var enemyPortrait = GameObject.Find("EnemyPortrait");
                if (enemyPortrait != null)
                {
                    var enemyRT = enemyPortrait.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(
                            enemyRT, eventData.position, eventData.pressEventCamera))
                    {
                        var enemy = battleController.State.FindFirstLivingEnemy();
                        if (enemy != null) targetId = enemy.Id;
                    }
                }
            }

            if (targetId == null) return;

            var result = battleController.UseCoffee(slotIndex, targetId);
            if (result.Success)
            {
                Debug.Log("[CoffeeSlot] Used coffee on " + targetId);
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("[CoffeeSlot] Use failed: " + result.Message);
            }
        }

        private Sprite GetIconForCoffee(string coffeeId)
        {
            return coffeeId switch
            {
                "PourOver" => pourOverIcon,
                "BloodGarment" => bloodGarmentIcon,
                _ => null,
            };
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
                    label.text = CoffeeEffectRegistry.GetDisplayName(coffeeId);
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
        }
    }
}
