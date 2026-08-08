using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace KiKs.Combat.Tests
{
    public sealed class CardJsonRepositoryTests
    {
        private static string CardDataRoot => Path.Combine(Application.streamingAssetsPath, "CardDataV2");
        [Test]
        public void CardLibrary_LoadsPlayerAndEnemyCards()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "CardDataV2");
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            Assert.That(repository.Cards.Count, Is.EqualTo(85));
            Assert.That(repository.Cards.Sum(card => card.DeckCopies), Is.EqualTo(85));
            Assert.That(repository.Cards.Count(card => card.IsEnemyCard), Is.EqualTo(39));
            Assert.That(repository.Cards.All(card => !string.IsNullOrWhiteSpace(card.DescriptionEn)), Is.True);
            Assert.That(
                repository.Cards.Count(card =>
                    card.Category == "enemy_big_eye" && !card.IsSpecial),
                Is.EqualTo(5));
            Assert.That(
                repository.Cards.Single(card => card.Id == "enemy_big_eye_ten_thousand_hands").IsSpecial,
                Is.True);

            var sniper = repository.GetRequiredCard("ranged_sniper_rifle");
            var damage = sniper.Effects.Single(effect => effect.Type == CardEffectType.Damage);
            Assert.That(damage.Amount.BaseValue, Is.EqualTo(12));
            Assert.That(damage.Amount.UpgradedValue, Is.EqualTo(20));
            Assert.That(damage.Hits.BaseValue, Is.EqualTo(1));

            var scalpel = repository.GetRequiredCard("bleed_scalpel");
            var bleedEffect = scalpel.Effects.Single(effect => effect.Type == CardEffectType.Bleed);
            Assert.That(scalpel.DisplayNameEn, Is.EqualTo("scalpel"));
            Assert.That(bleedEffect.Amount.BaseValue, Is.GreaterThan(0));
        }

        [Test]
        public void CardLibrary_ParsesGeneratedChineseDescriptions()
        {
            var root = CardDataRoot;
            var manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            var repository = CardJsonRepository.Load(
                manifest,
                fileName => File.ReadAllText(Path.Combine(root, fileName)));

            // 全部 85 张卡都已生成 zhCN 文案（含图标标记），en 不受影响
            Assert.That(repository.Cards.All(card => !string.IsNullOrWhiteSpace(card.DescriptionZhCn)), Is.True);
            Assert.That(repository.Cards.All(card => !string.IsNullOrWhiteSpace(card.DescriptionEn)), Is.True);

            // 抽样验证文案语义
            Assert.That(repository.GetRequiredCard("heavy_labrys").DescriptionZhCn,
                Is.EqualTo("造成{剑}4点伤害，削减{盾}8点韧性"));
            Assert.That(repository.GetRequiredCard("ranged_sniper_rifle").DescriptionZhCn,
                Is.EqualTo("造成{剑}12点伤害"));
            Assert.That(repository.GetRequiredCard("magic_lifesteal").DescriptionZhCn,
                Is.EqualTo("造成{剑}9点伤害并回复等量生命"));
            Assert.That(repository.GetRequiredCard("magic_shield_bash").DescriptionZhCn,
                Is.EqualTo("造成{盾}与当前格挡值等量的伤害"));

            // 敌人卡纯文字：不包含任何图标标记
            Assert.That(repository.GetRequiredCard("enemy_fatty_devour").DescriptionZhCn,
                Is.EqualTo("恢复20点生命"));
            Assert.That(repository.Cards.Where(card => card.IsEnemyCard)
                .All(card => !card.DescriptionZhCn.Contains("{")), Is.True);
        }

        [Test]
        public void CardJsonFiles_HaveNoUtf8Bom()
        {
            foreach (var file in Directory.GetFiles(CardDataRoot, "*.json"))
            {
                var bytes = File.ReadAllBytes(file);
                var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                Assert.That(hasBom, Is.False, $"{file} 含有 UTF-8 BOM，会导致 CardJsonRepository 解析失败");
            }
        }

        [Test]
        public void CardDescriptionObjects_OnlyContainEnAndZhCnKeys()
        {
            var descRegex = new Regex("\"description\"\\s*:\\s*\\{([^}]*)\\}");
            var keyRegex = new Regex("\"([^\"]+)\"\\s*:");

            // 只检查卡牌数据文件（manifest 与 schema 是元数据，其内部也有 description 关键字）
            foreach (var file in Directory.GetFiles(CardDataRoot, "*.json")
                .Where(path => !path.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase)
                            && !path.EndsWith("card-data.schema.json", StringComparison.OrdinalIgnoreCase)))
            {
                var json = File.ReadAllText(file);
                foreach (Match descMatch in descRegex.Matches(json))
                {
                    var keys = keyRegex.Matches(descMatch.Groups[1].Value)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .ToList();

                    Assert.That(keys.Except(new[] { "en", "zhCN" }), Is.Empty,
                        $"{file} 中 description 对象含未知键: {string.Join(",", keys.Except(new[] { "en", "zhCN" }))}");
                }
            }
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
