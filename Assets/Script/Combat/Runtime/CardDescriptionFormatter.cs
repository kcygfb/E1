using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KiKs.Combat
{
    /// <summary>
    /// 动态数值使用 {amount:效果序号} / {hits:效果序号} 绑定到 CardSpec.Effects。
    /// 卡面描述转换：把 JSON 文案里的 `{剑}` 标记替换成 TMP 行内图标标签。
    /// 例：`造成{剑}*5点伤害` → `造成&lt;sprite name="sword"&gt;*5点伤害`，图标在那一格渲染。
    /// 规则：`{中文token}` 命中下表 → 换成对应图片；未知 token 原样保留（便于发现漏配）。
    /// 图标资源由「Tools/KiKs/Card/生成卡牌图标SpriteAsset」从 Assets/Art/CardIcons/*.png 生成，
    /// 文件名必须与下表 value 一致（如 剑 → sword.png）。
    /// </summary>
    public static class CardDescriptionFormatter
    {
        /// <summary>中文标记 → 图标文件名（SpriteAsset 里的 sprite 名）</summary>
        private static readonly Dictionary<string, string> IconMap = new()
        {
            { "剑", "sword" },      // 攻击
            { "盾", "shield" },     // 韧性/格挡
            { "心", "heart" },      // 治疗
            { "血", "blood" },      // 流血/吸血
            { "星", "star" },       // 眩晕
            { "毒", "poison" },     // 中毒
            { "甲", "armor" },      // 减伤
            { "闪", "dodge" },      // 闪避/攻击无效
            { "处", "execution" },  // 处决
        };

        private static readonly Regex TokenRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);
        private static readonly Regex EffectValueRegex = new(
            @"\{(amount|hits):(\d+)\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>选择卡牌的本地化描述，并按当前强化状态解析效果数值和图标。</summary>
        public static string FormatDescription(CardSpec spec, bool isUpgraded)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            var template = !string.IsNullOrWhiteSpace(spec.DescriptionZhCn)
                ? spec.DescriptionZhCn
                : spec.DescriptionEn;
            return Format(template, spec.Effects, isUpgraded);
        }

        /// <summary>按效果序号解析动态数值，然后转换图标标记。</summary>
        public static string Format(
            string text,
            IReadOnlyList<CardEffectSpec> effects,
            bool isUpgraded)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (effects == null) throw new ArgumentNullException(nameof(effects));

            var resolvedText = EffectValueRegex.Replace(text, match =>
            {
                if (!int.TryParse(match.Groups[2].Value, out var effectIndex) ||
                    effectIndex < 0 || effectIndex >= effects.Count)
                    return match.Value;

                var effect = effects[effectIndex];
                var value = string.Equals(
                    match.Groups[1].Value,
                    "hits",
                    StringComparison.OrdinalIgnoreCase)
                    ? effect.Hits.Resolve(isUpgraded)
                    : effect.Amount.Resolve(isUpgraded);
                return value.ToString();
            });

            return Format(resolvedText);
        }

        /// <summary>把 `{token}` 全部替换成 <sprite> 标签，返回可直接赋给 TMP 文本的字符串。</summary>
        public static string Format(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return TokenRegex.Replace(text, match =>
            {
                var token = match.Groups[1].Value.Trim();
                return IconMap.TryGetValue(token, out var spriteName)
                    ? $"<sprite name=\"{spriteName}\">"
                    : match.Value;
            });
        }

        /// <summary>供测试/调试：查询某个 token 是否已注册。</summary>
        public static bool HasToken(string token) => IconMap.ContainsKey(token);
    }
}
