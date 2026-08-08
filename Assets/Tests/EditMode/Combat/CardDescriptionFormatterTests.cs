using NUnit.Framework;

namespace KiKs.Combat.Tests
{
    public sealed class CardDescriptionFormatterTests
    {
        [Test]
        public void Format_SingleToken_ConvertsToSpriteTag()
        {
            var result = CardDescriptionFormatter.Format("挥舞巨斧，造成{剑}4点伤害");
            Assert.That(result, Is.EqualTo("挥舞巨斧，造成<sprite name=\"sword\">4点伤害"));
        }

        [Test]
        public void Format_IconThenAsteriskNumber_KeepsNumberAsPlainText()
        {
            var result = CardDescriptionFormatter.Format("{剑}*2");
            Assert.That(result, Is.EqualTo("<sprite name=\"sword\">*2"));
        }

        [Test]
        public void Format_IconThenNumber_KeepsNumberAsPlainText()
        {
            var result = CardDescriptionFormatter.Format("精准射击，造成{剑}12点伤害");
            Assert.That(result, Is.EqualTo("精准射击，造成<sprite name=\"sword\">12点伤害"));
        }

        [Test]
        public void Format_ConsecutiveTokens_ConvertsBoth()
        {
            var result = CardDescriptionFormatter.Format("{剑}{盾}");
            Assert.That(result, Is.EqualTo("<sprite name=\"sword\"><sprite name=\"shield\">"));
        }

        [Test]
        public void Format_MultipleTokensMixedWithText_ConvertsAll()
        {
            var result = CardDescriptionFormatter.Format("造成{剑}5点伤害，并削减{盾}22点韧性");
            Assert.That(result, Is.EqualTo("造成<sprite name=\"sword\">5点伤害，并削减<sprite name=\"shield\">22点韧性"));
        }

        [Test]
        public void Format_UnknownToken_KeptAsIs()
        {
            var result = CardDescriptionFormatter.Format("造成{雷}1点伤害");
            Assert.That(result, Is.EqualTo("造成{雷}1点伤害"));
        }

        [Test]
        public void Format_TokenWithInnerWhitespace_TrimsAndConverts()
        {
            var result = CardDescriptionFormatter.Format("{ 剑 }");
            Assert.That(result, Is.EqualTo("<sprite name=\"sword\">"));
        }

        [Test]
        public void Format_EmptyOrNull_ReturnsInput()
        {
            Assert.That(CardDescriptionFormatter.Format(""), Is.Empty);
            Assert.That(CardDescriptionFormatter.Format(null), Is.Null);
        }

        [Test]
        public void Format_NoTokens_Unchanged()
        {
            const string plain = "普通的描述文字";
            Assert.That(CardDescriptionFormatter.Format(plain), Is.EqualTo(plain));
        }

        [Test]
        public void CoreTokens_AllRegistered()
        {
            // 当前保留的 token：剑盾心血星毒甲闪处（箭/火/抽已按用户要求移除映射）
            foreach (var token in new[] { "剑", "盾", "心", "血", "星", "毒", "甲", "闪", "处" })
                Assert.That(CardDescriptionFormatter.HasToken(token), Is.True, $"token {token} 应已注册");
            Assert.That(CardDescriptionFormatter.HasToken("不存在的token"), Is.False);
            // 已移除的 token 应返回 false
            foreach (var token in new[] { "箭", "火", "抽" })
                Assert.That(CardDescriptionFormatter.HasToken(token), Is.False, $"token {token} 应已移除");
        }
    }
}
