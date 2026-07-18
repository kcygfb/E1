using UnityEngine;
using UnityEngine.UI;

public class PhaseDisplayUI : MonoBehaviour
{
    [SerializeField] private Text displayText;

    private bool dialogueActive;
    private DayPhase currentPhase = DayPhase.MorningCheck;

    private void OnEnable()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        GameEvent.On("CraftViewChanged", OnCraftViewChanged);
        GameEvent.On("DialogueRequested", OnDialogueRequested);
        GameEvent.On("DialogueEnded", OnDialogueEnded);
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        GameEvent.Off("CraftViewChanged", OnCraftViewChanged);
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
        GameEvent.Off("DialogueEnded", OnDialogueEnded);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        currentPhase = p.Phase;

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
        UpdateVisibility();
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

    private void OnDialogueRequested(object payload)
    {
        dialogueActive = true;
        UpdateVisibility();
    }

    private void OnDialogueEnded(object payload)
    {
        dialogueActive = false;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool shouldShow = currentPhase != DayPhase.Shop || !dialogueActive;
        if (displayText != null)
            displayText.enabled = shouldShow;
    }

    private void SetText(string text)
    {
        if (displayText != null)
            displayText.text = text;
    }
}
