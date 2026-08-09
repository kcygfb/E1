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

        public static bool HasEffect(string coffeeId)
        {
            Load();
            return _effects.ContainsKey(coffeeId);
        }
    }
}
