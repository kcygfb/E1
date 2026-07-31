using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace KiKs.Combat.Tests
{
    public sealed class CardJsonRepositoryTests
    {
        [Test]
        public void X5CardLibrary_LoadsFortyTwoCardsAndSixtyFourDeckCopies()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "CardDataV2");
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            Assert.That(repository.Cards.Count, Is.EqualTo(42));
            Assert.That(repository.Cards.Sum(card => card.DeckCopies), Is.EqualTo(64));

            var sniper = repository.GetRequiredCard("ranged_sniper_rifle");
            var damage = sniper.Effects.Single(effect => effect.Type == CardEffectType.Damage);
            Assert.That(damage.Amount.BaseValue, Is.EqualTo(60));
            Assert.That(damage.Amount.UpgradedValue, Is.EqualTo(100));
            Assert.That(damage.Hits.BaseValue, Is.EqualTo(1));
        }
    }
}
