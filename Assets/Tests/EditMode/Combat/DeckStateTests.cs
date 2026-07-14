using System.Collections.Generic;
using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class DeckStateTests
    {
        [Test]
        public void Draw_WhenOnlyThreeRemain_ReshufflesAndDrawsFourthCard()
        {
            var deck = new DeckState(CreateCards(7), 123, false);
            Assert.That(deck.Draw(4, 10).DrawnCards.Count, Is.EqualTo(4));
            Assert.That(deck.DrawPile.Count, Is.EqualTo(3));

            deck.DiscardHand();
            var secondDraw = deck.Draw(4, 10);

            Assert.That(secondDraw.DrawnCards.Count, Is.EqualTo(4));
            Assert.That(secondDraw.ReshuffleCount, Is.EqualTo(1));
            Assert.That(deck.Hand.Count, Is.EqualTo(4));
        }

        [Test]
        public void Draw_WhenHandIsFull_SendsOverflowCardsToDiscardPile()
        {
            var deck = new DeckState(CreateCards(5), 123, false);
            var result = deck.Draw(4, 2);

            Assert.That(result.DrawnCards.Count, Is.EqualTo(2));
            Assert.That(result.OverflowDiscardedCards.Count, Is.EqualTo(2));
            Assert.That(deck.Hand.Count, Is.EqualTo(2));
            Assert.That(deck.DiscardPile.Count, Is.EqualTo(2));
        }

        private static IEnumerable<CardInstance> CreateCards(int count)
        {
            var effect = new CardEffectSpec(
                CardEffectType.Damage,
                new UpgradeableNumber(1, 2),
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
                string.Empty);
            var spec = new CardSpec(
                "test", "Test", "Test", "test",
                CardResourceType.ActionPoint, 1, false,
                CardTargetType.SingleEnemy, new[] { effect });
            var cards = new List<CardInstance>();
            for (var i = 0; i < count; i++) cards.Add(new CardInstance("test#" + i, spec));
            return cards;
        }
    }
}
