using System.Collections.Generic;
using UnityEngine;

/// <summary>材料图标缓存。在场景中挂此组件，Inspector 里分配材料 Sprite，
/// 运行时填充 MaterialDefinition 的静态缓存供 UI 使用。</summary>
public class MaterialSpriteCache : MonoBehaviour
{
    [Header("Icons - 原始材料")]
    [SerializeField] private Sprite claw;
    [SerializeField] private Sprite wolfHair;
    [SerializeField] private Sprite eyeball;
    [SerializeField] private Sprite purpleFlame;
    [SerializeField] private Sprite snakeDried;
    [SerializeField] private Sprite tentacle;
    [SerializeField] private Sprite coffeeBean;
    [SerializeField] private Sprite milk;
    [SerializeField] private Sprite sugar;
    [SerializeField] private Sprite water;

    [Header("Full Art - 格子里显示用")]
    [SerializeField] private Sprite clawAll;
    [SerializeField] private Sprite wolfHairAll;
    [SerializeField] private Sprite eyeballAll;
    [SerializeField] private Sprite purpleFlameAll;
    [SerializeField] private Sprite snakeDriedAll;
    [SerializeField] private Sprite tentacleAll;
    [SerializeField] private Sprite coffeeBeanAll;
    [SerializeField] private Sprite milkAll;
    [SerializeField] private Sprite sugarAll;
    [SerializeField] private Sprite waterAll;

    [Header("Icons - 机器产出材料")]
    [SerializeField] private Sprite groundCoffee;
    [SerializeField] private Sprite espresso;
    [SerializeField] private Sprite steamedMilk;
    [SerializeField] private Sprite pourOverCoffee;
    [SerializeField] private Sprite unknown;

    private void Awake()
    {
        var icons = new Dictionary<string, Sprite>
        {
            { "claw", claw },
            { "wolfHair", wolfHair },
            { "eyeball", eyeball },
            { "purpleFlame", purpleFlame },
            { "snakeDried", snakeDried },
            { "tentacle", tentacle },
            { "CoffeeBean", coffeeBean },
            { "Milk", milk },
            { "Sugar", sugar },
            { "Water", water },
            // 产出材料
            { "GroundCoffee", groundCoffee },
            { "Espresso", espresso },
            { "SteamedMilk", steamedMilk },
            { "PourOverCoffee", pourOverCoffee },
            { "Unknown", unknown },
        };

        // 产出材料没有 ALL 版本，复用 icon
        var allArt = new Dictionary<string, Sprite>
        {
            { "claw", clawAll },
            { "wolfHair", wolfHairAll },
            { "eyeball", eyeballAll },
            { "purpleFlame", purpleFlameAll },
            { "snakeDried", snakeDriedAll },
            { "tentacle", tentacleAll },
            { "CoffeeBean", coffeeBeanAll },
            { "Milk", milkAll },
            { "Sugar", sugarAll },
            { "Water", waterAll },
            // 产出材料复用 icon 作为 ALL
            { "GroundCoffee", groundCoffee },
            { "Espresso", espresso },
            { "SteamedMilk", steamedMilk },
            { "PourOverCoffee", pourOverCoffee },
            { "Unknown", unknown },
        };

        MaterialDefinition.SetSpriteCache(icons);
        MaterialDefinition.SetSpriteAllCache(allArt);
    }
}
