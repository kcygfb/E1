using UnityEngine;

namespace KiKs.Data
{
    /// <summary>
    /// 资源数据：定义一种资源类型（如金币、木材、宝石），作为可复用的 ScriptableObject 资产。
    /// 通过 Assets > Create > KiKs > Resource Data 创建实例。
    /// Defines a resource type (e.g. gold, wood, gems) as a reusable ScriptableObject asset.
    /// Create instances via Assets > Create > KiKs > Resource Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "KiKs/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        // 资源唯一标识符
        [SerializeField] private string resourceId = "gold";
        // 资源显示名称
        [SerializeField] private string displayName = "Gold";
        // 资源图标
        [SerializeField] private Sprite icon;
        // 资源初始数量
        [SerializeField] private int startingAmount = 0;

        /// <summary>资源唯一标识符（只读）</summary>
        public string ResourceId => resourceId;
        /// <summary>资源显示名称（只读）</summary>
        public string DisplayName => displayName;
        /// <summary>资源图标（只读）</summary>
        public Sprite Icon => icon;
        /// <summary>资源初始数量（只读）</summary>
        public int StartingAmount => startingAmount;
    }
}
