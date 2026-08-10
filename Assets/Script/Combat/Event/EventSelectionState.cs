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
        /// 按天选事件：优先返回 e.day==day 的事件，无则从 day=0 随机选。
        /// </summary>
        public static EventDefinition PickEventForDay(int day)
        {
            var definition = EventJsonRepository.Load();
            if (definition == null || definition.events == null || definition.events.Length == 0)
                return null;

            // 优先匹配当天
            var dayMatch = definition.events
                .Where(e => e != null &&
                            !string.IsNullOrWhiteSpace(e.id) &&
                            !DeadNpcs.Contains(e.npcId) &&
                            !CompletedEvents.Contains(e.id) &&
                            e.day == day)
                .ToList();

            if (dayMatch.Count > 0)
            {
                var selected = dayMatch[Random.Range(0, dayMatch.Count)];
                Debug.Log($"[EventSelection] Picked '{selected.id}' (day {day}, {dayMatch.Count} available).");
                return selected;
            }

            // fallback: day=0 的通用事件
            var available = definition.events
                .Where(e => e != null &&
                            !string.IsNullOrWhiteSpace(e.id) &&
                            !DeadNpcs.Contains(e.npcId) &&
                            !CompletedEvents.Contains(e.id) &&
                            (e.day == 0 || e.day == day))
                .ToList();

            if (available.Count == 0)
            {
                Debug.Log($"[EventSelection] No available events for day {day}.");
                return null;
            }

            var minOrder = available.Min(e => e.order);
            var batch = available.Where(e => e.order == minOrder).ToList();
            var sel = batch[Random.Range(0, batch.Count)];
            Debug.Log($"[EventSelection] Picked '{sel.id}' (day {day}, order {sel.order}, {available.Count} available).");
            return sel;
        }

        /// <summary>兼容旧调用</summary>
        public static EventDefinition PickRandomEvent()
        {
            return PickEventForDay(GetCurrentDay());
        }

        /// <summary>直接读 RuntimeGameRepository.CurrentDay</summary>
        private static int GetCurrentDay()
        {
            return RuntimeGameRepository.CurrentDay;
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