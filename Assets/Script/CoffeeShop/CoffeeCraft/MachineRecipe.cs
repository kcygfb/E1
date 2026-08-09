using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>单条机器配方：机器ID + 输入材料 = 输出材料</summary>
[Serializable]
public class MachineRecipe
{
    public string machineId;
    public string inputMaterialId;
    public string outputMaterialId;
}

/// <summary>多输入机器配方：机器ID + 多个输入材料(集合匹配) = 输出材料</summary>
[Serializable]
public class MultiMachineRecipe
{
    public string machineId;
    public List<string> inputMaterialIds;
    public string outputMaterialId;
}

/// <summary>机器配方库。支持单输入和多输入配方查找。</summary>
public static class MachineRecipeLibrary
{
    private static readonly Dictionary<string, string> _recipes = new();
    private static readonly List<MultiMachineRecipe> _multiRecipes = new();

    static MachineRecipeLibrary()
    {
        // === 通用配方 ===
        Register("Grinder", "CoffeeBean", "GroundCoffee");
        Register("Extractor", "GroundCoffee", "Espresso");
        Register("Steamer", "Milk", "SteamedMilk");
        RegisterMulti("PourOver", new[] { "GroundCoffee", "Water" }, "PourOverCoffee");

        // === 7种特殊材料磨粉配方（Grinder: 原材料 → 粉） ===
        Register("Grinder", "claw", "clawPowder");
        Register("Grinder", "eye", "eyePowder");
        Register("Grinder", "fire", "firePowder");
        Register("Grinder", "oil", "oilPowder");
        Register("Grinder", "snake", "snakePowder");
        Register("Grinder", "tentacle", "tentaclePowder");
        Register("Grinder", "wolffur", "wolffurPowder");

        // === 7种特殊材料萃取配方（Extractor: 粉 → 萃取液） ===
        Register("Extractor", "clawPowder", "clawEspresso");
        Register("Extractor", "eyePowder", "eyeEspresso");
        Register("Extractor", "firePowder", "fireEspresso");
        Register("Extractor", "oilPowder", "oilEspresso");
        Register("Extractor", "snakePowder", "snakeEspresso");
        Register("Extractor", "tentaclePowder", "tentacleEspresso");
        // wolffur 用多输入：wolffurPowder + GroundCoffee → wolffurEspresso
        RegisterMulti("Extractor", new[] { "wolffurPowder", "GroundCoffee" }, "wolffurEspresso");
        // ESM：eyePowder + snakePowder → ESMEspresso
        RegisterMulti("Extractor", new[] { "eyePowder", "snakePowder" }, "ESMEspresso");

        // === 7种特殊材料手冲配方（PourOver: 粉 + 水 → 手冲球） ===
        RegisterMulti("PourOver", new[] { "clawPowder", "Water" }, "clawBall");
        RegisterMulti("PourOver", new[] { "eyePowder", "Water" }, "eyeBall");
        RegisterMulti("PourOver", new[] { "firePowder", "Water" }, "fireBall");
        RegisterMulti("PourOver", new[] { "oilPowder", "Water" }, "oilBall");
        RegisterMulti("PourOver", new[] { "snakePowder", "Water" }, "snakeBall");
        RegisterMulti("PourOver", new[] { "tentaclePowder", "Water" }, "tentacleBall");
        RegisterMulti("PourOver", new[] { "wolffurPowder", "Water" }, "wolffurBall");
    }

    // === 单输入配方 ===
    public static void Register(string machineId, string input, string output)
    {
        _recipes[Key(machineId, input)] = output;
    }

    public static string GetOutput(string machineId, string inputMaterialId)
    {
        if (string.IsNullOrEmpty(machineId) || string.IsNullOrEmpty(inputMaterialId)) return null;
        return _recipes.TryGetValue(Key(machineId, inputMaterialId), out var output) ? output : null;
    }

    public static bool TryGetOutput(string machineId, string inputMaterialId, out string output)
    {
        return _recipes.TryGetValue(Key(machineId, inputMaterialId), out output);
    }

    // === 多输入配方 ===
    public static void RegisterMulti(string machineId, string[] inputs, string output)
    {
        _multiRecipes.Add(new MultiMachineRecipe
        {
            machineId = machineId,
            inputMaterialIds = inputs.ToList(),
            outputMaterialId = output
        });
    }

    /// <summary>用材料集合查找多输入配方。SetEquals 匹配，顺序无关。</summary>
    public static bool TryGetOutputMulti(string machineId, List<string> inputs, out string output)
    {
        output = null;
        if (string.IsNullOrEmpty(machineId) || inputs == null || inputs.Count == 0) return false;

        var inputSet = new HashSet<string>(inputs);
        foreach (var recipe in _multiRecipes)
        {
            if (recipe.machineId != machineId) continue;
            if (recipe.inputMaterialIds == null || recipe.inputMaterialIds.Count == 0) continue;
            var recipeSet = new HashSet<string>(recipe.inputMaterialIds);
            if (inputSet.SetEquals(recipeSet))
            {
                output = recipe.outputMaterialId;
                return true;
            }
        }
        return false;
    }

    private static string Key(string machineId, string materialId) => machineId + "|" + materialId;
}
