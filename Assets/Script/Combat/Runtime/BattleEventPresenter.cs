using System.Collections;
using System.Collections.Generic;
using TMPro;
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

        [Header("Battle UI")]
        [SerializeField] private TMP_Text roundText;

        [Header("Enemy UI")]
        [SerializeField] private Image enemyHpFill;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyToughnessFill;
        [SerializeField] private TMP_Text enemyToughnessText;

        [Header("Player UI")]
        [SerializeField] private Image playerHpFill;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private Image playerToughnessFill;
        [SerializeField] private TMP_Text playerToughnessText;
        [SerializeField] private TMP_Text actionPointText;
        [SerializeField] private TMP_Text manaText;

        private int _enemyMaxHp;
        private int _enemyMaxToughness;
        private int _playerMaxHp;
        private int _playerMaxToughness;

        private IEnumerator Start()
        {
            ConfigureFillImage(enemyHpFill);
            ConfigureFillImage(enemyToughnessFill);
            ConfigureFillImage(playerHpFill);
            ConfigureFillImage(playerToughnessFill);

            if (battleController == null)
                battleController = FindFirstObjectByType<BattleController>();
            if (battleController == null)
                yield break;

            battleController.CombatEventRaised += OnCombatEvent;
            while (!battleController.IsInitialized)
                yield return null;

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (battleController != null)
                battleController.CombatEventRaised -= OnCombatEvent;
        }

        private void OnCombatEvent(CombatEvent evt)
        {
            RefreshUI();
        }

        private void RefreshUI()
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
            else
            {
                if (enemyHpFill != null) enemyHpFill.fillAmount = 0;
                if (enemyHpText != null) enemyHpText.text = "0 / " + _enemyMaxHp;
            }

            UpdatePlayerUI(state.Player);
            if (roundText != null)
                roundText.text = "ROUND " + state.TurnNumber;
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

        private static void ConfigureFillImage(Image image)
        {
            if (image == null) return;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
        }
    }
}
