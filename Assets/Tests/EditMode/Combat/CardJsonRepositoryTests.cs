using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace KiKs.Combat.Tests
{
    public sealed class CardJsonRepositoryTests
    {
        [Test]
        public void CardLibrary_LoadsPlayerAndEnemyCards()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "CardDataV2");
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            Assert.That(repository.Cards.Count, Is.EqualTo(54));
            Assert.That(repository.Cards.Sum(card => card.DeckCopies), Is.EqualTo(76));
            Assert.That(repository.Cards.Count(card => card.IsEnemyCard), Is.EqualTo(12));
            Assert.That(
                repository.Cards.Count(card =>
                    card.Category == "enemy_big_eye" && !card.IsSpecial),
                Is.EqualTo(4));
            Assert.That(
                repository.Cards.Single(card => card.Id == "enemy_big_eye_ten_thousand_hands").IsSpecial,
                Is.True);

            var sniper = repository.GetRequiredCard("ranged_sniper_rifle");
            var damage = sniper.Effects.Single(effect => effect.Type == CardEffectType.Damage);
            Assert.That(damage.Amount.BaseValue, Is.EqualTo(60));
            Assert.That(damage.Amount.UpgradedValue, Is.EqualTo(100));
            Assert.That(damage.Hits.BaseValue, Is.EqualTo(1));
        }
        [Test]
        public void DefaultEnemyTurnRules_MatchConfiguredDifficultyFlow()
        {
            var rules = CombatRules.CreateDefault();
            var minion = rules.GetEnemyTurnRules(EnemyRank.Minion);
            var elite = rules.GetEnemyTurnRules(EnemyRank.Elite);
            var boss = rules.GetEnemyTurnRules(EnemyRank.Boss);

            Assert.That(
                new[] { minion.DeckSize, minion.BaseActionPoints, minion.CardsDrawnPerTurn, minion.CardsPlayedPerTurn },
                Is.EqualTo(new[] { 3, 3, 1, 1 }));
            Assert.That(
                new[] { elite.DeckSize, elite.BaseActionPoints, elite.CardsDrawnPerTurn, elite.CardsPlayedPerTurn },
                Is.EqualTo(new[] { 4, 4, 2, 2 }));
            Assert.That(elite.RecentCardWindowSize, Is.EqualTo(5));
            Assert.That(elite.MaxTwoCostCardsInWindow, Is.EqualTo(2));
            Assert.That(
                new[] { boss.DeckSize, boss.BaseActionPoints, boss.CardsDrawnPerTurn, boss.CardsPlayedPerTurn },
                Is.EqualTo(new[] { 4, 5, 2, 2 }));
            Assert.That(boss.BerserkTurn, Is.EqualTo(12));
        }

    }
}
