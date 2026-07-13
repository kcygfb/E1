using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "NPC/NPCData")]
public class NPCData : ScriptableObject
{
    public string npcId;

    public string npcName;

    public string[] possibleOrders;

    public Sprite portrait;

    public Vector2 portraitSize = new Vector2(150, 200);

    public DialogueData arrivalDialogue;

    public DialogueData departureDialogue;
}
