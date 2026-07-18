using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePlayer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text speakerText;
    public Text lineText;
    public Button nextButton;

    private DialogueDataJson currentDialogue;
    private int currentIndex;
    private bool isRunning;
    private Dictionary<string, string> tokens = new();
    private string speakerOverride;
    private string currentContext;

    public bool IsRunning => isRunning;

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

    private void OnEnable()
    {
        GameEvent.On("DialogueRequested", OnDialogueRequested);
    }

    private void OnDisable()
    {
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
    }

    private void OnDialogueRequested(object payload)
    {
        if (payload is not DialogueRequest req) return;
        StartDialogue(req.DialogueId, req.Context, req.Tokens, req.SpeakerOverride);
    }

    public void StartDialogue(string dialogueId, string context,
        Dictionary<string, string> tokens = null, string speakerOverride = null)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            GameEvent.Emit("DialogueEnded", context);
            return;
        }

        if (DialogueRepository.Instance == null || !DialogueRepository.Instance.IsLoaded)
        {
            Debug.LogError("[DialoguePlayer] DialogueRepository is not loaded.");
            GameEvent.Emit("DialogueEnded", context);
            return;
        }

        currentDialogue = DialogueRepository.Instance.GetDialogue(dialogueId);
        if (currentDialogue == null || currentDialogue.lines == null || currentDialogue.lines.Count == 0)
        {
            Debug.LogWarning("[DialoguePlayer] Dialogue not found or empty: " + dialogueId);
            GameEvent.Emit("DialogueEnded", context);
            return;
        }

        currentIndex = 0;
        isRunning = true;
        this.tokens = tokens ?? new();
        this.speakerOverride = speakerOverride;
        currentContext = context;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowLine(0);
    }

    private void ShowLine(int index)
    {
        if (currentDialogue == null || index < 0 || index >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        currentIndex = index;
        DialogueLineJson line = currentDialogue.lines[index];

        if (lineText != null)
        {
            string text = line.text;
            foreach (var kvp in tokens)
                text = text.Replace($"{{{kvp.Key}}}", kvp.Value);

            if (speakerText != null)
            {
                string speaker = !string.IsNullOrEmpty(speakerOverride) ? speakerOverride : line.speaker;
                lineText.text = $"{speaker}：{text}";
            }
            else
            {
                lineText.text = text;
            }
        }
    }

    private void OnNextClicked()
    {
        if (!isRunning || currentDialogue == null) return;
        int next = currentIndex + 1;
        if (next >= currentDialogue.lines.Count) EndDialogue();
        else ShowLine(next);
    }

    private void EndDialogue()
    {
        isRunning = false;
        currentDialogue = null;
        currentIndex = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        var ctx = currentContext;
        currentContext = null;
        GameEvent.Emit("DialogueEnded", ctx);
    }
}
