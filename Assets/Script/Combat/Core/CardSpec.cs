using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KiKs.Combat
{
    /// <summary>Immutable card data parsed from the JSON library.</summary>
    public sealed class CardSpec
    {
        public string Id { get; }
        public string DisplayNameZhCn { get; }
        public string DisplayNameEn { get; }
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(DisplayNameZhCn) ? DisplayNameZhCn :
            !string.IsNullOrWhiteSpace(DisplayNameEn) ? DisplayNameEn : Id;
        public string Category { get; }
        public CardResourceType CostResource { get; }
        public int CostAmount { get; }
        public bool IsSpecial { get; }
        public CardTargetType TargetType { get; }
        public IReadOnlyList<CardEffectSpec> Effects { get; }
        public bool CanUpgrade => Effects.Any(effect => effect.HasUpgrade);

        public CardSpec(
            string id,
            string displayNameZhCn,
            string displayNameEn,
            string category,
            CardResourceType costResource,
            int costAmount,
            bool isSpecial,
            CardTargetType targetType,
            IEnumerable<CardEffectSpec> effects)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Card id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Card category is required.", nameof(category));
            if (costAmount < 0) throw new ArgumentOutOfRangeException(nameof(costAmount));
            if (effects == null) throw new ArgumentNullException(nameof(effects));

            var effectList = new List<CardEffectSpec>(effects);
            if (effectList.Count == 0) throw new ArgumentException("A card needs at least one effect.", nameof(effects));
            if (effectList.Any(effect => effect == null)) throw new ArgumentException("Effect list contains null.", nameof(effects));

            Id = id;
            DisplayNameZhCn = displayNameZhCn ?? string.Empty;
            DisplayNameEn = displayNameEn ?? string.Empty;
            Category = category;
            CostResource = costResource;
            CostAmount = costAmount;
            IsSpecial = isSpecial;
            TargetType = targetType;
            Effects = new ReadOnlyCollection<CardEffectSpec>(effectList);
        }
    }
}
