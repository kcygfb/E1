using System.Collections.Generic;
using UnityEngine;

/// <summary>完成咖啡图标缓存。挂载在场景中，Inspector 分配各咖啡的完成态 Sprite。
/// 当杯子内容物匹配某咖啡配方时，用对应 Sprite 替换杯内多个 icon 为一个合并 icon。</summary>
public class CoffeeIconCache : MonoBehaviour
{
    public static CoffeeIconCache Instance { get; private set; }

    [Header("基础咖啡图标")]
    [SerializeField] private Sprite pourOver;
    [SerializeField] private Sprite espresso;
    [SerializeField] private Sprite americano;
    [SerializeField] private Sprite latte;
    [SerializeField] private Sprite mochaLatte;

    [Header("特殊咖啡图标")]
    [SerializeField] private Sprite budgetBrew;
    [SerializeField] private Sprite viscousDream;
    [SerializeField] private Sprite finalGaze;
    [SerializeField] private Sprite afterTaste;
    [SerializeField] private Sprite oneSnakeTwoWays;
    [SerializeField] private Sprite tentacleLabyrinth;
    [SerializeField] private Sprite freeWom;
    [SerializeField] private Sprite sunset;
    [SerializeField] private Sprite flameLatte;
    [SerializeField] private Sprite theFifthFlavor;

    [Header("半成品图标")]
    [SerializeField] private Sprite viscousDreamHalf;
    [SerializeField] private Sprite afterTasteHalf;
    [SerializeField] private Sprite sunsetHalf;
    [SerializeField] private Sprite tentacleLabyrinthHalf;
    [SerializeField] private Sprite theFifthFlavorHalf;

    private readonly Dictionary<string, Sprite> _sprites = new();
    private readonly Dictionary<string, Sprite> _halfSprites = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _sprites["PourOver"] = pourOver;
        _sprites["Espresso"] = espresso;
        _sprites["Americano"] = americano;
        _sprites["Latte"] = latte;
        _sprites["MochaLatte"] = mochaLatte;
        _sprites["BudgetBrew"] = budgetBrew;
        _sprites["ViscousDream"] = viscousDream;
        _sprites["FinalGaze"] = finalGaze;
        _sprites["AfterTaste"] = afterTaste;
        _sprites["OneSnakeTwoWays"] = oneSnakeTwoWays;
        _sprites["TentacleLabyrinth"] = tentacleLabyrinth;
        _sprites["FreeWom"] = freeWom;
        _sprites["Sunset"] = sunset;
        _sprites["FlameLatte"] = flameLatte;
        _sprites["TheFifthFlavor"] = theFifthFlavor;

        _halfSprites["ViscousDream"] = viscousDreamHalf;
        _halfSprites["AfterTaste"] = afterTasteHalf;
        _halfSprites["Sunset"] = sunsetHalf;
        _halfSprites["TentacleLabyrinth"] = tentacleLabyrinthHalf;
        _halfSprites["TheFifthFlavor"] = theFifthFlavorHalf;
    }

    public Sprite GetCoffeeSprite(string coffeeId)
    {
        if (string.IsNullOrEmpty(coffeeId)) return null;
        return _sprites.TryGetValue(coffeeId, out var sp) ? sp : null;
    }

    public Sprite GetHalfProductSprite(string coffeeId)
    {
        if (string.IsNullOrEmpty(coffeeId)) return null;
        return _halfSprites.TryGetValue(coffeeId, out var sp) ? sp : null;
    }
}
