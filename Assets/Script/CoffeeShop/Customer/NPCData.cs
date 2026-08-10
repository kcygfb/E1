using System.Collections.Generic;
using UnityEngine;

public enum SpawnOrder
{
    Random,
    First,
    Last
}

[CreateAssetMenu(fileName = "NPCData", menuName = "NPC/NPCData")]
public class NPCData : ScriptableObject
{
    public string npcId;

    public string npcName;

    [Tooltip("说话者别名列表。DialoguePlayer 匹配时除了 npcName 还会匹配这些别名。")]
    public List<string> speakerAliases = new();

    public Color speakerColor = Color.white;

    public Sprite portrait;

    [Tooltip("立绘池。出场时随机抽取一张；为空则使用 portrait。")]
    public List<Sprite> portraitPool = new();

    public Vector2 portraitSize = new Vector2(150, 200);
}
