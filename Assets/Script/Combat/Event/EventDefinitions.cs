using System;

namespace KiKs.Combat
{
    [Serializable]
    public sealed class EventSceneDefinition
    {
        public int schemaVersion = 1;
        public EventDefinition[] events = Array.Empty<EventDefinition>();
    }

    [Serializable]
    public sealed class EventDefinition
    {
        public string id;
        public string npcId;
        public string npcDisplayName;
        public int order = 1;

        /// <summary>该事件在第几天出现。0=任意天。多个事件可共用同一天。</summary>
        public int day = 0;

        public string introDialogueId;
        public EventCardDefinition[] cards = Array.Empty<EventCardDefinition>();
    }

    [Serializable]
    public sealed class EventCardDefinition
    {
        /// <summary>
        /// effect=触发效果+对话；attack=攻击NPC+掉落；end=对话后结束。
        /// </summary>
        public string type;
        public string imagePath;
        public string dialogueId;

        // 代价
        public int hpCost;
        public int goldCost;

        // 金币奖励范围
        public int goldRewardMin;
        public int goldRewardMax;

        // 材料奖励
        public string materialRewardId;
        public int materialRewardAmount;

        // 卡牌奖励: "random_normal" = 随机普通卡, "random_special" = 随机特殊卡, "specific" = 指定卡
        public string cardRewardMode;
        public string cardRewardSpecificId;
    }
}