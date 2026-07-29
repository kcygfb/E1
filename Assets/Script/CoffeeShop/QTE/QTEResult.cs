using System;
using System.Collections.Generic;

/// <summary>
/// QTE 单次判定结果。
/// </summary>
public enum QTERating
{
    Perfect,
    Good,
    Miss
}

/// <summary>
/// 一杯咖啡所有步骤的 QTE 评分汇总。
/// 由 CraftController 在交付时填充，传递给 Rewarder 计算金币。
/// </summary>
[Serializable]
public class QTEScoreResult
{
    /// <summary>stepId → 该步骤的 QTE 评级</summary>
    public Dictionary<string, QTERating> StepResults { get; } = new();

    /// <summary>
    /// 平均倍率：Perfect=1.5x, Good=1.0x, Miss=0.5x
    /// </summary>
    public float GetMultiplier()
    {
        if (StepResults.Count == 0) return 1f;
        float sum = 0f;
        foreach (var r in StepResults.Values)
        {
            sum += r switch
            {
                QTERating.Perfect => 1.5f,
                QTERating.Good => 1.0f,
                QTERating.Miss => 0.5f,
                _ => 1f
            };
        }
        return sum / StepResults.Count;
    }

    /// <summary>
    /// 全部 Perfect 时额外 1.5x 倍乘
    /// </summary>
    public bool IsAllPerfect()
    {
        if (StepResults.Count == 0) return false;
        foreach (var r in StepResults.Values)
            if (r != QTERating.Perfect) return false;
        return true;
    }

    public void Record(string stepId, QTERating rating)
    {
        StepResults[stepId] = rating;
    }
}
