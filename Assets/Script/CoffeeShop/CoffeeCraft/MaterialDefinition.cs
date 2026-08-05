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
    }

    private static readonly List<MatInfo> _all = new()
    {
        new MatInfo { id = "claw",        displayName = "爪子",     color = new Color(0.85f, 0.75f, 0.55f) },
        new MatInfo { id = "wolfHair",    displayName = "狼毫",     color = new Color(0.70f, 0.70f, 0.75f) },
        new MatInfo { id = "eyeball",     displayName = "眼珠",     color = new Color(0.55f, 0.80f, 0.55f) },
        new MatInfo { id = "purpleFlame", displayName = "紫色火焰", color = new Color(0.70f, 0.35f, 0.85f) },
        new MatInfo { id = "snakeDried",  displayName = "蛇干",     color = new Color(0.80f, 0.65f, 0.40f) },
        new MatInfo { id = "tentacle",    displayName = "触手",     color = new Color(0.60f, 0.55f, 0.75f) },
        new MatInfo { id = "CoffeeBean",  displayName = "咖啡豆",   color = new Color(0.45f, 0.28f, 0.15f) },
        new MatInfo { id = "Milk",        displayName = "牛奶",     color = new Color(0.95f, 0.95f, 0.90f) },
        new MatInfo { id = "Sugar",       displayName = "糖",       color = new Color(0.90f, 0.85f, 0.70f) },
        new MatInfo { id = "Water",       displayName = "水",       color = new Color(0.45f, 0.60f, 0.85f) },
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
