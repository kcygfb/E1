using System;
using System.Collections.Generic;

/// <summary>单条机器配方：机器ID + 输入材料 = 输出材料</summary>
[Serializable]
public class MachineRecipe
{
    public string machineId;
    public string inputMaterialId;
    public string outputMaterialId;
}

/// <summary>机器配方库。静态查找 (machineId, materialId) → outputMaterialId。</summary>
public static class MachineRecipeLibrary
{
    private static readonly Dictionary<string, string> _recipes = new();

    static MachineRecipeLibrary()
    {
        // 硬编码配方
        Register("Grinder", "CoffeeBean", "GroundCoffee");
        Register("Extractor", "GroundCoffee", "Espresso");
        Register("Steamer", "Milk", "SteamedMilk");
        Register("PourOver", "GroundCoffee", "PourOverCoffee");
    }

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

    private static string Key(string machineId, string materialId) => machineId + "|" + materialId;
}
