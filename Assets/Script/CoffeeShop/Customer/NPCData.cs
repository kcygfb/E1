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

    [Tooltip("对话ID，对应 StreamingAssets/Dialogue/ 下的 JSON 文件名（不含.json后缀）")]
    public string arrivalDialogueId;

    public string departureDialogueId;

    [Header("Special NPC")]
    [Tooltip("留空 = 普通NPC（随机选已解锁咖啡）；填写 = 指定咖啡ID，从JSON运行时解析")]
    public string desiredCoffeeId;

    [Tooltip("当 desiredCoffee 未解锁时播放的到达对话ID")]
    public string lockedDialogueId;

    [Tooltip("当 desiredCoffee 未解锁时，离开时播放的对话ID")]
    public string lockedDepartureDialogueId;

    [Header("Return Visit")]
    [Tooltip("回访时，如果咖啡已解锁，播放此对话ID")]
    public string returnFoundDialogueId;

    [Tooltip("回访时，如果咖啡仍锁定，播放此对话ID")]
    public string returnNotFoundDialogueId;

    [Tooltip("回访发现咖啡已解锁时的金币奖励")]
    public int returnReward = 0;

    [Tooltip("出场顺序，默认随机")]
    public SpawnOrder spawnOrder = SpawnOrder.Random;
}
