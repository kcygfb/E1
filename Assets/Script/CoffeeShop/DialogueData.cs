using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "NPC/DialogueData")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}
