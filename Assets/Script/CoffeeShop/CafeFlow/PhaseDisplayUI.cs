using UnityEngine;
using UnityEngine.UI;

public class PhaseDisplayUI : MonoBehaviour
{
    [SerializeField] private Text displayText;

    private bool dialogueActive;
    private bool hasOrder;
    private DayPhase currentPhase = DayPhase.MorningCheck;

    private void OnEnable()
    {
        GameEvent.On("PhaseChanged", OnPhaseChanged);
        GameEvent.On("CraftViewChanged", OnCraftViewChanged);
        GameEvent.On("DialogueRequested", OnDialogueRequested);
        GameEvent.On("DialogueEnded", OnDialogueEnded);
        GameEvent.On("OrderCreated", OnOrderCreated);
        GameEvent.On("OrderCompleted", OnOrderCompleted);
    }

    private void OnDisable()
    {
        GameEvent.Off("PhaseChanged", OnPhaseChanged);
        GameEvent.Off("CraftViewChanged", OnCraftViewChanged);
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
        GameEvent.Off("DialogueEnded", OnDialogueEnded);
        GameEvent.Off("OrderCreated", OnOrderCreated);
        GameEvent.Off("OrderCompleted", OnOrderCompleted);
    }

    private void OnPhaseChanged(object payload)
    {
        if (payload is not PhaseChangedPayload p) return;
        currentPhase = p.Phase;
        hasOrder = false; // 新阶段重置

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

    private void OnOrderCreated(object payload)
    {
        hasOrder = true;
        UpdateVisibility();
    }

    private void OnOrderCompleted(object payload)
    {
        hasOrder = false;
        UpdateVisibility();
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
        // Shop阶段：有订单才显示Menu文本
        if (currentPhase == DayPhase.Shop)
        {
            bool shouldShow = hasOrder && !dialogueActive;
            if (displayText != null)
                displayText.enabled = shouldShow;
        }
        else
        {
            if (displayText != null)
                displayText.enabled = !dialogueActive;
        }
    }

    private void SetText(string text)
    {
        if (displayText != null)
            displayText.text = text;
    }
}
