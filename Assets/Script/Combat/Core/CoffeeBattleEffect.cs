using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KiKs.Combat
{
    public enum CoffeeEffectType { Heal, Bleed, Block, Damage }
    public enum CoffeeTarget { Self, Enemy }

    [System.Serializable]
    public struct CoffeeBattleEffect
    {
        public CoffeeEffectType Type;
        public int Amount;
        public CoffeeTarget Target;
    }

    /// <summary>
    /// 咖啡战斗效果注册表。从 StreamingAssets/CoffeeData/*.json 的 battleEffect 字段动态加载。
    /// KiKs.Combat asmdef 不能引用 Assembly-CSharp 的 CoffeeDataLoader，所以自行读文件解析。
    /// </summary>
    public static class CoffeeEffectRegistry
    {
        private static readonly Dictionary<string, string> ChineseDisplayNames = new(StringComparer.Ordinal)
        {
            ["PourOver"] = "手冲咖啡",
            ["Espresso"] = "浓缩咖啡",
            ["Americano"] = "美式咖啡",
            ["Latte"] = "拿铁",
            ["MochaLatte"] = "摩卡拿铁",
            ["BudgetBrew"] = "平价特调",
            ["ViscousDream"] = "黏稠之梦",
            ["FinalGaze"] = "最终凝视",
            ["AfterTaste"] = "回味",
            ["OneSnakeTwoWays"] = "一蛇两吃",
            ["TentacleLabyrinth"] = "触手迷宫",
            ["FreeWom"] = "自由狼毫",
            ["Sunset"] = "日落",
            ["FlameLatte"] = "烈焰拿铁",
            ["TheFifthFlavor"] = "第五味",
            ["ESSymphony"] = "眼蛇交响曲",
            ["BloodGarment"] = "血衣咖啡"
        };

        [Serializable]
        private struct BattleEffectJson
        {
            public string type;
            public int amount;
            public string target;
        }

        [Serializable]
        private struct CoffeeEffectEntry
        {
            public string coffeeId;
            public string coffeeName;
            public BattleEffectJson battleEffect;
        }

        private static readonly Dictionary<string, CoffeeBattleEffect> _effects = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _displayNames = new(StringComparer.Ordinal);
        private static bool _loaded;

        private const string CoffeeDataDirectory = "CoffeeData";

        /// <summary>加载所有咖啡 JSON 的 battleEffect。可安全多次调用（只有首次或 Reload 后才真正读文件）。</summary>
        public static void Load()
        {
            if (_loaded) return;
            Reload();
        }

        /// <summary>强制重新从磁盘加载。</summary>
        public static void Reload()
        {
            _effects.Clear();
            _displayNames.Clear();

            var dir = Path.Combine(Application.streamingAssetsPath, CoffeeDataDirectory);
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning($"[CoffeeEffectRegistry] Directory not found: {dir}");
                _loaded = true;
                return;
            }

            foreach (var filePath in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                string json;
                try { json = File.ReadAllText(filePath); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CoffeeEffectRegistry] Cannot read {Path.GetFileName(filePath)}: {e.Message}");
                    continue;
                }

                CoffeeEffectEntry entry;
                try { entry = JsonUtility.FromJson<CoffeeEffectEntry>(json); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CoffeeEffectRegistry] Cannot parse {Path.GetFileName(filePath)}: {e.Message}");
                    continue;
                }

                if (entry.coffeeId == null) continue;

                if (!string.IsNullOrEmpty(entry.coffeeName))
                    _displayNames[entry.coffeeId] = entry.coffeeName;

                if (entry.battleEffect.type == null) continue;

                if (!Enum.TryParse(entry.battleEffect.type, true, out CoffeeEffectType type))
                {
                    Debug.LogWarning($"[CoffeeEffectRegistry] Unknown effect type '{entry.battleEffect.type}' for {entry.coffeeId}");
                    continue;
                }

                var target = string.Equals(entry.battleEffect.target, "Self", StringComparison.OrdinalIgnoreCase)
                    ? CoffeeTarget.Self
                    : CoffeeTarget.Enemy;

                _effects[entry.coffeeId] = new CoffeeBattleEffect
                {
                    Type = type,
                    Amount = entry.battleEffect.amount,
                    Target = target
                };
            }

            _loaded = true;
            Debug.Log($"[CoffeeEffectRegistry] Loaded {_effects.Count} battle effects from JSON.");
        }

        public static bool TryGet(string coffeeId, out CoffeeBattleEffect effect)
        {
            Load();
            return _effects.TryGetValue(coffeeId, out effect);
        }

        public static string GetDisplayName(string coffeeId)
        {
            Load();
            return _displayNames.TryGetValue(coffeeId, out var name) ? name : coffeeId;
        }

        /// <summary>提示框使用的中文咖啡名。未知的新配方仍回退到数据文件中的名称。</summary>
        public static string GetChineseDisplayName(string coffeeId)
        {
            if (string.IsNullOrWhiteSpace(coffeeId))
                return "咖啡";

            return ChineseDisplayNames.TryGetValue(coffeeId, out var name)
                ? name
                : GetDisplayName(coffeeId);
        }

        /// <summary>根据实际战斗效果生成选择界面与战斗界面共用的中文悬停说明。</summary>
        public static string BuildChineseTooltip(string coffeeId)
        {
            var displayName = GetChineseDisplayName(coffeeId);
            if (!TryGet(coffeeId, out var effect))
                return $"{displayName}\n该咖啡暂未配置可用的战斗效果。";

            var target = effect.Target == CoffeeTarget.Self ? "自身" : "敌人";
            var effectDescription = effect.Type switch
            {
                CoffeeEffectType.Heal => $"为{target}回复 {effect.Amount} 点生命",
                CoffeeEffectType.Bleed => $"使{target}获得 {effect.Amount} 层流血",
                CoffeeEffectType.Block => $"使{target}获得 {effect.Amount} 点格挡",
                CoffeeEffectType.Damage => $"对{target}造成 {effect.Amount} 点伤害",
                _ => "产生特殊效果"
            };
            var usage = effect.Target == CoffeeTarget.Self
                ? "将咖啡拖到己方角色头像上使用。"
                : "将咖啡拖到敌方角色头像上使用。";

            return $"{displayName}\n战斗效果：{effectDescription}。\n使用方法：{usage}";
        }

        public static bool HasEffect(string coffeeId)
        {
            Load();
            return _effects.ContainsKey(coffeeId);
        }
    }
}
