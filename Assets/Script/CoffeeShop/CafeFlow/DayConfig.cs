using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一天的咖啡店配置。只建剧情天，无 DayConfig 的天=纯随机日。
/// 三阶段 NPC 列表统一在此配置：startOfDay / customers / endOfDay。
/// </summary>
[CreateAssetMenu(fileName = "DayConfig", menuName = "Cafe/DayConfig")]
public class DayConfig : ScriptableObject
{
    [Tooltip("天数（1-based）")]
    public int day;

    [Header("开场（空=跳过开场直接进选材）")]
    [Tooltip("经营前出场的 NPC，依次播对话后离开")]
    public List<NPCEntry> startOfDay = new();

    [Header("经营顾客（空=随机刷通用顾客）")]
    [Tooltip("经营阶段的顾客列表，按顺序出场")]
    public List<NPCEntry> customers = new();

    [Header("收尾（空=跳过收尾直接进结算）")]
    [Tooltip("所有顾客离开后出场的 NPC，依次播对话后离开")]
    public List<NPCEntry> endOfDay = new();
}
