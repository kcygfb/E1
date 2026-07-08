using UnityEngine;

namespace KiKs.Data
{
    /// <summary>
    /// Defines a resource type (e.g. gold, wood, gems) as a reusable ScriptableObject asset.
    /// Create instances via Assets > Create > KiKs > Resource Data.
    /// </summary>
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
}
