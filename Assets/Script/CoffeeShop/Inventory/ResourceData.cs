using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "KiKs/Resource Data")]
public class ResourceData : ScriptableObject
{
    [SerializeField] private string resourceId = "gold";
    [SerializeField] private string displayName = "Gold";
    [SerializeField] private Sprite icon;
    [SerializeField] private int startingAmount = 0;

    public string ResourceId => resourceId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int StartingAmount => startingAmount;
}
