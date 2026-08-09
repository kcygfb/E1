using System.Collections.Generic;
using UnityEngine;

/// <summary>材料元数据。硬编码 10 种可选材料的 id/displayName/颜色，供 UI 渲染和拖拽使用。
/// Sprite 缓存由 MaterialSpriteCache 在运行时填充。</summary>
public static class MaterialDefinition
{
    public class MatInfo
    {
        public string id;
        public string displayName;
        public Color color;
        public bool isRaw = true; // true=原始材料(可放仓库), false=机器产出
    }

    private static readonly List<MatInfo> _all = new()
    {
        // 原始材料（可放入仓库/TrayGrid）
        new MatInfo { id = "claw",        displayName = "爪子",     color = new Color(0.85f, 0.75f, 0.55f) },
        new MatInfo { id = "wolffur",     displayName = "狼毫",     color = new Color(0.70f, 0.70f, 0.75f) },
        new MatInfo { id = "eye",         displayName = "眼珠",     color = new Color(0.55f, 0.80f, 0.55f) },
        new MatInfo { id = "fire",        displayName = "紫色火焰", color = new Color(0.70f, 0.35f, 0.85f) },
        new MatInfo { id = "oil",         displayName = "肥油",     color = new Color(0.80f, 0.60f, 0.30f) },
        new MatInfo { id = "snake",       displayName = "蛇干",     color = new Color(0.80f, 0.65f, 0.40f) },
        new MatInfo { id = "tentacle",    displayName = "触手",     color = new Color(0.60f, 0.55f, 0.75f) },
        new MatInfo { id = "CoffeeBean",  displayName = "咖啡豆",   color = new Color(0.45f, 0.28f, 0.15f) },
        new MatInfo { id = "Milk",        displayName = "牛奶",     color = new Color(0.95f, 0.95f, 0.90f) },
        new MatInfo { id = "Sugar",       displayName = "糖",       color = new Color(0.90f, 0.85f, 0.70f) },
        new MatInfo { id = "Water",       displayName = "水",       color = new Color(0.45f, 0.60f, 0.85f) },
        // 机器产出材料（不能放入仓库）— 通用产出
        new MatInfo { id = "GroundCoffee",  displayName = "咖啡粉",   color = new Color(0.35f, 0.20f, 0.10f), isRaw = false },
        new MatInfo { id = "Espresso",      displayName = "浓缩咖啡", color = new Color(0.25f, 0.12f, 0.05f), isRaw = false },
        new MatInfo { id = "SteamedMilk",   displayName = "奶泡",     color = new Color(0.98f, 0.96f, 0.92f), isRaw = false },
        new MatInfo { id = "PourOverCoffee",displayName = "手冲咖啡", color = new Color(0.30f, 0.18f, 0.08f), isRaw = false },
        // 磨粉产物
        new MatInfo { id = "clawPowder",     displayName = "爪粉",   color = new Color(0.75f, 0.65f, 0.45f), isRaw = false },
        new MatInfo { id = "eyePowder",      displayName = "眼珠粉", color = new Color(0.45f, 0.70f, 0.45f), isRaw = false },
        new MatInfo { id = "firePowder",     displayName = "火焰粉", color = new Color(0.60f, 0.30f, 0.75f), isRaw = false },
        new MatInfo { id = "oilPowder",      displayName = "肥油粉", color = new Color(0.70f, 0.50f, 0.20f), isRaw = false },
        new MatInfo { id = "snakePowder",    displayName = "蛇粉",   color = new Color(0.70f, 0.55f, 0.30f), isRaw = false },
        new MatInfo { id = "tentaclePowder", displayName = "触手粉", color = new Color(0.50f, 0.45f, 0.65f), isRaw = false },
        new MatInfo { id = "wolffurPowder",  displayName = "狼毫粉", color = new Color(0.60f, 0.60f, 0.65f), isRaw = false },
        // 萃取液产物
        new MatInfo { id = "clawEspresso",     displayName = "爪子浓缩", color = new Color(0.65f, 0.55f, 0.35f), isRaw = false },
        new MatInfo { id = "eyeEspresso",      displayName = "眼珠浓缩", color = new Color(0.35f, 0.60f, 0.35f), isRaw = false },
        new MatInfo { id = "fireEspresso",     displayName = "火焰浓缩", color = new Color(0.50f, 0.20f, 0.65f), isRaw = false },
        new MatInfo { id = "oilEspresso",      displayName = "肥油浓缩", color = new Color(0.60f, 0.40f, 0.10f), isRaw = false },
        new MatInfo { id = "snakeEspresso",    displayName = "蛇干浓缩", color = new Color(0.60f, 0.45f, 0.20f), isRaw = false },
        new MatInfo { id = "tentacleEspresso", displayName = "触手浓缩", color = new Color(0.40f, 0.35f, 0.55f), isRaw = false },
        new MatInfo { id = "wolffurEspresso",  displayName = "狼毫浓缩", color = new Color(0.50f, 0.50f, 0.55f), isRaw = false },
        // ESM 萃取液（eye+snake 双粉萃取）
        new MatInfo { id = "ESMEspresso",  displayName = "ESM浓缩", color = new Color(0.48f, 0.55f, 0.35f), isRaw = false },
        // 手冲球产物
        new MatInfo { id = "clawBall",     displayName = "爪子手冲球",   color = new Color(0.70f, 0.60f, 0.40f), isRaw = false },
        new MatInfo { id = "eyeBall",      displayName = "眼珠手冲球",   color = new Color(0.40f, 0.65f, 0.40f), isRaw = false },
        new MatInfo { id = "fireBall",     displayName = "火焰手冲球",   color = new Color(0.55f, 0.25f, 0.70f), isRaw = false },
        new MatInfo { id = "oilBall",      displayName = "肥油手冲球",   color = new Color(0.65f, 0.45f, 0.15f), isRaw = false },
        new MatInfo { id = "snakeBall",    displayName = "蛇干手冲球",   color = new Color(0.65f, 0.50f, 0.25f), isRaw = false },
        new MatInfo { id = "tentacleBall", displayName = "触手手冲球",   color = new Color(0.45f, 0.40f, 0.60f), isRaw = false },
        new MatInfo { id = "wolffurBall",  displayName = "狼毫手冲球",   color = new Color(0.55f, 0.55f, 0.60f), isRaw = false },
        // 未知产物
        new MatInfo { id = "Unknown",      displayName = "未知产物", color = new Color(0.5f, 0.5f, 0.5f), isRaw = false },
    };

    private static readonly Dictionary<string, MatInfo> _map = new();
    private static Dictionary<string, Sprite> _sprites = new();
    private static Dictionary<string, Sprite> _spritesAll = new();

    static MaterialDefinition()
    {
        foreach (var info in _all)
            _map[info.id] = info;
    }

    public static MatInfo Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _map.TryGetValue(id, out var info) ? info : null;
    }

    public static IReadOnlyList<MatInfo> All => _all;

    /// <summary>由 MaterialSpriteCache 调用，填充 sprite 缓存。</summary>
    public static void SetSpriteCache(Dictionary<string, Sprite> sprites) => _sprites = sprites;

    /// <summary>由 MaterialSpriteCache 调用，填充 *ALL 整图缓存。</summary>
    public static void SetSpriteAllCache(Dictionary<string, Sprite> spritesAll) => _spritesAll = spritesAll;

    /// <summary>获取材料的单个图标 sprite，未分配则返回 null。</summary>
    public static Sprite GetSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _sprites.TryGetValue(id, out var sp) ? sp : null;
    }

    /// <summary>获取材料的整图（*ALL）sprite，未分配则返回 null。</summary>
    public static Sprite GetSpriteAll(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _spritesAll.TryGetValue(id, out var sp) ? sp : null;
    }
}
