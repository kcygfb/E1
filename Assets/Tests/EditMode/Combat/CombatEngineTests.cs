using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class CombatEngineTests
    {
        [Test]
        public void StartBattle_RestoresThreeActionPointsAndDrawsFourCards()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 1));
            var result = engine.StartBattle();

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
            Assert.That(engine.State.Mana.Current, Is.EqualTo(5));
            Assert.That(engine.State.Deck.Hand.Count, Is.EqualTo(4));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.PlayerInput));
        }

        [Test]
        public void PlayCard_SpendsActionPointAndMovesCardToDiscardPile()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 2));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var result = engine.PlayCard(card.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));
            Assert.That(engine.State.Deck.FindInHand(card.InstanceId), Is.Null);
            Assert.That(engine.State.Deck.DiscardPile, Does.Contain(card));
        }

        [Test]
        public void ToughnessBreak_RequiresConfirmation_AndMinionExecutionIsExtraDamageNotExtraTurn()
        {
            var engine = CreateEngine(CreateToughnessCard("break", 1, 5));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            Assert.That(engine.PlayCard(card.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.AwaitingExecutionConfirmation));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));

            Assert.That(engine.ConfirmExecution().Success, Is.True);
            Assert.That(engine.State.Enemies[0].IsDead, Is.True);
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.Victory));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));
        }

        [Test]
        public void NewPlayerTurn_DiscardsOldHandAndRestoresActionPoints()
        {
            var engine = CreateEngine(CreateDamageCard("costly", 2, 1), 8);
            engine.StartBattle();
            engine.PlayCard(engine.State.Deck.Hand[0].InstanceId, "enemy");
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(1));

            engine.EndPlayerTurn();
            Assert.That(engine.State.Deck.Hand.Count, Is.EqualTo(0));
            engine.CompleteEnemyTurn();

            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
            Assert.That(engine.State.Deck.Hand.Count, Is.EqualTo(4));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void UpgradeCard_SpendsManaInBattle_AndUsesUpgradedValue()
        {
            var engine = CreateEngine(CreateDamageCard("upgradeable", 1, 3, 9));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var upgrade = engine.UpgradeCard(card.InstanceId, "enemy");

            Assert.That(upgrade.Success, Is.True);
            Assert.That(card.IsUpgraded, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(4));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
            Assert.That(engine.PlayCard(card.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(91));
        }

        [Test]
        public void ManaSpendLimit_IsSharedByUpgradeAndMagicCard()
        {
            var basic = CreateDamageCard("basic", 1, 1, 2);
            var magic = CreateDamageCard("magic", 1, 1, null, CardResourceType.Mana);
            var engine = CreateEngine(new[] { basic, basic, magic, magic });
            engine.StartBattle();

            var basicCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "basic");
            var magicCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "magic");

            Assert.That(engine.UpgradeCard(basicCard.InstanceId).Success, Is.True);
            var magicResult = engine.PlayCard(magicCard.InstanceId, "enemy");

            Assert.That(magicResult.Success, Is.False);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(4));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
        }

        [Test]
        public void ThirdCumulativeManaSpend_TriggersUltimateAndRefundsThree()
        {
            var engine = CreateEngine(CreateDamageCard("basic", 0, 0, 1), 12);
            engine.StartBattle();

            for (var turn = 1; turn <= 3; turn++)
            {
                var card = engine.State.Deck.Hand.First(candidate => candidate.Spec.CanUpgrade);
                var result = engine.UpgradeCard(card.InstanceId, "enemy");
                Assert.That(result.Success, Is.True);

                if (turn < 3)
                {
                    engine.EndPlayerTurn();
                    engine.CompleteEnemyTurn();
                }
                else
                {
                    Assert.That(result.Events.Any(e => e.Type == CombatEventType.UltimateTriggered), Is.True);
                }
            }

            Assert.That(engine.State.Mana.Current, Is.EqualTo(5));
            Assert.That(engine.State.Mana.SpentTowardUltimate, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(1));
        }

        [Test]
        public void ActionPointModifier_ChangesNextTurnRestoredAmount()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 1));
            engine.State.Player.SetActionPointModifier(-1);
            engine.StartBattle();
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));
        }

        private static CombatEngine CreateEngine(CardSpec spec, int cardCount = 4)
        {
            var specs = new List<CardSpec>();
            for (var i = 0; i < cardCount; i++) specs.Add(spec);
            return CreateEngine(specs);
        }

        private static CombatEngine CreateEngine(IEnumerable<CardSpec> specs)
        {
            var cards = specs.Select((spec, index) =>
                new CardInstance(spec.Id + "#" + index, spec)).ToList();
            var player = new CombatantState("player", "Player", CombatantSide.Player, EnemyRank.None, 30, 0);
            var enemy = new CombatantState("enemy", "Enemy", CombatantSide.Enemy, EnemyRank.Minion, 100, 5);
            return new CombatEngine(new BattleState(
                CombatRules.CreateDefault(),
                player,
                new[] { enemy },
                new DeckState(cards, 123, false)));
        }

        private static CardSpec CreateDamageCard(
            string id,
            int cost,
            int baseDamage,
            int? upgradedDamage = null,
            CardResourceType resource = CardResourceType.ActionPoint)
        {
            return CreateCard(id, cost, resource, new CardEffectSpec(
                CardEffectType.Damage,
                new UpgradeableNumber(baseDamage, upgradedDamage),
                UpgradeableNumber.One,
                UpgradeableNumber.Zero,
                UpgradeableNumber.Zero,
                UpgradeableNumber.Zero,
                DamageType.Normal,
                ValueUnit.Points,
                0,
                false,
                1,
                string.Empty,
                0,
                0,
                CardResourceType.ActionPoint,
                string.Empty,
                string.Empty));
        }

        private static CardSpec CreateToughnessCard(string id, int cost, int amount)
        {
            return CreateCard(id, cost, CardResourceType.ActionPoint, new CardEffectSpec(
                CardEffectType.ToughnessDamage,
                new UpgradeableNumber(amount, null),
                UpgradeableNumber.One,
                UpgradeableNumber.Zero,
                UpgradeableNumber.Zero,
                UpgradeableNumber.Zero,
                DamageType.Normal,
                ValueUnit.Points,
                0,
                false,
                1,
                string.Empty,
                0,
                0,
                CardResourceType.ActionPoint,
                string.Empty,
                string.Empty));
        }

        private static CardSpec CreateCard(
            string id,
            int cost,
            CardResourceType resource,
            CardEffectSpec effect)
        {
            return new CardSpec(
                id, id, id, "test", resource, cost, false,
                effect.Type == CardEffectType.Damage || effect.Type == CardEffectType.ToughnessDamage
                    ? CardTargetType.SingleEnemy
                    : CardTargetType.Self,
                new[] { effect });
        }
    }
}
