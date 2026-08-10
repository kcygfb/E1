namespace KiKs.Combat

{
    /// <summary>
    /// The single public boundary for starting from a clean run state.
    /// Feature code must reset through this class instead of clearing repositories independently.
    /// </summary>
    public static class GameRunLifecycle
    {
        private static bool _resetInProgress;


        public static void ResetForNewGame()
        {
            if (_resetInProgress)
                return;

            _resetInProgress = true;
            try
            {
                DemoFlowState.ResetProgress();
                RuntimeGameRepository.ClearRunState();
            }
            finally
            {
                _resetInProgress = false;
            }
        }
    }
}
