using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KiKs.Combat
{
    /// <summary>
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
            { "火", "fire" },       // 燃烧/灼烧
            { "星", "star" },       // 眩晕
            { "毒", "poison" },     // 中毒
            { "箭", "arrow" },      // 射击
            { "甲", "armor" },      // 减伤
            { "抽", "draw" },       // 抽牌
            { "闪", "dodge" },      // 闪避/攻击无效
            { "处", "execution" },  // 处决
        };

        private static readonly Regex TokenRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

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
