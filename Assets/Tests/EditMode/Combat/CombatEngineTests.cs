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
        public void EnemyAttack_DamagesHealthAndReducesPlayerToughness()
        {
            var spec = CreateDamageCard("attack", 1, 1);
            var cards = Enumerable.Range(0, 4)
                .Select(index => new CardInstance(spec.Id + "#" + index, spec))
                .ToList();
            var player = new CombatantState(
                "player", "Player", CombatantSide.Player, EnemyRank.None, 30, 100);
            var enemy = new CombatantState(
                "enemy", "Enemy", CombatantSide.Enemy, EnemyRank.Minion, 100, 5);
            var engine = new CombatEngine(new BattleState(
                CombatRules.CreateDefault(),
                player,
                new[] { enemy },
                new DeckState(cards, 123, false)));

            Assert.That(engine.StartBattle().Success, Is.True);
            Assert.That(engine.EndPlayerTurn().Success, Is.True);

            var result = engine.ResolveEnemyAttack("enemy", 20, 10);

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(10));
            Assert.That(engine.State.Player.CurrentToughness, Is.EqualTo(90));
            Assert.That(result.Events.Any(combatEvent =>
                combatEvent.Type == CombatEventType.ToughnessChanged &&
                combatEvent.Amount == 90), Is.True);
        }

        [Test]
        public void BleedScaledDamage_DealsThreeDamagePerBleedStack()
        {
            var bleed = CreateCard("bleed", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 5));
            var blizzard = CreateCard("blizzard", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.BleedScaledDamage, multiplier: 3));
            var engine = CreateEngine(new[] { bleed, blizzard, bleed, blizzard });
            engine.StartBattle();

            var bleedCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "bleed");
            Assert.That(engine.PlayCard(bleedCard.InstanceId, "enemy").Success, Is.True);
            var blizzardCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "blizzard");
            Assert.That(engine.PlayCard(blizzardCard.InstanceId, "enemy").Success, Is.True);

            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(5));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void Bleed_TicksEachTurnDecreasingDamageUntilZero()
        {
            var bleed = CreateCard("bleed", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 5));
            var engine = CreateEngine(bleed, 8);
            engine.StartBattle();

            // Apply 5 bleed stacks on turn 1, no tick yet on same turn
            var bleedCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "bleed");
            Assert.That(engine.PlayCard(bleedCard.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(5));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(100));

            // Turn 2: bleed ticks for 5 damage, stacks become 4
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(4));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(95));

            // Turn 3: bleed ticks for 4 damage, stacks become 3
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(3));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(91));

            // Turn 4: bleed ticks for 3 damage, stacks become 2
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(2));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(88));

            // Turn 5: bleed ticks for 2 damage, stacks become 1
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(1));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(86));

            // Turn 6: bleed ticks for 1 damage, stacks become 0
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(85));

            // Turn 7: no more bleed, stacks stay 0
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void Bleed_StackingMidBleedResetsWithCurrentRemaining()
        {
            var bleed = CreateCard("bleed", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 5));
            var engine = CreateEngine(bleed, 8);
            engine.StartBattle();

            // Apply 5 bleed on turn 1
            var card1 = engine.State.Deck.Hand.First(card => card.Spec.Id == "bleed");
            engine.PlayCard(card1.InstanceId, "enemy");

            // Turn 2: bleed ticks 5, stacks = 4; then apply 3 more → stacks = 7
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(4));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(95));

            // Apply 3 more bleed stacks mid-bleed (simulating playing another bleed card)
            engine.State.Enemies[0].AddBleedStacks(3);
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(7));

            // Turn 3: bleed ticks 7, stacks = 6
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(6));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(88));

            // Turn 4: bleed ticks 6, stacks = 5
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(5));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(82));
        }

        [Test]
        public void Bleed_KillsEnemyWhenDamageExceedsRemainingHealth()
        {
            var bleed = CreateCard("bleed", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 20));
            var engine = CreateEngine(bleed, 8);
            engine.StartBattle();

            var card = engine.State.Deck.Hand.First(c => c.Spec.Id == "bleed");
            engine.PlayCard(card.InstanceId, "enemy");
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(20));

            // Enemy has 100 HP, bleed deals 20 → 80, 19 → 61, 18 → 43, 17 → 26, 16 → 10, 15 → dead
            // Turn 2: tick 20
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(80));

            // Turn 3: tick 19
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(61));

            // Turn 4: tick 18
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(43));

            // Turn 5: tick 17
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(26));

            // Turn 6: tick 16
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(10));

            // Turn 7: tick 15 → kill
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].IsDead, Is.True);
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(14));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.Victory));
        }

        [Test]
        public void BlockAndReflect_ModifyTheNextEnemyAttack()
        {
            var block = CreateCard("block", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.BlockDamage, amount: 12));
            var reflect = CreateCard("reflect", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.ReflectDamage, amount: 6));
            var engine = CreateEngine(new[] { block, reflect, block, reflect });
            engine.StartBattle();

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "block").InstanceId,
                null).Success, Is.True);
            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "reflect").InstanceId,
                null).Success, Is.True);
            Assert.That(engine.EndPlayerTurn().Success, Is.True);

            Assert.That(engine.ResolveEnemyAttack("enemy", 20).Success, Is.True);

            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(22));
            Assert.That(engine.State.Player.BlockPoints, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(94));
            Assert.That(engine.State.Player.PendingReflectDamage, Is.EqualTo(0));
        }

        [Test]
        public void UpgradeCard_SpendsManaAndAppliesToTheNextPlayOnly()
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
            Assert.That(card.IsUpgraded, Is.False);
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
        public void ThirdCumulativeManaSpend_TriggersUltimateWithoutRefundingMana()
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

            Assert.That(engine.State.Mana.Current, Is.EqualTo(2));
            Assert.That(engine.State.Mana.SpentTowardUltimate, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(1));
        }

        [Test]
        public void ToughnessBreak_RestoresManaToMaximum()
        {
            var upgradeable = CreateDamageCard("upgradeable", 0, 0, 1);
            var breaker = CreateToughnessCard("breaker", 0, 5);
            var engine = CreateEngine(new[] { upgradeable, breaker, upgradeable, breaker });
            engine.StartBattle();

            var cardToUpgrade = engine.State.Deck.Hand.First(card => card.Spec.Id == "upgradeable");
            Assert.That(engine.UpgradeCard(cardToUpgrade.InstanceId).Success, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(4));

            var breakCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "breaker");
            var result = engine.PlayCard(breakCard.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(5));
            Assert.That(result.Events.Any(combatEvent =>
                combatEvent.Type == CombatEventType.ManaChanged && combatEvent.Amount == 5), Is.True);
        }

        [Test]
        public void ExecutionOnElite_DealsFortyDamageAndStunsOneTurn()
        {
            var engine = CreateEngine(CreateToughnessCard("break", 1, 5), 4, EnemyRank.Elite);
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            Assert.That(engine.PlayCard(card.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.AwaitingExecutionConfirmation));

            Assert.That(engine.ConfirmExecution().Success, Is.True);
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(60)); // 100 - 40
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(1));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.PlayerInput));

            // Next turn: enemy should be stunned (cannot attack)
            engine.EndPlayerTurn();
            var attackResult = engine.ResolveEnemyAttack("enemy", 20);
            Assert.That(attackResult.Success, Is.True);
            Assert.That(attackResult.Events.Any(e => e.Type == CombatEventType.EnemyActionSkipped), Is.True);
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(0));
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(30)); // No damage taken
        }

        [Test]
        public void ExecutionOnBoss_DealsFortyDamageAndStunsOneTurn()
        {
            var engine = CreateEngine(CreateToughnessCard("break", 1, 5), 4, EnemyRank.Boss);
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            Assert.That(engine.PlayCard(card.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.AwaitingExecutionConfirmation));

            Assert.That(engine.ConfirmExecution().Success, Is.True);
            Assert.That(engine.State.Enemies[0].IsDead, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(60)); // 100 - 40
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(1));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.PlayerInput));

            // Next turn: boss should be stunned (cannot attack)
            engine.EndPlayerTurn();
            var attackResult = engine.ResolveEnemyAttack("enemy", 20);
            Assert.That(attackResult.Success, Is.True);
            Assert.That(attackResult.Events.Any(e => e.Type == CombatEventType.EnemyActionSkipped), Is.True);
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(0));
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(30)); // No damage taken
        }

        [Test]
        public void ActionPointModifier_ChangesNextTurnRestoredAmount()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 1));
            engine.State.Player.SetActionPointModifier(-1);
            engine.StartBattle();
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));
        }

        private static CombatEngine CreateEngine(CardSpec spec, int cardCount = 4, EnemyRank enemyRank = EnemyRank.Minion)
        {
            var specs = new List<CardSpec>();
            for (var i = 0; i < cardCount; i++) specs.Add(spec);
            return CreateEngine(specs, enemyRank);
        }

        private static CombatEngine CreateEngine(IEnumerable<CardSpec> specs, EnemyRank enemyRank = EnemyRank.Minion)
        {
            var cards = specs.Select((spec, index) =>
                new CardInstance(spec.Id + "#" + index, spec)).ToList();
            var player = new CombatantState("player", "Player", CombatantSide.Player, EnemyRank.None, 30, 0);
            var enemy = new CombatantState("enemy", "Enemy", CombatantSide.Enemy, enemyRank, 100, 5);
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
                ValueUnit.Points,
                1));
        }

        private static CardSpec CreateToughnessCard(string id, int cost, int amount)
        {
            return CreateCard(id, cost, CardResourceType.ActionPoint, new CardEffectSpec(
                CardEffectType.ToughnessDamage,
                new UpgradeableNumber(amount, null),
                UpgradeableNumber.One,
                ValueUnit.Points,
                1));
        }

        private static CardSpec CreateCard(
            string id,
            int cost,
            CardResourceType resource,
            CardEffectSpec effect)
        {
            return new CardSpec(
                id, id, id, "test", resource, cost, false,
                effect.Type == CardEffectType.Damage ||
                effect.Type == CardEffectType.ToughnessDamage ||
                effect.Type == CardEffectType.Bleed ||
                effect.Type == CardEffectType.BleedScaledDamage ||
                effect.Type == CardEffectType.LifeSteal
                    ? CardTargetType.SingleEnemy
                    : CardTargetType.Self,
                new[] { effect });
        }

        private static CardEffectSpec CreateEffect(
            CardEffectType type,
            int amount = 0,
            double multiplier = 1d)
        {
            return new CardEffectSpec(
                type,
                new UpgradeableNumber(amount, null),
                UpgradeableNumber.One,
                ValueUnit.Points,
                multiplier);
        }
    }
}
