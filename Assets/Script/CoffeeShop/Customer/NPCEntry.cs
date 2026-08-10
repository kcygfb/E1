using System;
using UnityEngine;

/// <summary>
/// 点单方式。
/// </summary>
public enum OrderMode
{
    /// <summary>指定咖啡ID（填 coffeeId 字段）</summary>
    SpecificCoffee,
    /// <summary>从当前已解锁/能做的咖啡中随机选</summary>
    RandomUnlocked,
    /// <summary>接受任何咖啡</summary>
    AcceptAny
}

/// <summary>
/// 描述一个 NPC 在某天的具体出现：对话和点单。
/// 内联在 DayConfig 里，不是独立 asset。
/// </summary>
[Serializable]
public class NPCEntry
{
    [Tooltip("NPC 角色身份")]
    public NPCData npc;

    [Tooltip("对话键。代码自动拼接后缀: key_arrival / key_departure / key_startofday / key_endofday。留空=用 npc.npcName 小写")]
    public string dialogueKey;

    [Tooltip("点单方式")]
    public OrderMode orderMode = OrderMode.RandomUnlocked;

    [Tooltip("orderMode=SpecificCoffee 时填写咖啡ID")]
    public string coffeeId;

    /// <summary>获取指定阶段的对话ID。找不到时返回空字符串（调用方跳过对话）。</summary>
    public string GetDialogueId(string phase)
    {
        if (npc == null) return string.Empty;
        string key = string.IsNullOrEmpty(dialogueKey)
            ? npc.npcName?.ToLowerInvariant() ?? string.Empty
            : dialogueKey;
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return $"{key}_{phase}";
    }
}
