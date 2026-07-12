using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 点击攻击卡：点击直接对目标敌人造成伤害。
    /// 复用卡牌上已有的 Button 组件。
    /// </summary>
    [RequireComponent(typeof(CardData))]
    [RequireComponent(typeof(Button))]
    public class ClickAttackCard : MonoBehaviour
    {
        [SerializeField] private EnemyStats target;

        private CardData _cardData;
        private Button _button;

        private void Awake()
        {
            _cardData = GetComponent<CardData>();
            _button = GetComponent<Button>();

            // 未在 Inspector 指定时，自动查找场景中的敌人
            if (target == null)
                target = FindFirstObjectByType<EnemyStats>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnCardClicked);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnCardClicked);
        }

        private void OnCardClicked()
        {
            if (target == null)
            {
                Debug.LogWarning($"[{name}] 未找到敌人目标 EnemyStats");
                return;
            }

            if (!target.IsDead)
            {
                target.TakeDamage(_cardData.Attack, _cardData.ToughnessReduction);
                Debug.Log($"[{name}] 对敌人造成 {_cardData.Attack} 点伤害，削韧 {_cardData.ToughnessReduction}");
            }
        }
    }
}
