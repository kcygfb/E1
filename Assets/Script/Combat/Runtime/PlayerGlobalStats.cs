namespace KiKs.Combat
{
    /// <summary>
    /// 跨场景玩家全局状态（HP）。BattleController 在战斗开始/结束时间步。
    /// </summary>
    public static class PlayerGlobalStats
    {
        public static int MaxHealth { get; private set; } = 100;
        public static int CurrentHealth { get; private set; } = 100;

        public static void SetHealth(int current, int max)
        {
            MaxHealth = max > 0 ? max : 100;
            CurrentHealth = UnityEngine.Mathf.Clamp(current, 0, MaxHealth);
        }

        public static void ResetToFull(int max = 0)
        {
            if (max > 0) MaxHealth = max;
            CurrentHealth = MaxHealth;
        }
    }
}
