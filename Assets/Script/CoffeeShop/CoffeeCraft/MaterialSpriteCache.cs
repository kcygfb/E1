using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>材料图标缓存。在场景中挂此组件，Inspector 里分配材料 Sprite，
/// 运行时填充 MaterialDefinition 的静态缓存供 UI 使用。</summary>
public class MaterialSpriteCache : MonoBehaviour
{
    [Header("Icons - 原始材料")]
    [SerializeField] private Sprite claw;
    [FormerlySerializedAs("wolfHair")] [SerializeField] private Sprite wolffur;
    [FormerlySerializedAs("eyeball")] [SerializeField] private Sprite eye;
    [FormerlySerializedAs("purpleFlame")] [SerializeField] private Sprite fire;
    [SerializeField] private Sprite oil;
    [FormerlySerializedAs("snakeDried")] [SerializeField] private Sprite snake;
    [SerializeField] private Sprite tentacle;
    [SerializeField] private Sprite coffeeBean;
    [SerializeField] private Sprite milk;
    [SerializeField] private Sprite sugar;
    [SerializeField] private Sprite water;

    [Header("Full Art - 格子里显示用")]
    [SerializeField] private Sprite clawAll;
    [FormerlySerializedAs("wolfHairAll")] [SerializeField] private Sprite wolffurAll;
    [FormerlySerializedAs("eyeballAll")] [SerializeField] private Sprite eyeAll;
    [FormerlySerializedAs("purpleFlameAll")] [SerializeField] private Sprite fireAll;
    [SerializeField] private Sprite oilAll;
    [FormerlySerializedAs("snakeDriedAll")] [SerializeField] private Sprite snakeAll;
    [SerializeField] private Sprite tentacleAll;
    [SerializeField] private Sprite coffeeBeanAll;
    [SerializeField] private Sprite milkAll;
    [SerializeField] private Sprite sugarAll;
    [SerializeField] private Sprite waterAll;

    [Header("Icons - 通用机器产出")]
    [SerializeField] private Sprite groundCoffee;
    [SerializeField] private Sprite espresso;
    [SerializeField] private Sprite steamedMilk;
    [SerializeField] private Sprite pourOverCoffee;
    [SerializeField] private Sprite unknown;

    [Header("Icons - 磨粉产物 (Grinder)")]
    [SerializeField] private Sprite clawPowder;
    [SerializeField] private Sprite eyePowder;
    [SerializeField] private Sprite firePowder;
    [SerializeField] private Sprite oilPowder;
    [SerializeField] private Sprite snakePowder;
    [SerializeField] private Sprite tentaclePowder;
    [SerializeField] private Sprite wolffurPowder;

    [Header("Icons - 萃取液产物 (Extractor)")]
    [SerializeField] private Sprite clawEspresso;
    [SerializeField] private Sprite eyeEspresso;
    [SerializeField] private Sprite fireEspresso;
    [SerializeField] private Sprite oilEspresso;
    [SerializeField] private Sprite snakeEspresso;
    [SerializeField] private Sprite tentacleEspresso;
    [SerializeField] private Sprite wolffurEspresso;
    [SerializeField] private Sprite ESMEspresso;

    [Header("Icons - 手冲球产物 (PourOver)")]
    [SerializeField] private Sprite clawBall;
    [SerializeField] private Sprite eyeBall;
    [SerializeField] private Sprite fireBall;
    [SerializeField] private Sprite oilBall;
    [SerializeField] private Sprite snakeBall;
    [SerializeField] private Sprite tentacleBall;
    [SerializeField] private Sprite wolffurBall;
    [SerializeField] private Sprite mystry1;

    private void Awake()
    {
        var icons = new Dictionary<string, Sprite>
        {
            { "claw", claw },
            { "wolffur", wolffur },
            { "eye", eye },
            { "fire", fire },
            { "oil", oil },
            { "snake", snake },
            { "tentacle", tentacle },
            { "CoffeeBean", coffeeBean },
            { "Milk", milk },
            { "Sugar", sugar },
            { "Water", water },
            // 通用产出
            { "GroundCoffee", groundCoffee },
            { "Espresso", espresso },
            { "SteamedMilk", steamedMilk },
            { "PourOverCoffee", pourOverCoffee },
            { "Unknown", unknown },
            // 磨粉产物
            { "clawPowder", clawPowder },
            { "eyePowder", eyePowder },
            { "firePowder", firePowder },
            { "oilPowder", oilPowder },
            { "snakePowder", snakePowder },
            { "tentaclePowder", tentaclePowder },
            { "wolffurPowder", wolffurPowder },
            // 萃取液产物
            { "clawEspresso", clawEspresso },
            { "eyeEspresso", eyeEspresso },
            { "fireEspresso", fireEspresso },
            { "oilEspresso", oilEspresso },
            { "snakeEspresso", snakeEspresso },
            { "tentacleEspresso", tentacleEspresso },
            { "wolffurEspresso", wolffurEspresso },
            { "ESMEspresso", ESMEspresso },
            // 手冲球产物
            { "clawBall", clawBall },
            { "eyeBall", eyeBall },
            { "fireBall", fireBall },
            { "oilBall", oilBall },
            { "snakeBall", snakeBall },
            { "tentacleBall", tentacleBall },
            { "wolffurBall", wolffurBall },
            { "mystry1", mystry1 },
        };

        // 产出材料没有 ALL 版本，复用 icon
        var allArt = new Dictionary<string, Sprite>
        {
            { "claw", clawAll },
            { "wolffur", wolffurAll },
            { "eye", eyeAll },
            { "fire", fireAll },
            { "oil", oilAll },
            { "snake", snakeAll },
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
            // 磨粉产物
            { "clawPowder", clawPowder },
            { "eyePowder", eyePowder },
            { "firePowder", firePowder },
            { "oilPowder", oilPowder },
            { "snakePowder", snakePowder },
            { "tentaclePowder", tentaclePowder },
            { "wolffurPowder", wolffurPowder },
            // 萃取液产物
            { "clawEspresso", clawEspresso },
            { "eyeEspresso", eyeEspresso },
            { "fireEspresso", fireEspresso },
            { "oilEspresso", oilEspresso },
            { "snakeEspresso", snakeEspresso },
            { "tentacleEspresso", tentacleEspresso },
            { "wolffurEspresso", wolffurEspresso },
            { "ESMEspresso", ESMEspresso },
            // 手冲球产物
            { "clawBall", clawBall },
            { "eyeBall", eyeBall },
            { "fireBall", fireBall },
            { "oilBall", oilBall },
            { "snakeBall", snakeBall },
            { "tentacleBall", tentacleBall },
            { "wolffurBall", wolffurBall },
            { "mystry1", mystry1 },
        };

        MaterialDefinition.SetSpriteCache(icons);
        MaterialDefinition.SetSpriteAllCache(allArt);
    }
}
