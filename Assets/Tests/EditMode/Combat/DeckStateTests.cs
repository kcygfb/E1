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
        public void DrawAfterDiscard_ReshufflesTheSamePhysicalCardInstances()
        {
            var deck = new DeckState(CreateCards(3), 123, false);
            var firstDraw = deck.Draw(3, 10);

            deck.DiscardHand();
            var secondDraw = deck.Draw(3, 10);

            Assert.That(secondDraw.ReshuffleCount, Is.EqualTo(1));
            CollectionAssert.AreEquivalent(firstDraw.DrawnCards, secondDraw.DrawnCards);
            Assert.That(deck.DrawPile.Count, Is.EqualTo(0));
            Assert.That(deck.DiscardPile.Count, Is.EqualTo(0));
            Assert.That(deck.Hand.Count, Is.EqualTo(3));
        }

        [Test]
        public void TakeFromDiscard_RemovesTopDiscardCards_AndReturnAddsThemBack()
        {
            var deck = new DeckState(CreateCards(3), 123, false);
            var firstDraw = deck.Draw(2, 10);
            var firstCard = firstDraw.DrawnCards[0];
            var secondCard = firstDraw.DrawnCards[1];
            deck.DiscardFromHand(firstCard.InstanceId, out _);
            deck.DiscardFromHand(secondCard.InstanceId, out _);

            var taken = deck.TakeFromDiscard(1);

            Assert.That(taken.Count, Is.EqualTo(1));
            Assert.That(taken[0], Is.SameAs(secondCard));
            Assert.That(deck.DiscardPile.Count, Is.EqualTo(1));

            deck.ReturnToDiscard(taken[0]);
            Assert.That(deck.DiscardPile.Count, Is.EqualTo(2));
            Assert.That(deck.DiscardPile[1], Is.SameAs(secondCard));
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
                ValueUnit.Points,
                1);
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
