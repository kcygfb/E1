using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KiKs.Combat
{
    /// <summary>
    /// Validated, immutable card library. Both the selection screen and battle creation query this
    /// same repository by card id.
    /// </summary>
    public sealed class CardJsonRepository
    {
        private readonly Dictionary<string, CardSpec> _byId;

        public IReadOnlyList<CardSpec> Cards { get; }

        private CardJsonRepository(List<CardSpec> cards)
        {
            Cards = new ReadOnlyCollection<CardSpec>(cards);
            _byId = cards.ToDictionary(card => card.Id, StringComparer.Ordinal);
        }

        public bool TryGetCard(string cardId, out CardSpec card)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                card = null;
                return false;
            }

            return _byId.TryGetValue(cardId, out card);
        }

        public CardSpec GetRequiredCard(string cardId)
        {
            if (!TryGetCard(cardId, out var card))
                throw new KeyNotFoundException("Card id does not exist in JSON: " + cardId);
            return card;
        }

        public static IReadOnlyList<string> ReadManifestFiles(string manifestJson)
        {
            var manifest = SimpleJsonParser.ParseObject(manifestJson);
            ValidateSchemaVersion(manifest, "manifest");
            var files = RequiredList(manifest, "files");
            var result = new List<string>(files.Count);

            foreach (var item in files)
            {
                var entry = AsObject(item, "manifest.files entry");
                result.Add(RequiredString(entry, "file"));
            }

            return new ReadOnlyCollection<string>(result);
        }

        public static CardJsonRepository Load(string manifestJson, Func<string, string> readCardFile)
        {
            if (readCardFile == null) throw new ArgumentNullException(nameof(readCardFile));

            var manifest = SimpleJsonParser.ParseObject(manifestJson);
            ValidateSchemaVersion(manifest, "manifest");
            var expectedTotal = RequiredInt(manifest, "cardCount");
            var fileEntries = RequiredList(manifest, "files");
            var cards = new List<CardSpec>(expectedTotal);
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (var rawEntry in fileEntries)
            {
                var entry = AsObject(rawEntry, "manifest.files entry");
                var fileName = RequiredString(entry, "file");
                var expectedCategory = RequiredString(entry, "category");
                var expectedFileCount = RequiredInt(entry, "cardCount");
                var text = readCardFile(fileName);
                if (text == null) throw new InvalidOperationException("Card file returned null: " + fileName);

                var root = SimpleJsonParser.ParseObject(text);
                ValidateSchemaVersion(root, fileName);
                var category = RequiredString(root, "category");
                if (!string.Equals(category, expectedCategory, StringComparison.Ordinal))
                    throw new FormatException(fileName + " category does not match the manifest.");

                var rawCards = RequiredList(root, "cards");
                if (rawCards.Count != expectedFileCount)
                    throw new FormatException(fileName + " card count does not match the manifest.");

                foreach (var rawCard in rawCards)
                {
                    var card = ParseCard(AsObject(rawCard, fileName + ".cards entry"), category);
                    if (!ids.Add(card.Id)) throw new FormatException("Duplicate card id: " + card.Id);
                    cards.Add(card);
                }
            }

            if (cards.Count != expectedTotal)
                throw new FormatException("Loaded " + cards.Count + " cards but manifest expects " + expectedTotal + ".");

            return new CardJsonRepository(cards);
        }

        private static CardSpec ParseCard(Dictionary<string, object> card, string fileCategory)
        {
            var id = RequiredString(card, "id");
            var category = RequiredString(card, "category");
            if (!string.Equals(category, fileCategory, StringComparison.Ordinal))
                throw new FormatException(id + " category does not match its file.");

            var names = RequiredObject(card, "name");
            var zhCn = OptionalString(names, "zhCN");
            var en = OptionalString(names, "en");
            var cost = RequiredObject(card, "cost");
            var costResource = ParseResource(RequiredString(cost, "resource"));
            var costAmount = RequiredInt(cost, "amount");
            var special = OptionalBool(card, "special");
            var rawEffects = RequiredList(card, "effects");
            if (rawEffects.Count == 0) throw new FormatException(id + " has no effects.");

            var effects = new List<CardEffectSpec>(rawEffects.Count);
            foreach (var rawEffect in rawEffects)
                effects.Add(ParseEffect(AsObject(rawEffect, id + ".effects entry")));

            return new CardSpec(
                id,
                zhCn,
                en,
                category,
                costResource,
                costAmount,
                special,
                InferTargetType(effects),
                effects);
        }

        private static CardEffectSpec ParseEffect(Dictionary<string, object> effect)
        {
            var type = ParseEffectType(RequiredString(effect, "type"));
            return new CardEffectSpec(
                type,
                OptionalUpgradeable(effect, "amount", UpgradeableNumber.Zero),
                OptionalUpgradeable(effect, "hits", UpgradeableNumber.One),
                OptionalUpgradeable(effect, "durationTurns", UpgradeableNumber.Zero),
                OptionalUpgradeable(effect, "triggerCount", UpgradeableNumber.Zero),
                OptionalUpgradeable(effect, "damagePerTurn", UpgradeableNumber.Zero),
                ParseDamageType(OptionalString(effect, "damageType")),
                ParseUnit(OptionalString(effect, "unit")),
                OptionalInt(effect, "minimumDamagePerHit"),
                OptionalBool(effect, "stackable"),
                OptionalDouble(effect, "multiplier", 1d),
                OptionalString(effect, "companionId"),
                OptionalInt(effect, "normalTargetPercent"),
                OptionalInt(effect, "bossPercent"),
                ParseResource(OptionalString(effect, "resource")),
                OptionalString(effect, "timing"),
                OptionalString(effect, "selection"));
        }

        private static CardTargetType InferTargetType(IEnumerable<CardEffectSpec> effects)
        {
            return effects.Any(effect =>
                effect.Type == CardEffectType.Damage ||
                effect.Type == CardEffectType.ToughnessDamage ||
                effect.Type == CardEffectType.Stun ||
                effect.Type == CardEffectType.Bleed ||
                effect.Type == CardEffectType.Poison ||
                effect.Type == CardEffectType.Vulnerability ||
                effect.Type == CardEffectType.LifeStealMaxHealth)
                ? CardTargetType.SingleEnemy
                : CardTargetType.Self;
        }

        private static CardEffectType ParseEffectType(string value)
        {
            switch (value)
            {
                case "damage": return CardEffectType.Damage;
                case "toughness_damage": return CardEffectType.ToughnessDamage;
                case "stun": return CardEffectType.Stun;
                case "bleed": return CardEffectType.Bleed;
                case "poison": return CardEffectType.Poison;
                case "vulnerability": return CardEffectType.Vulnerability;
                case "nullify_attacks": return CardEffectType.NullifyAttacks;
                case "damage_reduction": return CardEffectType.DamageReduction;
                case "skip_enemy_turns": return CardEffectType.SkipEnemyTurns;
                case "draw_cards": return CardEffectType.DrawCards;
                case "immunity": return CardEffectType.Immunity;
                case "summon_companion": return CardEffectType.SummonCompanion;
                case "life_steal_max_health": return CardEffectType.LifeStealMaxHealth;
                case "gain_resource": return CardEffectType.GainResource;
                case "play_cards_from_discard": return CardEffectType.PlayCardsFromDiscard;
                default: throw new FormatException("Unknown card effect type: " + value);
            }
        }

        private static CardResourceType ParseResource(string value)
        {
            switch (value)
            {
                case "":
                case "action_point": return CardResourceType.ActionPoint;
                case "mana": return CardResourceType.Mana;
                default: throw new FormatException("Unknown resource: " + value);
            }
        }

        private static DamageType ParseDamageType(string value)
        {
            switch (value)
            {
                case "":
                case "normal": return DamageType.Normal;
                case "true": return DamageType.True;
                default: throw new FormatException("Unknown damage type: " + value);
            }
        }

        private static ValueUnit ParseUnit(string value)
        {
            switch (value)
            {
                case "":
                case "points": return ValueUnit.Points;
                case "percent": return ValueUnit.Percent;
                default: throw new FormatException("Unknown value unit: " + value);
            }
        }

        private static UpgradeableNumber OptionalUpgradeable(
            Dictionary<string, object> owner,
            string key,
            UpgradeableNumber fallback)
        {
            if (!owner.TryGetValue(key, out var raw) || raw == null) return fallback;
            if (raw is long || raw is double)
                return new UpgradeableNumber(ConvertToInt(raw, key), null);
            var value = AsObject(raw, key);
            var baseValue = RequiredInt(value, "base");
            int? upgraded = null;
            if (value.TryGetValue("upgraded", out var rawUpgraded) && rawUpgraded != null)
                upgraded = ConvertToInt(rawUpgraded, key + ".upgraded");
            return new UpgradeableNumber(baseValue, upgraded);
        }

        private static void ValidateSchemaVersion(Dictionary<string, object> value, string source)
        {
            var version = RequiredInt(value, "schemaVersion");
            if (version != 1) throw new FormatException(source + " uses unsupported schemaVersion " + version + ".");
        }

        private static Dictionary<string, object> RequiredObject(Dictionary<string, object> owner, string key)
        {
            if (!owner.TryGetValue(key, out var value)) throw new FormatException("Missing object: " + key);
            return AsObject(value, key);
        }

        private static Dictionary<string, object> AsObject(object value, string context)
        {
            if (value is Dictionary<string, object> objectValue) return objectValue;
            throw new FormatException(context + " must be an object.");
        }

        private static List<object> RequiredList(Dictionary<string, object> owner, string key)
        {
            if (!owner.TryGetValue(key, out var value) || !(value is List<object> list))
                throw new FormatException("Missing or invalid array: " + key);
            return list;
        }

        private static string RequiredString(Dictionary<string, object> owner, string key)
        {
            var value = OptionalString(owner, key);
            if (string.IsNullOrWhiteSpace(value)) throw new FormatException("Missing string: " + key);
            return value;
        }

        private static string OptionalString(Dictionary<string, object> owner, string key)
        {
            if (!owner.TryGetValue(key, out var value) || value == null) return string.Empty;
            if (value is string text) return text;
            throw new FormatException(key + " must be a string.");
        }

        private static int RequiredInt(Dictionary<string, object> owner, string key)
        {
            if (!owner.TryGetValue(key, out var value)) throw new FormatException("Missing integer: " + key);
            return ConvertToInt(value, key);
        }

        private static int OptionalInt(Dictionary<string, object> owner, string key)
        {
            return owner.TryGetValue(key, out var value) && value != null ? ConvertToInt(value, key) : 0;
        }

        private static int ConvertToInt(object value, string context)
        {
            if (value is long integer && integer >= int.MinValue && integer <= int.MaxValue) return (int)integer;
            if (value is double number && Math.Abs(number % 1d) < double.Epsilon &&
                number >= int.MinValue && number <= int.MaxValue) return (int)number;
            throw new FormatException(context + " must be an integer.");
        }

        private static double OptionalDouble(Dictionary<string, object> owner, string key, double fallback)
        {
            if (!owner.TryGetValue(key, out var value) || value == null) return fallback;
            if (value is long integer) return integer;
            if (value is double number) return number;
            throw new FormatException(key + " must be a number.");
        }

        private static bool OptionalBool(Dictionary<string, object> owner, string key)
        {
            if (!owner.TryGetValue(key, out var value) || value == null) return false;
            if (value is bool boolean) return boolean;
            throw new FormatException(key + " must be a boolean.");
        }
    }
}
