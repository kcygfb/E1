using UnityEngine;
using TMPro;

namespace KiKs.Core
{
    public class DayDisplayUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private string format = "DAY: {0}";

        private void OnEnable()
        {
            EventBus.DayStarted += OnDayStarted;
        }

        private void OnDisable()
        {
            EventBus.DayStarted -= OnDayStarted;
        }

        private void Start()
        {
            var ts = FindFirstObjectByType<TimeSystem>();
            if (ts != null)
                UpdateText(ts.dayCount);
        }

        private void OnDayStarted(int day)
        {
            UpdateText(day);
        }

        private void UpdateText(int day)
        {
            if (displayText != null)
                displayText.text = string.Format(format, day);
        }
    }
}
