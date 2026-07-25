using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KiKs.Combat;

namespace KiKs.UI
{
    /// <summary>
    /// 显示魔法点图标：Current 个图标亮起，其余熄灭。
    /// 挂在 PlayerArea 下，icons 数组按顺序对应每个魔法点。
    /// </summary>
    public class MagicPointIconUI : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;
        [SerializeField] private Image[] magicPointIcons;

        private IEnumerator Start()
        {
            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (battleController == null)
                yield break;

            battleController.CombatEventRaised += OnCombatEvent;
            while (!battleController.IsInitialized)
                yield return null;

            RefreshIcons();
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            RefreshIcons();
        }

        private void RefreshIcons()
        {
            if (battleController == null || !battleController.IsInitialized) return;

            int current = battleController.State.Mana.Current;
            for (int i = 0; i < magicPointIcons.Length; i++)
            {
                if (magicPointIcons[i] != null)
                    magicPointIcons[i].gameObject.SetActive(i < current);
            }
        }
    }
}
