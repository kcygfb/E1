using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace KiKs.Combat.Tests
{
    public sealed class CardJsonRepositoryTests
    {
        [Test]
        public void ProjectCardLibrary_LoadsAllFortyThreeCards()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "CardData");
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            Assert.That(repository.Cards.Count, Is.EqualTo(43));

            var dagger = repository.GetRequiredCard("blade_dagger");
            Assert.That(dagger.CostResource, Is.EqualTo(CardResourceType.ActionPoint));
            Assert.That(dagger.CostAmount, Is.EqualTo(1));
            Assert.That(dagger.CanUpgrade, Is.True);
            Assert.That(dagger.Effects.Count, Is.EqualTo(2));
            Assert.That(dagger.Effects[0].Amount.BaseValue, Is.EqualTo(10));
            Assert.That(dagger.Effects[0].Amount.UpgradedValue, Is.EqualTo(15));

            var tachi = repository.GetRequiredCard("blade_tachi");
            var bleed = tachi.Effects.Single(effect => effect.Type == CardEffectType.Bleed);
            Assert.That(bleed.DamagePerTurn.BaseValue, Is.EqualTo(2));

            var blizzard = repository.GetRequiredCard("magic_blizzard");
            Assert.That(blizzard.CostResource, Is.EqualTo(CardResourceType.Mana));
            Assert.That(blizzard.CanUpgrade, Is.False);
        }

        [Test]
        public void Sheet3CardLibrary_LoadsEightCardsAndFifteenDeckCopies()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "CardDataV2");
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            Assert.That(repository.Cards.Count, Is.EqualTo(8));
            Assert.That(repository.Cards.Sum(card => card.DeckCopies), Is.EqualTo(15));

            var sniper = repository.GetRequiredCard("ranged_sniper_rifle");
            var damage = sniper.Effects.Single(effect => effect.Type == CardEffectType.Damage);
            Assert.That(damage.Amount.BaseValue, Is.EqualTo(12));
            Assert.That(damage.Amount.UpgradedValue, Is.EqualTo(20));
            Assert.That(damage.DamageType, Is.EqualTo(DamageType.Normal));
            Assert.That(File.ReadAllText(Path.Combine(root, "ranged.json")),
                Does.Not.Contain("damageType"));
        }
    }
}
