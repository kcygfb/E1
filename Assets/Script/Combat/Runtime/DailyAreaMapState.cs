using System.Collections.Generic;
using UnityEngine;

namespace KiKs.Combat
{
    public enum AreaPointType
    {
        Battle,
        Event,
        Treasure
    }

    public sealed class DailyAreaMapPoint
    {
        public AreaPointType Type { get; }
        public bool IsSelected { get; private set; }
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 敌人槽位索引（0/1/2，对应 BattleController.demoEnemyDefinitions）。
        /// 仅战斗点有效；非战斗点恒为 -1。每局开始随机分配。
        /// </summary>
        public int EncounterIndex { get; private set; } = -1;

        internal DailyAreaMapPoint(AreaPointType type)
        {
            Type = type;
        }

        internal void SetEncounterIndex(int index)
        {
            EncounterIndex = index;
        }

        internal void Select()
        {
            IsSelected = true;
        }

        internal void CancelSelection()
        {
            IsSelected = false;
        }

        internal void Complete()
        {
            IsSelected = false;
            IsCompleted = true;
        }
    }

    /// <summary>
    /// Session-lifetime state for the five area points shown during one expedition night.
    /// The layout stays stable while moving between PreBattle and the combat scene.
    /// </summary>
    public static class DailyAreaMapState
    {
        public const int PointCount = 5;
        public const int MaxExplorations = 3;

        private static readonly List<DailyAreaMapPoint> Points = new();
        private static int selectedPointIndex = -1;

        public static IReadOnlyList<DailyAreaMapPoint> MapPoints => Points;
        public static bool HasSelectedPoint => selectedPointIndex >= 0;
        public static int SelectedPointIndex => selectedPointIndex;
        public static int CompletedExplorationCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            Reset();
        }

        public static void EnsureGenerated()
        {
            if (Points.Count == PointCount)
                return;

            Points.Clear();
            selectedPointIndex = -1;
            CompletedExplorationCount = 0;

            var typeBag = new List<AreaPointType>
            {
                AreaPointType.Battle,
                AreaPointType.Battle,
                AreaPointType.Battle,
                AreaPointType.Event,
                AreaPointType.Treasure
            };

            for (var i = typeBag.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (typeBag[i], typeBag[swapIndex]) = (typeBag[swapIndex], typeBag[i]);
            }

            foreach (var type in typeBag)
                Points.Add(new DailyAreaMapPoint(type));

            AssignBattleEncounters();
        }

        /// <summary>
        /// 把 3 个敌人（槽位 0/1/2）随机均匀分配给 3 个战斗点：每局重开时分配不同。
        /// </summary>
        private static void AssignBattleEncounters()
        {
            var order = new List<int> { 0, 1, 2 };
            for (var i = order.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
            }

            var battleIndex = 0;
            foreach (var point in Points)
            {
                if (point.Type != AreaPointType.Battle)
                    continue;

                point.SetEncounterIndex(order[battleIndex]);
                battleIndex++;
            }
        }

        public static bool TryGetPoint(int pointIndex, out DailyAreaMapPoint point)
        {
            EnsureGenerated();
            if (pointIndex < 0 || pointIndex >= Points.Count)
            {
                point = null;
                return false;
            }

            point = Points[pointIndex];
            return true;
        }

        public static bool TrySelectPoint(int pointIndex, out string failureReason)
        {
            EnsureGenerated();

            if (!TryGetPoint(pointIndex, out var point))
            {
                failureReason = "Invalid map point.";
                return false;
            }

            if (CompletedExplorationCount >= MaxExplorations)
            {
                failureReason = "All exploration chances have been used.";
                return false;
            }

            if (point.IsCompleted)
            {
                failureReason = "This area has already been completed.";
                return false;
            }

            if (HasSelectedPoint)
            {
                if (selectedPointIndex == pointIndex)
                {
                    failureReason = "This area is already selected.";
                    return false;
                }

                Points[selectedPointIndex].CancelSelection();
            }

            point.Select();
            selectedPointIndex = pointIndex;
            failureReason = string.Empty;
            return true;
        }

        public static void CompleteSelectedPoint()
        {
            CompleteSelectedPoint(countExploration: true);
        }

        /// <summary>
        /// Completes and hides the selected point without advancing the current three-battle
        /// playtest counter. Treasure uses this temporary path until the unified daily
        /// exploration/save flow replaces the battle-only demo progression.
        /// </summary>
        public static void CompleteSelectedPointWithoutCountingExploration()
        {
            CompleteSelectedPoint(countExploration: false);
        }

        private static void CompleteSelectedPoint(bool countExploration)
        {
            if (!HasSelectedPoint || !TryGetPoint(selectedPointIndex, out var point))
                return;

            point.Complete();
            selectedPointIndex = -1;
            if (countExploration)
                CompletedExplorationCount++;
        }

        public static void CancelSelectedPoint()
        {
            if (!HasSelectedPoint || !TryGetPoint(selectedPointIndex, out var point))
                return;

            point.CancelSelection();
            selectedPointIndex = -1;
        }

        public static void Reset()
        {
            Points.Clear();
            selectedPointIndex = -1;
            CompletedExplorationCount = 0;
        }
    }
}
