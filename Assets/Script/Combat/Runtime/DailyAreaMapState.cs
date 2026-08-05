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

        internal DailyAreaMapPoint(AreaPointType type)
        {
            Type = type;
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
            if (!HasSelectedPoint || !TryGetPoint(selectedPointIndex, out var point))
                return;

            point.Complete();
            selectedPointIndex = -1;
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
