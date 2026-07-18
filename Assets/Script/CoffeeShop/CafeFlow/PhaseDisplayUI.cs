using UnityEngine;
using UnityEngine.UI;

public class PhaseDisplayUI : MonoBehaviour
{
    [SerializeField] private Text displayText;

    private void OnEnable()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        GameEvent.On("CraftViewChanged", OnCraftViewChanged);
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        GameEvent.Off("CraftViewChanged", OnCraftViewChanged);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;

        switch (p.Phase)
        {
            case DayPhase.MorningCheck:
                SetText("MorningCheck");
                break;
            case DayPhase.Shop:
                SetText("Menu");
                break;
            case DayPhase.Night:
                SetText("Night");
                break;
        }
    }

    private void OnCraftViewChanged(object payload)
    {
        if (payload is string view)
        {
            if (view == "CoffeeMake")
                SetText("CoffeeMake");
            else if (view == "Menu")
                SetText("Menu");
        }
    }

    private void SetText(string text)
    {
        if (displayText != null)
            displayText.text = text;
    }
}
