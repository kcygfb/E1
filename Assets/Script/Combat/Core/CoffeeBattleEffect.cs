using System.Collections.Generic;

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
    /// 硬编码咖啡战斗效果注册表。后续可改为从 JSON 加载。
    /// </summary>
    public static class CoffeeEffectRegistry
    {
        private static readonly Dictionary<string, CoffeeBattleEffect> _effects = new()
        {
            { "PourOver", new CoffeeBattleEffect { Type = CoffeeEffectType.Heal, Amount = 20, Target = CoffeeTarget.Self } },
            { "BloodGarment", new CoffeeBattleEffect { Type = CoffeeEffectType.Bleed, Amount = 3, Target = CoffeeTarget.Enemy } },
        };

        public static bool TryGet(string coffeeId, out CoffeeBattleEffect effect) => _effects.TryGetValue(coffeeId, out effect);

        public static string GetDisplayName(string coffeeId) => coffeeId switch
        {
            "PourOver" => "手冲咖啡",
            "BloodGarment" => "血衣",
            _ => coffeeId,
        };
    }
}
