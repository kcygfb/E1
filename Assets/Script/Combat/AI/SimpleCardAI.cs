using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// 默认 AI 策略：抽牌 → 按伤害从高到低出牌 → 没牌时固定伤害 fallback。
    /// 所有参数可在 Inspector 里配置。
    /// </summary>
    [CreateAssetMenu(fileName = "SimpleCardAI", menuName = "KiKs/Combat/AI/Simple Card AI")]
    public class SimpleCardAI : EnemyAIStrategy
    {
        [Header("Timing")]
        [SerializeField] private float drawDelay = 1.0f;
        [SerializeField] private float playDelay = 1.5f;
        [SerializeField] private float fallbackDelay = 1.0f;

        [Header("Fallback Attack")]
        [SerializeField] private int fallbackDamage = 20;
        [SerializeField] private int fallbackToughnessDamage = 10;

        public override IEnumerator ExecuteTurn(
            string enemyId, BattleController controller, CombatEngine engine)
        {
            var state = controller.State;
            var enemy = state.FindEnemy(enemyId);
            if (enemy == null || enemy.IsDead) yield break;
            if (!engine.CanEnemyTakeCardTurn(enemyId))
                yield break;

            var turnRules = state.Rules.GetEnemyTurnRules(enemy.EnemyRank);

            var deck = state.GetEnemyDeck(enemyId);

            if (turnRules.BerserkTurn > 0 &&
                state.TurnNumber == turnRules.BerserkTurn &&
                state.GetEnemySpecialCard(enemyId) != null)
            {
                yield return new WaitForSeconds(drawDelay);
                var specialResult = engine.PlayEnemySpecialCard(enemyId);
                if (specialResult.Success)
                {
                    yield return new WaitForSeconds(playDelay);
                    engine.DiscardEnemyHand(enemyId);
                    yield break;
                }
            }

            // 没有牌库 → 直接 fallback 固定伤害
            if (deck == null)
            {
                yield return new WaitForSeconds(fallbackDelay);
                controller.ResolveEnemyAttack(enemyId, fallbackDamage, fallbackToughnessDamage);
                yield break;
            }

            // 1. 抽牌
            engine.DrawEnemyCards(enemyId, turnRules.CardsDrawnPerTurn, turnRules.HandLimit);
            yield return new WaitForSeconds(drawDelay);

            // 2. 按伤害降序出牌（AP 限制）
            int actionsRemaining = turnRules.CardsPlayedPerTurn;

            // 取手牌快照，按伤害排序
            var handSnapshot = deck.Hand.Where(card => !card.Spec.IsSpecial).ToList();
            var sorted = handSnapshot
                .OrderByDescending(c => GetCardDamage(c))
                .ThenBy(c => c.Spec.CostAmount)
                .ToList();

            foreach (var card in sorted)
            {
                if (actionsRemaining <= 0) break;
                if (enemy.IsDead) break;

                // 检查 AP 是否足够（AP 卡才检查）
                if (!engine.CanPlayEnemyCard(enemyId, card, out _))
                    continue;

                // 出牌
                var playResult = engine.PlayEnemyCard(enemyId, card.InstanceId);
                if (!playResult.Success) continue;
                actionsRemaining--;
                yield return new WaitForSeconds(playDelay);

                // 检查玩家是否已死
                if (state.Player.IsDead) break;
            }

            // 3. 没出牌 → fallback 固定伤害

            // 4. 弃手牌
            engine.DiscardEnemyHand(enemyId);
        }

        private static int GetCardDamage(CardInstance card)
        {
            foreach (var effect in card.Spec.Effects)
            {
                if (effect.Type == CardEffectType.Damage)
                    return effect.Amount.Resolve(card.IsUpgraded) * effect.Hits.Resolve(card.IsUpgraded);
            }
            return 0;
        }

        private int GetCardsPerTurn(BattleController controller, string enemyId)
        {
            var def = FindEnemyDefinition(controller, enemyId);
            return def != null ? def.CardsPerTurn : 2;
        }

        private int GetHandLimit(BattleController controller, string enemyId)
        {
            var def = FindEnemyDefinition(controller, enemyId);
            return def != null ? def.EnemyHandLimit : 5;
        }

        private int GetActionsPerTurn(BattleController controller, string enemyId)
        {
            var def = FindEnemyDefinition(controller, enemyId);
            return def != null ? def.EnemyActionsPerTurn : 1;
        }

        private static CombatantDefinition FindEnemyDefinition(BattleController controller, string enemyId)
        {
            if (controller == null) return null;
            return controller.FindEnemyDefinitionById(enemyId);
        }
    }
}
