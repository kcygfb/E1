using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text speakerText;
    public Text lineText;
    public Button nextButton;

    private DialogueData currentDialogue;
    private int currentIndex;
    private Action<DialogueTrigger> onTriggerCallback;
    private Action onCompleteCallback;
    private bool isRunning;
    private Dictionary<string, string> tokens = new();

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    public bool IsRunning => isRunning;

    private string speakerOverride;

    public void StartDialogue(
        DialogueData dialogue,
        Action<DialogueTrigger> onTrigger,
        Action onComplete,
        Dictionary<string, string> tokens = null,
        string speakerOverride = null
    )
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            onTrigger?.Invoke(DialogueTrigger.None);
            onComplete?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        currentIndex = 0;
        onTriggerCallback = onTrigger;
        onCompleteCallback = onComplete;
        isRunning = true;
        this.tokens = tokens ?? new();
        this.speakerOverride = speakerOverride;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        if (currentDialogue == null || index < 0 || index >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        currentIndex = index;
        DialogueLine line = currentDialogue.lines[index];

        if (lineText != null)
        {
            string text = line.text;
            foreach (var kvp in tokens)
                text = text.Replace($"{{{kvp.Key}}}", kvp.Value);

            if (speakerText != null)
            {
                speakerText.text = "";
                string speaker = !string.IsNullOrEmpty(speakerOverride) ? speakerOverride : line.speaker;
                lineText.text = $"{speaker}：{text}";
            }
            else
            {
                lineText.text = text;
            }
        }

        if (line.trigger != DialogueTrigger.None)
            onTriggerCallback?.Invoke(line.trigger);
    }

    private void OnNextClicked()
    {
        if (!isRunning || currentDialogue == null)
            return;

        Debug.Log($"[DialogueManager] OnNextClicked called, currentIndex={currentIndex}, totalLines={currentDialogue.lines.Length}");

        int next = currentIndex + 1;
        if (next >= currentDialogue.lines.Length)
            EndDialogue();
        else
            ShowLine(next);
    }

    private void EndDialogue()
    {
        isRunning = false;
        currentDialogue = null;
        currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        var cb = onCompleteCallback;
        onCompleteCallback = null;
        cb?.Invoke();
    }
}
