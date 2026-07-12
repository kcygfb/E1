// ──────────────────────────────────────────────────────────────
//  EnemyStats.cs
//  功能：敌人血量与韧性管理
//
//  使用说明：
//    1. 挂载到敌人 GameObject 上
//    2. 在 Inspector 设置 maxHP / maxToughness
//    3. 卡牌脚本通过 TakeDamage(攻击力, 削韧值) 造成伤害
//    4. 监听事件以更新 UI：
//       - OnHPChanged(int 当前血量, int 最大血量)
//       - OnToughnessChanged(int 当前韧性, int 最大韧性)
//       - OnDeath()  血量归零时触发
//
//  示例：
//    EnemyStats enemy = target.GetComponent<EnemyStats>();
//    enemy.TakeDamage(10, 5);  // 扣 10 血、5 韧性
// ──────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.Events;

namespace KiKs.Combat
{
    /// <summary>
    /// 敌人血量与韧性管理。
    /// 提供 TakeDamage 接口供卡牌调用，通过 UnityEvent 通知外部 UI 刷新。
    /// </summary>
    public class EnemyStats : MonoBehaviour
    {
        [Header("血量")]
        [SerializeField] private int maxHP = 100;          // 最大血量
        [SerializeField] private int currentHP;            // 当前血量，运行时由 maxHP 初始化

        [Header("韧性")]
        [SerializeField] private int maxToughness = 100;   // 最大韧性值
        [SerializeField] private int currentToughness;    // 当前韧性值，运行时由 maxToughness 初始化

        [Header("事件")]
        public UnityEvent<int, int> OnHPChanged;           // 血量变化时触发 (当前血量, 最大血量)
        public UnityEvent<int, int> OnToughnessChanged;    // 韧性变化时触发 (当前韧性, 最大韧性)
        public UnityEvent OnDeath;                         // 血量归零时触发

        // ── 只读属性，供外部查询 ──────────────────────────
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public int CurrentToughness => currentToughness;
        public int MaxToughness => maxToughness;
        public bool IsDead => currentHP <= 0;

        private void Awake()
        {
            // 初始化：满血满韧性
            currentHP = maxHP;
            currentToughness = maxToughness;
        }

        private void Start()
        {
            // 在所有 Awake 完成后触发初始事件，确保 UI 监听器已就绪
            OnHPChanged?.Invoke(currentHP, maxHP);
            OnToughnessChanged?.Invoke(currentToughness, maxToughness);
        }

        /// <summary>
        /// 受到伤害：同时扣减血量和韧性。
        /// </summary>
        /// <param name="attack">攻击力，扣减血量</param>
        /// <param name="toughnessReduction">削韧值，扣减韧性</param>
        public void TakeDamage(int attack, int toughnessReduction)
        {
            if (IsDead) return;

            // 扣减血量与韧性，下限为 0
            currentHP = Mathf.Max(0, currentHP - attack);
            currentToughness = Mathf.Max(0, currentToughness - toughnessReduction);

            // 通知外部（血条 UI 等）刷新显示
            OnHPChanged?.Invoke(currentHP, maxHP);
            OnToughnessChanged?.Invoke(currentToughness, maxToughness);

            // 血量归零，触发死亡事件
            if (currentHP <= 0)
                OnDeath?.Invoke();
        }
    }
}
