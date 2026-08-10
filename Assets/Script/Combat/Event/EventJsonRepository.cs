using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KiKs.Combat
{
    public static class EventJsonRepository
    {
        public const string RelativePath = "Event/events.json";
        public const int ExpectedSchemaVersion = 3;
        public const int MinimumCardCount = 1;

        public static EventSceneDefinition Load()
        {
            var path = Path.Combine(Application.streamingAssetsPath, RelativePath);
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("Event configuration was not found.", path);

                var definition = JsonUtility.FromJson<EventSceneDefinition>(File.ReadAllText(path));
                if (!TryValidate(definition, out var validationError))
                    throw new InvalidDataException(validationError);

                return definition;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Event] Failed to load '{path}': {exception.Message}. Using test fallback data.");
                return CreateFallback();
            }
        }

        public static bool TryValidate(EventSceneDefinition definition, out string error)
        {
            if (definition == null)
            {
                error = "The root object is null.";
                return false;
            }

            if (definition.schemaVersion != ExpectedSchemaVersion)
            {
                error = $"Event schemaVersion must be {ExpectedSchemaVersion}, got {definition.schemaVersion}.";
                return false;
            }

            if (definition.events == null || definition.events.Length == 0)
            {
                error = "At least one event is required.";
                return false;
            }

            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            for (var eventIndex = 0; eventIndex < definition.events.Length; eventIndex++)
            {
                var evt = definition.events[eventIndex];
                if (evt == null || string.IsNullOrWhiteSpace(evt.id))
                {
                    error = $"Event {eventIndex + 1} needs a non-empty id.";
                    return false;
                }

                if (!eventIds.Add(evt.id))
                {
                    error = $"Duplicate event id '{evt.id}'.";
                    return false;
                }

                if (evt.day < 0 || evt.day >= LoopProgressionRepository.FinalDay)
                {
                    error = $"Event '{evt.id}' has invalid loop day {evt.day}.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(evt.npcId))
                {
                    error = $"Event '{evt.id}' needs an npcId.";
                    return false;
                }

                if (evt.order <= 0)
                {
                    error = $"Event '{evt.id}' needs a positive order.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(evt.introDialogueId))
                {
                    error = $"Event '{evt.id}' needs an introDialogueId.";
                    return false;
                }

                if (evt.cards == null || evt.cards.Length < MinimumCardCount)
                {
                    error = $"Event '{evt.id}' needs at least {MinimumCardCount} cards.";
                    return false;
                }

                for (var cardIndex = 0; cardIndex < evt.cards.Length; cardIndex++)
                {
                    var card = evt.cards[cardIndex];
                    if (card == null || string.IsNullOrWhiteSpace(card.type) ||
                        string.IsNullOrWhiteSpace(card.imagePath))
                    {
                        error = $"Event '{evt.id}' card {cardIndex + 1} needs a type and imagePath.";
                        return false;
                    }

                    switch (card.type)
                    {
                        case "effect":
                            // effect 卡可选 dialogueId
                            break;
                        case "attack":
                            // attack 卡可选 cardRewardMode/cardRewardIds
                            break;
                        case "pilfer":
                            // pilfer 卡可选 cardRewardIds
                            break;
                        case "end":
                            // end 卡可选 dialogueId 和奖励
                            break;
                        default:
                            error = $"Event '{evt.id}' card {cardIndex + 1} has unknown type '{card.type}'.";
                            return false;
                    }
                }
            }

            for (var day = 1; day < LoopProgressionRepository.FinalDay; day++)
            {
                if (!Array.Exists(definition.events, evt => evt != null && evt.day == day))
                {
                    error = $"Event configuration has no event for loop day {day}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public static EventSceneDefinition CreateFallback()
        {
            var events = new EventDefinition[LoopProgressionRepository.FinalDay - 1];
            for (var day = 1; day < LoopProgressionRepository.FinalDay; day++)
                events[day - 1] = CreateFallbackEvent(day);

            return new EventSceneDefinition
            {
                schemaVersion = ExpectedSchemaVersion,
                events = events
            };
        }

        private static EventDefinition CreateFallbackEvent(int day)
        {
            return new EventDefinition
            {
                id = $"evt_fallback_day{day}",
                npcId = "namelessking",
                npcDisplayName = "Fallback Wanderer",
                order = 1,
                day = day,
                introDialogueId = "evt_light_intro",
                cards = new[]
                {
                    new EventCardDefinition
                    {
                        type = "effect",
                        imagePath = "Art/Cards/50C.png",
                        dialogueId = "evt_light_opt1",
                        hpCost = 10,
                        goldRewardMin = 20,
                        goldRewardMax = 100,
                        cardRewardMode = "random_normal"
                    },
                    new EventCardDefinition
                    {
                        type = "effect",
                        imagePath = "Art/Cards/100C.png",
                        dialogueId = "evt_light_opt2",
                        goldCost = 50,
                        materialRewardId = "random_raw",
                        materialRewardAmount = 2,
                        cardRewardMode = "random_normal"
                    },
                    new EventCardDefinition
                    {
                        type = "attack",
                        imagePath = "Art/Cards/200C.png",
                        cardRewardMode = "random_special"
                    },
                    new EventCardDefinition
                    {
                        type = "end",
                        imagePath = "Art/Cards/400C.png",
                        dialogueId = "evt_light_end"
                    }
                }
            };
        }
    }
}