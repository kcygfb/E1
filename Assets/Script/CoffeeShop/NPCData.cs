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

    public Sprite portrait;

    public Vector2 portraitSize = new Vector2(150, 200);

    [Tooltip("如果为 true，NPC 到达柜台后会下单；否则对话结束直接离开")]
    public bool willOrder = true;

    public DialogueData arrivalDialogue;

    public DialogueData departureDialogue;

    [Header("Special NPC")]
    [Tooltip("null = 普通NPC（随机选咖啡）；非null = 特殊NPC（指定咖啡）")]
    public CoffeeData desiredCoffee;

    [Tooltip("当 desiredCoffee 未解锁时播放的到达对话")]
    public DialogueData lockedDialogue;

    [Tooltip("当 desiredCoffee 未解锁时，离开时播放的对话")]
    public DialogueData lockedDepartureDialogue;

    [Header("Return Visit")]
    [Tooltip("回访时，如果咖啡已解锁，播放此对话")]
    public DialogueData returnFoundDialogue;

    [Tooltip("回访时，如果咖啡仍锁定，播放此对话")]
    public DialogueData returnNotFoundDialogue;

    [Tooltip("回访发现咖啡已解锁时的金币奖励")]
    public int returnReward = 0;

    [Tooltip("出场顺序，默认随机")]
    public SpawnOrder spawnOrder = SpawnOrder.Random;
}
