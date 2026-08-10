using System;
using System.Linq;

/// <summary>九宫格小料台静态数据。存储玩家在 MorningCheck 阶段选定的 9 格材料。</summary>
public static class IngredientTray
{
    /// <summary>9 格选材，null/空 = 未选。</summary>
    public static readonly string[] Slots = new string[9];

    /// <summary>是否有至少 1 格已选材（开始营业的最低条件）。</summary>
    public static bool HasAny => Slots.Any(s => !string.IsNullOrEmpty(s));

    /// <summary>可选材料 ID 列表（10 种）。</summary>
    public static readonly string[] SelectableMaterials =
    {
        "claw",
        "wolffur",
        "eye",
        "fire",
        "oil",
        "snake",
        "tentacle",
        "CoffeeBean",
        "Milk",
        "Sugar",
        "Water",
    };

    /// <summary>设置某格的材料。</summary>
    public static void SetSlot(int index, string materialId)
    {
        if (index < 0 || index >= Slots.Length) return;
        Slots[index] = materialId;
    }

    /// <summary>获取某格的材料 ID（可能为 null）。</summary>
    public static string GetSlot(int index)
    {
        if (index < 0 || index >= Slots.Length) return null;
        return Slots[index];
    }

    /// <summary>清空所有格子。</summary>
    public static void Clear()
    {
        Array.Fill(Slots, null);
    }

    /// <summary>默认预设格子（Milk/Sugar/Water/CoffeeBean 已放入前 4 格）。仅在全新一局时调用。</summary>
    public static void SetDefaults()
    {
        Clear();
        Slots[0] = "Milk";
        Slots[1] = "Sugar";
        Slots[2] = "Water";
        Slots[3] = "CoffeeBean";
    }

    /// <summary>每天开始时调用：仅当所有格子都为空时才填充默认值，保留玩家前一天的选择。</summary>
    public static void SetDefaultsIfEmpty()
    {
        if (HasAny) return;
        SetDefaults();
    }
}
