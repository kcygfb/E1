namespace KiKs.Combat
{
    /// <summary>
    /// Owns the atomic connection between event progression and the nightly area map.
    /// Event UI code should never load the return scene without going through this class.
    /// </summary>
    public static class EventAreaCompletion
    {
        public static AreaCompletionResult CompleteCurrentEvent()
        {
            if (!DailyAreaMapState.HasSelectedPoint ||
                !DailyAreaMapState.TryGetPoint(DailyAreaMapState.SelectedPointIndex, out var selectedPoint) ||
                selectedPoint.Type != AreaPointType.Event)
            {
                return new AreaCompletionResult(
                    completed: false,
                    dayAdvanced: false,
                    currentDay: RuntimeGameRepository.CurrentDay,
                    nextSceneName: "PreBattle");
            }

            var currentEvent = EventSelectionState.CurrentEvent;
            var completion = RuntimeGameRepository.CompleteSelectedArea(defeated: false);
            if (!completion.Completed)
                return completion;

            if (currentEvent != null)
                EventSelectionState.MarkEventCompleted(currentEvent.id);
            EventSelectionState.ClearCurrent();
            return completion;
        }

        public static void AbortCurrentEvent()
        {
            DailyAreaMapState.CancelSelectedPoint();
            EventSelectionState.ClearCurrent();
        }
    }
}