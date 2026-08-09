using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KiKs.Combat
{
    /// <summary>
    /// Session-lifetime event selection state. Tracks current event, completed events,
    /// and killed NPCs (killing an NPC locks all future events referencing it).
    /// Reset on each new play session (SubsystemRegistration).
    /// </summary>
    public static class EventSelectionState
    {
        private static readonly HashSet<string> CompletedEvents = new(System.StringComparer.Ordinal);
        private static readonly HashSet<string> DeadNpcs = new(System.StringComparer.Ordinal);

        public static EventDefinition CurrentEvent { get; private set; }
        public static IReadOnlyCollection<string> CompletedEventIds => CompletedEvents;
        public static IReadOnlyCollection<string> DeadNpcIds => DeadNpcs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlaySessionStart()
        {
            Reset();
        }

        /// <summary>
        /// 从所有可用事件中随机选一个：过滤已死 NPC 与已完成事件，
        /// 按当前天数过滤（day=0 表示任意天都出现），
        /// 在剩余事件中按最小 order 批次随机选一个。
        /// </summary>
        public static EventDefinition PickRandomEvent()
        {
            var definition = EventJsonRepository.Load();
            if (definition == null || definition.events == null || definition.events.Length == 0)
                return null;

            var currentDay = GetCurrentDay();

            var available = definition.events
                .Where(e => e != null &&
                            !string.IsNullOrWhiteSpace(e.id) &&
                            !DeadNpcs.Contains(e.npcId) &&
                            !CompletedEvents.Contains(e.id) &&
                            (e.day == 0 || e.day == currentDay))
                .ToList();

            if (available.Count == 0)
            {
                Debug.Log($"[EventSelection] No available events for day {currentDay}.");
                return null;
            }

            var minOrder = available.Min(e => e.order);
            var batch = available.Where(e => e.order == minOrder).ToList();
            var selected = batch[Random.Range(0, batch.Count)];
            Debug.Log($"[EventSelection] Picked '{selected.id}' (day {currentDay}, order {selected.order}, {available.Count} available).");
            return selected;
        }

        /// <summary>通过反射读取 TimeSystem.savedDayCount 获取当前天数。</summary>
        private static int GetCurrentDay()
        {
            var timeSystemType = System.Type.GetType("TimeSystem, Assembly-CSharp");
            var dayField = timeSystemType?.GetField("savedDayCount",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (dayField != null && dayField.FieldType == typeof(int))
                return (int)dayField.GetValue(null);
            return 1;
        }

        public static void SetCurrentEvent(EventDefinition evt)
        {
            CurrentEvent = evt;
        }

        public static void ClearCurrent()
        {
            CurrentEvent = null;
        }

        public static void MarkEventCompleted(string eventId)
        {
            if (!string.IsNullOrWhiteSpace(eventId))
                CompletedEvents.Add(eventId);
        }

        public static void MarkNpcDead(string npcId)
        {
            if (!string.IsNullOrWhiteSpace(npcId))
                DeadNpcs.Add(npcId);
        }

        public static void Reset()
        {
            CurrentEvent = null;
            CompletedEvents.Clear();
            DeadNpcs.Clear();
        }
    }
}