using System;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Session-lifetime progress for the three-battle playtest demo.
    /// Static state survives scene changes and is reset when a new play session starts.
    /// </summary>
    public static class DemoFlowState
    {
        public const int BattleCount = 3;
        private static int _currentBattleIndex;

        public static int CurrentBattleIndex => _currentBattleIndex;
        public static int CurrentDay => Math.Min(_currentBattleIndex + 1, BattleCount);
        public static DemoStage CurrentStage => (DemoStage)_currentBattleIndex;
        public static bool IsCompleted => _currentBattleIndex >= BattleCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            _currentBattleIndex = 0;
            RuntimeGameRepository.ResetRunState();
            DailyAreaMapState.Reset();
        }

        public static bool IsStageAvailable(DemoStage stage)
        {
            return !IsCompleted && stage == CurrentStage;
        }

        public static bool CompleteCurrentBattle(DemoStage completedStage)
        {
            if (!IsStageAvailable(completedStage))
            {
                Debug.LogError(
                    $"[DemoFlow] Cannot complete {completedStage}; current stage is {CurrentStage} " +
                    $"(index {_currentBattleIndex}).");
                return false;
            }

            _currentBattleIndex++;
            if (IsCompleted)
                Debug.Log("[DemoFlow] Demo Complete. All three battles were cleared in order.");
            else
                Debug.Log($"[DemoFlow] Advanced to day {CurrentDay}: {CurrentStage}.");

            return true;
        }

        public static void ResetDemoProgress()
        {
            _currentBattleIndex = 0;
            RuntimeGameRepository.ResetRunState();
            DailyAreaMapState.Reset();
            Debug.Log("[DemoFlow] Progress reset to day 1 / DogBattle.");
        }
    }
}
