using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using KiKs.UI;

public class DialoguePlayer : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text speakerText;
    public Text lineText;
    public Button nextButton;

    [Header("打字机效果")]
    [SerializeField] private float charsPerSecond = 30f;

    [Header("对话时隐藏的 UI")]
    [SerializeField] private GameObject[] hideDuringDialogue;

    [Header("下一句提示图标")]
    [SerializeField] private GameObject nextWordIcon;

    [Header("主角")]
    [SerializeField] private string playerName = "Avril";
    [SerializeField] private Color playerColor = new Color(0.4f, 0.8f, 1f, 1f);

    private DialogueDataJson currentDialogue;
    private int currentIndex;
    private bool isRunning;
    private bool isTyping;
    private Coroutine typingRoutine;
    private string currentFullText;
    private Color _speakerColor = Color.white;
    private Dictionary<string, string> tokens = new();
    private string speakerOverride;
    private string currentContext;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    private void Update()
    {
        if (!isRunning) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb[UnityEngine.InputSystem.Key.F8].wasPressedThisFrame)
        {
            Debug.Log("[DialoguePlayer] F8 skip dialogue");
            EndDialogue();
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
    }

    private void OnEnable()
    {
        GameEvent.On("DialogueRequested", OnDialogueRequested);
        DialogueBridge.OnAdvance += OnDialogueAdvance;
    }

    private void OnDisable()
    {
        GameEvent.Off("DialogueRequested", OnDialogueRequested);
        DialogueBridge.OnAdvance -= OnDialogueAdvance;
    }

    private void OnDialogueAdvance()
    {
        if (isRunning) OnNextClicked();
    }

    private void OnDialogueRequested(object payload)
    {
        if (payload is not DialogueRequest req) return;
        StartDialogue(req.DialogueId, req.Context, req.Tokens, req.SpeakerOverride, req.SpeakerColor);
    }

    public void StartDialogue(string dialogueId, string context,
        Dictionary<string, string> tokens = null, string speakerOverride = null,
        Color speakerColor = default)
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
        _speakerColor = speakerColor == default ? Color.white : speakerColor;
        currentContext = context;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (nextWordIcon != null) nextWordIcon.SetActive(false);
        HideUI(true);
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

            string speaker = !string.IsNullOrEmpty(line.speaker) ? line.speaker : (speakerOverride ?? "");

            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.color = speaker == playerName ? playerColor : _speakerColor;
            }

            if (typingRoutine != null) StopCoroutine(typingRoutine);
            currentFullText = text;
            if (nextWordIcon != null) nextWordIcon.SetActive(false);
            typingRoutine = StartCoroutine(TypeText(text));
            AnimateSpeaker(speaker);
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        lineText.text = "";
        float delay = 1f / charsPerSecond;
        for (int i = 0; i < fullText.Length; i++)
        {
            lineText.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }
        lineText.text = fullText;
        isTyping = false;
        if (nextWordIcon != null) nextWordIcon.SetActive(true);
    }

    public void OnNextClicked()
    {
        if (!isRunning || currentDialogue == null) return;

        if (isTyping)
        {
            // 停止打字动画，显示整句，不跳转下一句
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            lineText.text = currentFullText;
            isTyping = false;
            if (nextWordIcon != null) nextWordIcon.SetActive(true);
            return;
        }

        int next = currentIndex + 1;
        if (next >= currentDialogue.lines.Count) EndDialogue();
        else ShowLine(next);
    }

    private void EndDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        isTyping = false;
        isRunning = false;
        currentDialogue = null;
        currentIndex = 0;
        if (nextWordIcon != null) nextWordIcon.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        HideUI(false);
        var ctx = currentContext;
        currentContext = null;
        GameEvent.Emit("DialogueEnded", ctx);
    }

    private void HideUI(bool hide)
    {
        if (hideDuringDialogue == null) return;
        foreach (var go in hideDuringDialogue)
        {
            if (go != null) go.SetActive(!hide);
        }
    }

    private void AnimateSpeaker(string speaker)
    {
        if (string.IsNullOrEmpty(speaker)) return;

        RectTransform target = null;

        if (speaker == playerName)
        {
            var player = GameObject.Find("Canvas/PlayerArea/PlayerP");
            if (player != null) target = player.GetComponent<RectTransform>();
        }
        else
        {
            foreach (var kvp in CustomerController.ActiveCustomers)
            {
                if (kvp.Value == null) continue;
                var npcName = kvp.Key;
                if (npcName == speaker ||
                    npcName.Contains(speaker) ||
                    speaker.Contains(npcName))
                {
                    target = kvp.Value.GetComponent<RectTransform>();
                    break;
                }
            }
        }

        if (target == null) return;

        target.DOKill();
        target.localScale = Vector3.one;
        target.DOScale(1.08f, 0.12f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
    }
}
