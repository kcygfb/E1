using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KiKs.Combat
{
    /// <summary>
    /// A fact produced by the rules layer. UI and animation code consume these facts instead of
    /// inferring gameplay changes from mutable scene objects.
    /// </summary>
    /// <remarks>
    /// 战斗事件：由规则层产生的不可变事实。UI 和动画通过消费这些事件来驱动表现，
    /// 而不是直接监听可变场景对象的变化。
    /// </remarks>
    [Serializable]
    public sealed class CombatEvent
    {
        public CombatEventType Type { get; }
        public string SourceId { get; }
        public string TargetId { get; }
        public string CardInstanceId { get; }
        public int Amount { get; }
        public string Message { get; }

        public CombatEvent(
            CombatEventType type,
            string sourceId = null,
            string targetId = null,
            string cardInstanceId = null,
            int amount = 0,
            string message = null)
        {
            Type = type;
            SourceId = sourceId;
            TargetId = targetId;
            CardInstanceId = cardInstanceId;
            Amount = amount;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// 战斗操作结果：包含成功状态、消息文本和产生的事件列表。
    /// 构造函数为 internal，仅限 CombatEngine 内部创建。
    /// </summary>
    public sealed class CombatResult
    {
        public bool Success { get; }
        public string Message { get; }
        public IReadOnlyList<CombatEvent> Events { get; }

        internal CombatResult(bool success, string message, IList<CombatEvent> events)
        {
            Success = success;
            Message = message ?? string.Empty;
            Events = new ReadOnlyCollection<CombatEvent>(new List<CombatEvent>(events));
        }
    }
}
