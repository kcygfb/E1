using System;
using UnityEngine;

public enum DialogueTrigger
{
    None,
    CreateOrder
    // 预留扩展: ShowPrice, GiveItem, StartQuest, BranchSelect ...
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    public DialogueTrigger trigger = DialogueTrigger.None;
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "NPC/DialogueData")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}
