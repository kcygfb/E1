using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class CombatEngineTests
    {
        [Test]
        public void StartBattle_RestoresThreeActionPoints_DrawsFourCards_AndGrantsOneMana()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 1));
            var result = engine.StartBattle();

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
            Assert.That(engine.State.Mana.Current, Is.EqualTo(1));
            Assert.That(engine.State.Mana.PerTurn, Is.EqualTo(1));
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
        public void PlaySingleShot_KillingBeforeFinalShot_ImmediatelyTriggersVictory()
        {
            var engine = CreateEngine(CreateGunCard("gun", 1, 100, 3));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var result = engine.PlaySingleShot(card.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Enemies[0].IsDead, Is.True);
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.Victory));
            Assert.That(engine.IsShooting(card.InstanceId), Is.False);
            Assert.That(engine.State.Deck.DiscardPile, Does.Contain(card));
            Assert.That(result.Events.Any(e => e.Type == CombatEventType.Victory), Is.True);
        }

        [Test]
        public void ToughnessBreak_AutomaticallyDealsSixtyExecutionDamage_AndKeepsPlayerTurn()
        {
            var engine = CreateEngine(CreateToughnessCard("break", 1, 5));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var result = engine.PlayCard(card.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(40));
            Assert.That(engine.State.Enemies[0].CurrentToughness, Is.EqualTo(5));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.PlayerInput));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(2));
            Assert.That(result.Events.Any(e =>
                e.Type == CombatEventType.ExecutionResolved && e.Amount == 60), Is.True);
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

            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(85));
        }

        [Test]
        public void Poison_CanBeAppliedAndTicksPerTurn()
        {
            var poison = CreateCard("poison", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Poison, amount: 4));
            var engine = CreateEngine(poison, 8);
            engine.StartBattle();

            // Apply 4 poison stacks using PlayCard
            var poisonCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "poison");
            Assert.That(engine.PlayCard(poisonCard.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(4));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(100));

            // Turn 2: poison ticks 4 damage, stacks become 3
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(3));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(96));

            // Turn 3: poison ticks 3 damage, stacks become 2
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(2));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(93));

            // Turn 4: poison ticks 2 damage, stacks become 1
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(1));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(91));

            // Turn 5: poison ticks 1 damage, stacks become 0
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(90));

            // Turn 6: no more poison
            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(90));
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
        public void PlayerBleed_TicksAtPlayerTurnStart_AndCanCauseDefeat()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 1, 1), 8);
            engine.StartBattle();
            engine.EndPlayerTurn();
            engine.State.Player.AddBleedStacks(30);

            var result = engine.CompleteEnemyTurn();

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Player.IsDead, Is.True);
            Assert.That(engine.State.Player.BleedStacks, Is.EqualTo(29));
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.Defeat));
            Assert.That(result.Events.Any(e =>
                e.Type == CombatEventType.StatusTicked &&
                e.TargetId == "player" &&
                e.Amount == 30), Is.True);
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
        public void BlockScaledDamage_UsesCurrentBlockWithoutConsumingIt()
        {
            var block = CreateCard("block", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.BlockDamage, amount: 12));
            var shieldBash = CreateCard("shield_bash", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.BlockScaledDamage));
            var engine = CreateEngine(new[] { block, shieldBash, block, shieldBash });
            engine.StartBattle();

            var blockCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "block");
            Assert.That(engine.PlayCard(blockCard.InstanceId, null).Success, Is.True);
            Assert.That(engine.State.Player.BlockPoints, Is.EqualTo(12));

            var bashCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "shield_bash");
            Assert.That(engine.PlayCard(bashCard.InstanceId, "enemy").Success, Is.True);

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(88));
            Assert.That(engine.State.Player.BlockPoints, Is.EqualTo(12));
        }

        [Test]
        public void PoisonEnchant_AddsStacksFromCurrentTargetPoisonOnNextAttack()
        {
            var poison = CreateCard("poison", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Poison, amount: 2));
            var enchant = CreateCard("enchant", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.PoisonScaledNextAttack, multiplier: 5));
            var attack = CreateDamageCard("attack", 0, 1);
            var engine = CreateEngine(new[] { poison, enchant, attack, attack });
            engine.StartBattle();

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "poison").InstanceId,
                "enemy").Success, Is.True);
            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "enchant").InstanceId,
                null).Success, Is.True);
            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "attack").InstanceId,
                "enemy").Success, Is.True);

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(99));
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(12));
            Assert.That(engine.State.Player.NextAttackPoisonMultiplier, Is.EqualTo(0d));
        }

        [Test]
        public void PoisonDamageBonus_IncreasesPoisonTicksUntilPoisonExpires()
        {
            var poison = CreateCard("poison", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Poison, amount: 2));
            var dragon = CreateCard("dragon", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.PoisonDamageBonus, amount: 5));
            var engine = CreateEngine(new[] { poison, dragon, poison, dragon });
            engine.StartBattle();

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "poison").InstanceId,
                "enemy").Success, Is.True);
            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "dragon").InstanceId,
                "enemy").Success, Is.True);

            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(93));
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(1));
            Assert.That(engine.State.Enemies[0].PoisonDamageBonus, Is.EqualTo(5));

            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(87));
            Assert.That(engine.State.Enemies[0].PoisonStacks, Is.EqualTo(0));
            Assert.That(engine.State.Enemies[0].PoisonDamageBonus, Is.EqualTo(0));
        }

        [Test]
        public void SummonCompanion_GrantsBonusManaForThreePlayerTurns()
        {
            var summon = CreateCard("summon", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.SummonCompanion, amount: 3));
            var engine = CreateEngine(summon, 8);
            engine.StartBattle();

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "summon").InstanceId,
                null).Success, Is.True);
            Assert.That(engine.State.Player.CompanionTurns, Is.EqualTo(3));

            for (var turn = 0; turn < 3; turn++)
            {
                engine.EndPlayerTurn();
                engine.CompleteEnemyTurn();
                Assert.That(engine.State.Mana.Current, Is.EqualTo(2));
                Assert.That(engine.State.Mana.BonusPerTurn, Is.EqualTo(1));
            }

            engine.EndPlayerTurn();
            engine.CompleteEnemyTurn();
            Assert.That(engine.State.Mana.Current, Is.EqualTo(1));
            Assert.That(engine.State.Player.CompanionTurns, Is.EqualTo(0));
        }

        [Test]
        public void VulnerabilityAndImmunity_ModifySharedDamageFlow()
        {
            var vulnerable = CreateCard("vulnerable", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Vulnerability, amount: 50));
            var attack = CreateDamageCard("attack", 0, 10);
            var immune = CreateCard("immune", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Immunity, amount: 1));
            var engine = CreateEngine(new[] { vulnerable, attack, immune, attack });
            engine.StartBattle();

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "vulnerable").InstanceId,
                "enemy").Success, Is.True);
            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "attack").InstanceId,
                "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(85));

            Assert.That(engine.PlayCard(
                engine.State.Deck.Hand.First(card => card.Spec.Id == "immune").InstanceId,
                null).Success, Is.True);
            engine.EndPlayerTurn();
            Assert.That(engine.ResolveEnemyAttack("enemy", 20).Success, Is.True);
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(30));
        }

        [Test]
        public void PlayCardsFromDiscard_ReplaysTopDiscardCardForFree()
        {
            var attack = CreateDamageCard("attack", 0, 5);
            var replay = CreateCard("replay", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.PlayCardsFromDiscard, amount: 1));
            var engine = CreateEngine(new[] { attack, replay, attack, replay });
            engine.StartBattle();

            var attackCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "attack");
            Assert.That(engine.PlayCard(attackCard.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(95));

            var replayCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "replay");
            var result = engine.PlayCard(replayCard.InstanceId, null);

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(90));
            Assert.That(result.Events.Count(e => e.Type == CombatEventType.CardPlayed), Is.EqualTo(2));
        }

        [Test]
        public void LifeSteal_DoublesDamageAgainstBleedingTargets()
        {
            var lifesteal = CreateCard("lifesteal", 0, CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.LifeSteal, amount: 9));
            var engine = CreateEngine(lifesteal, 4);
            engine.StartBattle();
            engine.State.Enemies[0].AddBleedStacks(1);

            Assert.That(engine.PlayCard(engine.State.Deck.Hand.First().InstanceId, "enemy").Success, Is.True);

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(82));
        }

        [Test]
        public void UpgradeCard_SpendsManaAndAppliesToTheNextPlayOnly()
        {
            var engine = CreateEngine(CreateDamageCard("upgradeable", 1, 3, 9));
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var upgrade = engine.UpgradeCard(card.InstanceId);

            Assert.That(upgrade.Success, Is.True);
            Assert.That(card.IsUpgraded, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(0));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
            Assert.That(engine.PlayCard(card.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(91));
            Assert.That(card.IsUpgraded, Is.False);
        }
        [Test]
        public void ManaPerTurn_IsSharedByUpgradeAndMagicCard()
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
            Assert.That(engine.State.Mana.Current, Is.EqualTo(0));
            Assert.That(engine.State.Player.CurrentActionPoints, Is.EqualTo(3));
        }
        [Test]
        public void ThreeManaSpends_DoNotTriggerAutomaticUltimate()
        {
            var engine = CreateEngine(CreateDamageCard("basic", 0, 0, 1), 12);
            engine.StartBattle();

            for (var turn = 1; turn <= 3; turn++)
            {
                var card = engine.State.Deck.Hand.First(candidate => candidate.Spec.CanUpgrade);
                var upgrade = engine.UpgradeCard(card.InstanceId);
                Assert.That(upgrade.Success, Is.True);
                Assert.That(engine.State.Mana.Current, Is.EqualTo(0));

                if (turn < 3)
                {
                    engine.EndPlayerTurn();
                    engine.CompleteEnemyTurn();
                    Assert.That(engine.State.Mana.Current, Is.EqualTo(engine.State.Mana.PerTurn));
                }
            }

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(100));
        }
        [Test]
        public void ToughnessBreak_DoesNotRestoreMana()
        {
            var upgradeable = CreateDamageCard("upgradeable", 0, 0, 1);
            var breaker = CreateToughnessCard("breaker", 0, 5);
            var engine = CreateEngine(new[] { upgradeable, breaker, upgradeable, breaker });
            engine.StartBattle();

            var cardToUpgrade = engine.State.Deck.Hand.First(card => card.Spec.Id == "upgradeable");
            Assert.That(engine.UpgradeCard(cardToUpgrade.InstanceId).Success, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(0));

            var breakCard = engine.State.Deck.Hand.First(card => card.Spec.Id == "breaker");
            var result = engine.PlayCard(breakCard.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Mana.Current, Is.EqualTo(0));
            Assert.That(result.Events.Any(combatEvent =>
                combatEvent.Type == CombatEventType.ManaChanged), Is.False);
        }
        [TestCase(EnemyRank.Elite)]
        [TestCase(EnemyRank.Boss)]
        public void Execution_DealsSixtyDamageAndStunsOneTurn_ForEveryEnemyRank(EnemyRank enemyRank)
        {
            var engine = CreateEngine(CreateToughnessCard("break", 1, 5), 4, enemyRank);
            engine.StartBattle();
            var card = engine.State.Deck.Hand[0];

            var result = engine.PlayCard(card.InstanceId, "enemy");

            Assert.That(result.Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(40));
            Assert.That(engine.State.Enemies[0].CurrentToughness, Is.EqualTo(5));
            Assert.That(engine.State.Enemies[0].StunTurns, Is.EqualTo(1));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.PlayerInput));
        }

        [Test]
        public void PlayerAndEnemyCards_ShareDamageMitigationFlow()
        {
            var playerAttack = CreateDamageCard("player_attack", 0, 10);
            var enemyAttack = CreateDamageCard("enemy_attack", 0, 10);
            var engine = CreateEngine(playerAttack, 8);
            RegisterEnemyDeck(engine, enemyAttack);

            engine.State.Enemies[0].AddBlockPoints(4);
            Assert.That(engine.StartBattle().Success, Is.True);

            var playerCard = engine.State.Deck.Hand.First();
            var playerResult = engine.PlayCard(playerCard.InstanceId, "enemy");

            Assert.That(playerResult.Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(94));
            Assert.That(engine.State.Enemies[0].BlockPoints, Is.EqualTo(0));

            engine.State.Player.AddBlockPoints(4);
            Assert.That(engine.EndPlayerTurn().Success, Is.True);
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();
            var enemyResult = engine.PlayEnemyCard("enemy", enemyCard.InstanceId);

            Assert.That(enemyResult.Success, Is.True);
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(24));
            Assert.That(engine.State.Player.BlockPoints, Is.EqualTo(0));
            Assert.That(playerResult.Events.Any(e =>
                e.Type == CombatEventType.DamageApplied &&
                e.SourceId == "player" &&
                e.Amount == 6), Is.True);
            Assert.That(enemyResult.Events.Any(e =>
                e.Type == CombatEventType.DamageApplied &&
                e.SourceId == "enemy" &&
                e.Amount == 6), Is.True);
        }

        [Test]
        public void PlayerAndEnemyCards_ApplyBleedThroughSameFlow()
        {
            var playerBleed = CreateCard(
                "player_bleed",
                0,
                CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 2));
            var enemyBleed = CreateCard(
                "enemy_bleed",
                0,
                CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.Bleed, amount: 3));
            var engine = CreateEngine(playerBleed, 8);
            RegisterEnemyDeck(engine, enemyBleed);

            engine.StartBattle();
            var playerCard = engine.State.Deck.Hand.First();
            Assert.That(engine.PlayCard(playerCard.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(2));

            engine.EndPlayerTurn();
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();
            Assert.That(engine.PlayEnemyCard("enemy", enemyCard.InstanceId).Success, Is.True);
            Assert.That(engine.State.Player.BleedStacks, Is.EqualTo(3));

            var nextTurn = engine.CompleteEnemyTurn();

            Assert.That(nextTurn.Success, Is.True);
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(27));
            Assert.That(engine.State.Player.BleedStacks, Is.EqualTo(2));
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(98));
            Assert.That(engine.State.Enemies[0].BleedStacks, Is.EqualTo(1));
        }

        [Test]
        public void EnemySelfEffect_UsesEnemyAsTheDestination()
        {
            var playerAttack = CreateDamageCard("player_attack", 0, 10);
            var enemyReduction = CreateCard(
                "enemy_reduction",
                0,
                CardResourceType.ActionPoint,
                CreateEffect(CardEffectType.DamageReduction, amount: 50));
            var engine = CreateEngine(playerAttack, 8);
            RegisterEnemyDeck(engine, enemyReduction);

            engine.StartBattle();
            engine.EndPlayerTurn();
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();
            Assert.That(engine.PlayEnemyCard("enemy", enemyCard.InstanceId).Success, Is.True);
            Assert.That(engine.State.Enemies[0].DamageReductionPercent, Is.EqualTo(50));

            engine.CompleteEnemyTurn();
            var playerCard = engine.State.Deck.Hand.First();
            Assert.That(engine.PlayCard(playerCard.InstanceId, "enemy").Success, Is.True);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(95));
        }

        [Test]
        public void NullifyAndReflect_WorkForActionsFromEitherSide()
        {
            var playerAttack = CreateDamageCard("player_attack", 0, 10);
            var enemyAttack = CreateDamageCard("enemy_attack", 0, 10);
            var engine = CreateEngine(playerAttack, 8);
            RegisterEnemyDeck(engine, enemyAttack);

            engine.State.Enemies[0].AddNullifyAttackCharges(1);
            engine.StartBattle();
            var playerCard = engine.State.Deck.Hand.First();
            var nullifiedPlayerCard = engine.PlayCard(playerCard.InstanceId, "enemy");

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(100));
            Assert.That(nullifiedPlayerCard.Events.Any(e =>
                e.Type == CombatEventType.ActionNullified &&
                e.SourceId == "player"), Is.True);

            engine.State.Player.AddNullifyAttackCharges(1);
            engine.EndPlayerTurn();
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();
            var nullifiedEnemyCard = engine.PlayEnemyCard("enemy", enemyCard.InstanceId);

            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(30));
            Assert.That(nullifiedEnemyCard.Events.Any(e =>
                e.Type == CombatEventType.ActionNullified &&
                e.SourceId == "enemy"), Is.True);

            engine.CompleteEnemyTurn();
            engine.State.Enemies[0].AddReflectDamage(5);
            playerCard = engine.State.Deck.Hand.First();
            engine.PlayCard(playerCard.InstanceId, "enemy");

            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(90));
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(25));
        }

        [Test]
        public void EnemyStun_CannotBeBypassedByCallingPlayEnemyCardDirectly()
        {
            var enemyAttack = CreateDamageCard("enemy_attack", 0, 10);
            var engine = CreateEngine(CreateDamageCard("player_attack", 0, 1), 8);
            RegisterEnemyDeck(engine, enemyAttack);
            engine.State.Enemies[0].AddStun(1);

            engine.StartBattle();
            engine.EndPlayerTurn();
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();

            var firstAttempt = engine.PlayEnemyCard("enemy", enemyCard.InstanceId);
            var secondAttempt = engine.PlayEnemyCard("enemy", enemyCard.InstanceId);

            Assert.That(firstAttempt.Success, Is.False);
            Assert.That(secondAttempt.Success, Is.False);
            Assert.That(engine.State.Player.CurrentHealth, Is.EqualTo(30));
            Assert.That(engine.State.GetDeck("enemy").FindInHand(enemyCard.InstanceId), Is.Not.Null);
            Assert.That(firstAttempt.Events.Any(e =>
                e.Type == CombatEventType.CombatantTurnSkipped &&
                e.SourceId == "enemy"), Is.True);
        }

        [Test]
        public void EnemyStunCard_SkipsThePlayersNextTurnThroughTheSameGate()
        {
            var enemyStun = new CardSpec(
                "enemy_stun",
                "enemy_stun",
                "enemy_stun",
                "enemy_test",
                CardResourceType.ActionPoint,
                0,
                false,
                CardTargetType.SingleEnemy,
                new[] { CreateEffect(CardEffectType.Stun, amount: 1) });
            var engine = CreateEngine(CreateDamageCard("player_attack", 0, 1), 8);
            RegisterEnemyDeck(engine, enemyStun);

            engine.StartBattle();
            engine.EndPlayerTurn();
            engine.DrawEnemyCards("enemy", 1, 10);
            var enemyCard = engine.State.GetDeck("enemy").Hand.Single();
            Assert.That(engine.PlayEnemyCard("enemy", enemyCard.InstanceId).Success, Is.True);
            Assert.That(engine.State.Player.StunTurns, Is.EqualTo(1));

            var nextTurn = engine.CompleteEnemyTurn();

            Assert.That(nextTurn.Success, Is.True);
            Assert.That(engine.State.Player.StunTurns, Is.EqualTo(0));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
            Assert.That(engine.State.Phase, Is.EqualTo(CombatPhase.EnemyTurn));
            Assert.That(engine.State.Deck.Hand.Count, Is.EqualTo(0));
            Assert.That(nextTurn.Events.Any(e =>
                e.Type == CombatEventType.CombatantTurnSkipped &&
                e.TargetId == "player"), Is.True);
        }

        [Test]
        public void UnifiedIntent_RejectsAnOriginThatControlsTheWrongSide()
        {
            var engine = CreateEngine(CreateDamageCard("attack", 0, 1));
            engine.StartBattle();
            var card = engine.State.Deck.Hand.First();

            var result = engine.SubmitCardAction(new CombatActionIntent(
                "player",
                card.InstanceId,
                "enemy",
                CombatActionOrigin.EnemyAI));

            Assert.That(result.Success, Is.False);
            Assert.That(engine.State.Enemies[0].CurrentHealth, Is.EqualTo(100));
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

        private static void RegisterEnemyDeck(CombatEngine engine, params CardSpec[] specs)
        {
            var cards = specs.Select((spec, index) =>
                new CardInstance("enemy:" + spec.Id + "#" + index, spec)).ToList();
            engine.State.RegisterCombatantDeck(
                "enemy",
                new DeckState(cards, 456, false));
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

        private static CardSpec CreateGunCard(string id, int cost, int damagePerHit, int hits)
        {
            return new CardSpec(
                id,
                id,
                id,
                "guns",
                CardResourceType.ActionPoint,
                cost,
                false,
                CardTargetType.SingleEnemy,
                new[]
                {
                    new CardEffectSpec(
                        CardEffectType.Damage,
                        new UpgradeableNumber(damagePerHit, null),
                        new UpgradeableNumber(hits, null),
                        ValueUnit.Points,
                        1)
                });
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
                effect.Type == CardEffectType.Stun ||
                effect.Type == CardEffectType.Vulnerability ||
                effect.Type == CardEffectType.Bleed ||
                effect.Type == CardEffectType.BleedScaledDamage ||
                effect.Type == CardEffectType.LifeSteal ||
                effect.Type == CardEffectType.LifeStealMaxHealth ||
                effect.Type == CardEffectType.Poison ||
                effect.Type == CardEffectType.PoisonDamageBonus ||
                effect.Type == CardEffectType.BlockScaledDamage
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
