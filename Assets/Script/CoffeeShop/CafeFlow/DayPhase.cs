public enum DayPhase
{
    StartOfDay,      // 开场对话（无配置则自动跳过到 MorningCheck）
    MorningCheck,    // 选材阶段
    Shop,            // 经营阶段
    EndOfDay,        // 收尾对话（无配置则自动跳过到 Settlement）
    Settlement,      // 结算阶段
    Night            // 夜晚转场
}
