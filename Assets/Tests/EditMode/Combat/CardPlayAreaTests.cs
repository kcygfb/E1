using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KiKs.Combat.Tests
{
    public sealed class CardPlayAreaTests
    {
        private GameObject _canvasObject;
        private GameObject _playAreaObject;
        private GameObject _cardObject;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _playAreaObject = new GameObject("PlayArea", typeof(RectTransform));
            var playArea = _playAreaObject.GetComponent<RectTransform>();
            playArea.SetParent(_canvasObject.transform, false);
            playArea.anchorMin = new Vector2(0.5f, 0.5f);
            playArea.anchorMax = new Vector2(0.5f, 0.5f);
            playArea.sizeDelta = new Vector2(400f, 300f);

            _cardObject = new GameObject("Card", typeof(RectTransform), typeof(CardView));
            var cardRect = _cardObject.GetComponent<RectTransform>();
            cardRect.SetParent(_canvasObject.transform, false);

            Canvas.ForceUpdateCanvases();
            _cardObject.GetComponent<CardView>().SetPlayArea(playArea);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cardObject);
            Object.DestroyImmediate(_playAreaObject);
            Object.DestroyImmediate(_canvasObject);
        }

        [Test]
        public void CardCenterInsidePlayArea_IsAccepted()
        {
            _cardObject.GetComponent<RectTransform>().position =
                _playAreaObject.GetComponent<RectTransform>().position;

            Assert.That(IsOverPlayArea(_cardObject.GetComponent<CardView>()), Is.True);
        }

        [Test]
        public void CardCenterOutsidePlayArea_IsRejected()
        {
            var playArea = _playAreaObject.GetComponent<RectTransform>();
            _cardObject.GetComponent<RectTransform>().position =
                playArea.position + new Vector3(playArea.rect.width, playArea.rect.height, 0f);

            Assert.That(IsOverPlayArea(_cardObject.GetComponent<CardView>()), Is.False);
        }

        [Test]
        public void MissingPlayArea_IsRejected()
        {
            var cardView = _cardObject.GetComponent<CardView>();
            cardView.SetPlayArea(null);

            Assert.That(IsOverPlayArea(cardView), Is.False);
        }

        private static bool IsOverPlayArea(CardView cardView)
        {
            var method = typeof(CardView).GetMethod(
                "IsOverPlayArea",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(cardView, null);
        }
    }
}
