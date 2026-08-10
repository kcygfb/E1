using UnityEngine;
using TMPro;

public class DayDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private string format = "DAY: {0}";

    private void OnEnable()
    {
        GameEvent.On("DayStarted", OnDayStarted);
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        KiKs.Combat.RuntimeGameRepository.DayChanged += OnRepositoryDayChanged;
        RefreshFromRepository();
    }

    private void OnDisable()
    {
        GameEvent.Off("DayStarted", OnDayStarted);
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        KiKs.Combat.RuntimeGameRepository.DayChanged -= OnRepositoryDayChanged;
    }

    private void Start()
    {
        RefreshFromRepository();
    }

    private void OnDayStarted(object payload)
    {
        RefreshFromRepository();
    }

    private void OnRepositoryDayChanged(int day)
    {
        UpdateText(day);
    }

    private void RefreshFromRepository()
    {
        UpdateText(KiKs.Combat.RuntimeGameRepository.CurrentDay);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is PhaseChangedPayload p && p.Phase == DayPhase.MorningCheck)
            UpdateText(p.Day);
    }

    private void UpdateText(int day)
    {
        if (displayText != null)
            displayText.text = string.Format(format, day);
    }
}
