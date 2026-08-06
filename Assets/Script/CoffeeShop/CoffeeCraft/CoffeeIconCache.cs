using System.Collections.Generic;
using UnityEngine;

/// <summary>完成咖啡图标缓存。挂载在场景中，Inspector 分配各咖啡的完成态 Sprite。
/// 当杯子内容物匹配某咖啡配方时，用对应 Sprite 替换杯内多个 icon 为一个合并 icon。</summary>
public class CoffeeIconCache : MonoBehaviour
{
    public static CoffeeIconCache Instance { get; private set; }

    [Header("完成咖啡图标")]
    [SerializeField] private Sprite pourOver;
    [SerializeField] private Sprite espresso;
    [SerializeField] private Sprite americano;
    [SerializeField] private Sprite latte;
    [SerializeField] private Sprite mochaLatte;

    private readonly Dictionary<string, Sprite> _sprites = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _sprites["PourOver"] = pourOver;
        _sprites["Espresso"] = espresso;
        _sprites["Americano"] = americano;
        _sprites["Latte"] = latte;
        _sprites["MochaLatte"] = mochaLatte;
    }

    public Sprite GetCoffeeSprite(string coffeeId)
    {
        if (string.IsNullOrEmpty(coffeeId)) return null;
        return _sprites.TryGetValue(coffeeId, out var sp) ? sp : null;
    }
}
