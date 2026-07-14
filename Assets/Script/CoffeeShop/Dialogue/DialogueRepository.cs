using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class DialogueLineJson
{
    public string speaker;
    public string text;
}

[Serializable]
public class DialogueDataJson
{
    public string dialogueId;
    public List<DialogueLineJson> lines = new List<DialogueLineJson>();
}

[DisallowMultipleComponent]
public class DialogueRepository : MonoBehaviour
{
    [SerializeField] private string relativeDirectory = "Dialogue";
    [SerializeField] private bool autoLoadOnAwake = true;
    [SerializeField] private bool persistAcrossScenes = true;

    public static DialogueRepository Instance { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool IsLoading { get; private set; }
    public string LastError { get; private set; } = string.Empty;
    public event Action Loaded;

    private readonly Dictionary<string, DialogueDataJson> _dialogues = new(StringComparer.Ordinal);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
        if (autoLoadOnAwake) StartCoroutine(EnsureLoaded());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public IEnumerator EnsureLoaded()
    {
        if (IsLoaded) yield break;

        if (IsLoading)
        {
            while (IsLoading) yield return null;
            yield break;
        }

        IsLoading = true;
        LastError = string.Empty;

        var root = Application.streamingAssetsPath;
        var dir = string.IsNullOrWhiteSpace(relativeDirectory)
            ? root
            : Path.Combine(root, relativeDirectory.Trim('/', '\\'));

        string[] files;
        try
        {
            if (!Directory.Exists(dir))
            {
                Fail("Dialogue directory not found: " + dir);
                yield break;
            }
            files = Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly);
        }
        catch (Exception e)
        {
            Fail("Cannot access dialogue directory: " + e.Message);
            yield break;
        }

        foreach (var filePath in files)
        {
            string jsonText;
            try
            {
                jsonText = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Fail("Cannot read " + Path.GetFileName(filePath) + ": " + e.Message);
                yield break;
            }

            DialogueDataJson data;
            try
            {
                data = JsonUtility.FromJson<DialogueDataJson>(jsonText);
            }
            catch (Exception e)
            {
                Fail("Cannot parse " + Path.GetFileName(filePath) + ": " + e.Message);
                yield break;
            }

            if (data == null || string.IsNullOrEmpty(data.dialogueId))
            {
                Fail("Invalid dialogue data in " + Path.GetFileName(filePath));
                yield break;
            }

            _dialogues[data.dialogueId] = data;
        }

        IsLoading = false;
        IsLoaded = true;
        Loaded?.Invoke();
        Debug.Log("[DialogueRepository] Loaded " + _dialogues.Count + " dialogues from JSON.");
    }

    public DialogueDataJson GetDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return null;
        _dialogues.TryGetValue(dialogueId, out var data);
        return data;
    }

    public bool TryGetDialogue(string dialogueId, out DialogueDataJson dialogue)
    {
        dialogue = GetDialogue(dialogueId);
        return dialogue != null;
    }

    private void Fail(string message)
    {
        LastError = message;
        IsLoading = false;
        Debug.LogError("[DialogueRepository] " + message);
    }
}
