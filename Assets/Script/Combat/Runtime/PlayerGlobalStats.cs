namespace KiKs.Combat
{
    /// <summary>
    /// 当天跨场景玩家生命。战斗开始时读取，战斗过程中及离开场景前写回。
    /// </summary>
    public static class PlayerGlobalStats
    {
        private static bool isInitialized;

        public static int MaxHealth { get; private set; } = 100;
        public static int CurrentHealth { get; private set; } = 100;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            MaxHealth = 100;
            CurrentHealth = 100;
            isInitialized = false;
        }

        public static int PrepareForBattle(int maxHealth)
        {
            if (maxHealth <= 0) throw new System.ArgumentOutOfRangeException(nameof(maxHealth));

            if (!isInitialized)
                ResetToFull(maxHealth);
            else if (MaxHealth != maxHealth)
                SetHealth(CurrentHealth, maxHealth);

            return CurrentHealth;
        }

        public static void SetHealth(int current, int max)
        {
            MaxHealth = max > 0 ? max : 100;
            CurrentHealth = UnityEngine.Mathf.Clamp(current, 0, MaxHealth);
            isInitialized = true;
        }

        public static void ResetToFull(int max = 0)
        {
            if (max > 0) MaxHealth = max;
            CurrentHealth = MaxHealth;
            isInitialized = true;
        }

        public static void RestoreAfterDefeat()
        {
            var restoredHealth = UnityEngine.Mathf.Max(1, (MaxHealth + 1) / 2);
            SetHealth(restoredHealth, MaxHealth);
        }
    }
}
