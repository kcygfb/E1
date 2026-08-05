using System.Collections.Generic;
using UnityEngine;

/// <summary>材料图标缓存。在场景中挂此组件，Inspector 里分配 10 种材料的 Sprite，
/// 运行时填充 MaterialDefinition 的静态缓存供 TrayGridUI / MaterialPalette 使用。
/// sprites = 拖拽/列表用的单个图标；spritesAll = 格子里显示的整图。</summary>
public class MaterialSpriteCache : MonoBehaviour
{
    [Header("Icons (拖拽/列表用)")]
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

    [Header("Full Art (格子里显示用 *ALL)")]
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
        };

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
        };

        MaterialDefinition.SetSpriteCache(icons);
        MaterialDefinition.SetSpriteAllCache(allArt);
    }
}
