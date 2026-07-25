using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// 战斗特效管理器：订阅 BattleController.CombatEventRaised，
    /// 根据事件类型在对应锚点播放粒子特效。挂在 VFXRoot 上。
    /// </summary>
    public class BattleVFXManager : MonoBehaviour
    {
        [Header("引擎引用")]
        [SerializeField] private BattleController battleController;

        [Header("锚点")]
        [Tooltip("玩家位置特效锚点")]
        [SerializeField] private Transform playerAnchor;
        [Tooltip("敌人位置特效锚点")]
        [SerializeField] private Transform enemyAnchor;

        [Header("特效预制体")]
        [Tooltip("命中受击特效（玩家攻击敌人 / 敌人攻击玩家共用）")]
        [SerializeField] private ParticleSystem hitImpactPrefab;
        [Tooltip("韧性击破特效")]
        [SerializeField] private ParticleSystem toughnessBreakPrefab;
        [Tooltip("状态施加特效（流血/中毒/眩晕等）")]
        [SerializeField] private ParticleSystem statusAppliedPrefab;
        [Tooltip("胜利特效")]
        [SerializeField] private ParticleSystem victoryPrefab;
        [Tooltip("失败特效")]
        [SerializeField] private ParticleSystem defeatPrefab;

        [Header("参数")]
        [SerializeField] private float vfxLifetime = 2f;

        private void Start()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (battleController != null)
                battleController.CombatEventRaised += OnCombatEvent;
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            if (battleController == null || !battleController.IsInitialized) return;

            switch (evt.Type)
            {
                case CombatEventType.DamageApplied:
                    HandleDamage(evt);
                    break;
                case CombatEventType.ToughnessBroken:
                    PlayAt(toughnessBreakPrefab, GetAnchorForTarget(evt.TargetId));
                    break;
                case CombatEventType.StatusApplied:
                    PlayAt(statusAppliedPrefab, GetAnchorForTarget(evt.TargetId));
                    break;
                case CombatEventType.Victory:
                    PlayAt(victoryPrefab, transform);
                    break;
                case CombatEventType.Defeat:
                    PlayAt(defeatPrefab, transform);
                    break;
            }
        }

        private void HandleDamage(CombatEvent evt)
        {
            if (string.IsNullOrEmpty(evt.SourceId)) return;

            if (evt.SourceId == battleController.State.Player.Id)
                PlayAt(hitImpactPrefab, enemyAnchor);
            else
                PlayAt(hitImpactPrefab, playerAnchor);
        }

        private Transform GetAnchorForTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return transform;
            if (battleController.State.Player.Id == targetId) return playerAnchor;
            if (battleController.State.FindEnemy(targetId) != null) return enemyAnchor;
            return transform;
        }

        private void PlayAt(ParticleSystem prefab, Transform anchor)
        {
            if (prefab == null) return;
            var pos = anchor != null ? anchor.position : transform.position;
            var vfx = Instantiate(prefab, pos, Quaternion.identity, transform);
            vfx.Play(true);
            Destroy(vfx.gameObject, vfxLifetime);
        }
    }
}
