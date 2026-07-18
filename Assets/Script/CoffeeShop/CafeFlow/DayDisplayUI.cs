using UnityEngine;
using TMPro;

public class DayDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string format = "DAY: {0}";

    private void OnEnable()
    {
        GameEvent.On("DayStarted", OnDayStarted);
    }

    private void OnDisable()
    {
        GameEvent.Off("DayStarted", OnDayStarted);
    }

    private void Start()
    {
        var ts = FindFirstObjectByType<TimeSystem>();
        if (ts != null) UpdateText(ts.dayCount);
    }

    private void OnDayStarted(object payload)
    {
        if (payload is int day) UpdateText(day);
    }

    private void UpdateText(int day)
    {
        if (displayText != null)
            displayText.text = string.Format(format, day);
    }
}
