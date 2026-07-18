using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// 监听 BattleController 的 CombatEvent，更新场景里的血条/韧性条/状态文字。
    /// </summary>
    public class BattleEventPresenter : MonoBehaviour
    {
        [SerializeField] private BattleController battleController;

        [Header("Enemy UI")]
        [SerializeField] private Image enemyHpFill;
        [SerializeField] private Text enemyHpText;
        [SerializeField] private Image enemyToughnessFill;
        [SerializeField] private Text enemyToughnessText;

        [Header("Player UI")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Image playerToughnessFill;
        [SerializeField] private Text playerToughnessText;
        [SerializeField] private Text actionPointText;
        [SerializeField] private Text manaText;

        private int _enemyMaxHp;
        private int _enemyMaxToughness;
        private int _playerMaxHp;
        private int _playerMaxToughness;

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

            var state = battleController.State;
            if (state == null) return;

            // 记录最大值
            _playerMaxHp = state.Player.MaxHealth;
            _playerMaxToughness = state.Player.MaxToughness;

            var enemy = state.FindFirstLivingEnemy();
            if (enemy != null)
            {
                _enemyMaxHp = enemy.MaxHealth;
                _enemyMaxToughness = enemy.MaxToughness;
                UpdateEnemyUI(enemy);
            }
            else if (evt.Type == CombatEventType.CombatantDied || evt.Type == CombatEventType.Victory)
            {
                if (enemyHpFill != null) enemyHpFill.fillAmount = 0;
                if (enemyHpText != null) enemyHpText.text = "0 / " + _enemyMaxHp;
            }

            UpdatePlayerUI(state.Player);
        }

        private void UpdateEnemyUI(CombatantState enemy)
        {
            if (enemyHpFill != null && _enemyMaxHp > 0)
                enemyHpFill.fillAmount = (float)enemy.CurrentHealth / _enemyMaxHp;
            if (enemyHpText != null)
                enemyHpText.text = enemy.CurrentHealth + " / " + enemy.MaxHealth;

            if (enemyToughnessFill != null && _enemyMaxToughness > 0)
                enemyToughnessFill.fillAmount = (float)enemy.CurrentToughness / _enemyMaxToughness;
            if (enemyToughnessText != null)
                enemyToughnessText.text = enemy.CurrentToughness + " / " + enemy.MaxToughness;
        }

        private void UpdatePlayerUI(CombatantState player)
        {
            if (playerHpFill != null && _playerMaxHp > 0)
                playerHpFill.fillAmount = (float)player.CurrentHealth / _playerMaxHp;
            if (playerHpText != null)
                playerHpText.text = player.CurrentHealth + " / " + player.MaxHealth;

            if (playerToughnessFill != null && _playerMaxToughness > 0)
                playerToughnessFill.fillAmount = (float)player.CurrentToughness / _playerMaxToughness;
            if (playerToughnessText != null)
                playerToughnessText.text = player.CurrentToughness + " / " + player.MaxToughness;

            if (actionPointText != null)
                actionPointText.text = "ACT POINT : " + player.CurrentActionPoints;

            if (manaText != null && battleController != null && battleController.IsInitialized)
                manaText.text = "MAGIC POINT : " + battleController.State.Mana.Current;
        }
    }
}
