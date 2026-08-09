using System;
using System.IO;
using UnityEngine;

namespace KiKs.Combat
{
    public static class EventJsonRepository
    {
        public const string RelativePath = "Event/events.json";
        public const int RequiredCardCount = 4;

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

            if (definition.events == null || definition.events.Length == 0)
            {
                error = "At least one event is required.";
                return false;
            }

            for (var eventIndex = 0; eventIndex < definition.events.Length; eventIndex++)
            {
                var evt = definition.events[eventIndex];
                if (evt == null || string.IsNullOrWhiteSpace(evt.id))
                {
                    error = $"Event {eventIndex + 1} needs a non-empty id.";
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

                if (evt.cards == null || evt.cards.Length != RequiredCardCount)
                {
                    error = $"Event '{evt.id}' needs exactly {RequiredCardCount} cards.";
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
                            if (string.IsNullOrWhiteSpace(card.dialogueId))
                            {
                                error = $"Event '{evt.id}' effect card {cardIndex + 1} needs a dialogueId.";
                                return false;
                            }
                            break;
                        case "attack":
                            // attack 卡需要 cardRewardMode
                            if (string.IsNullOrWhiteSpace(card.cardRewardMode))
                            {
                                error = $"Event '{evt.id}' attack card {cardIndex + 1} needs a cardRewardMode.";
                                return false;
                            }
                            break;
                        case "end":
                            if (string.IsNullOrWhiteSpace(card.dialogueId))
                            {
                                error = $"Event '{evt.id}' end card {cardIndex + 1} needs a dialogueId.";
                                return false;
                            }
                            break;
                        default:
                            error = $"Event '{evt.id}' card {cardIndex + 1} has unknown type '{card.type}'.";
                            return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        public static EventSceneDefinition CreateFallback()
        {
            return new EventSceneDefinition
            {
                schemaVersion = 2,
                events = new[]
                {
                    new EventDefinition
                    {
                        id = "evt_fallback",
                        npcId = "light",
                        npcDisplayName = "提灯人",
                        order = 1,
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
                    }
                }
            };
        }
    }
}