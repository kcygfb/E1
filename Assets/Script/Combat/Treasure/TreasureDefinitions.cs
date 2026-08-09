using System;

namespace KiKs.Combat
{
    [Serializable]
    public sealed class TreasureSceneDefinition
    {
        public int schemaVersion = 1;
        public int testStartingGold = 400;
        public TreasureOfferDefinition[] offers = Array.Empty<TreasureOfferDefinition>();
    }

    [Serializable]
    public sealed class TreasureOfferDefinition
    {
        public string id;
        public int price;

        // Card face art is authored as a Sprite under Assets/Resources.
        // Example: "Art/Cards/treasure_50.png".
        public string imagePath;

        public TreasureProductDefinition[] productPool = Array.Empty<TreasureProductDefinition>();
    }

    [Serializable]
    public sealed class TreasureProductDefinition
    {
        public int weight = 1;
        public TreasureRewardDefinition reward;
    }

    [Serializable]
    public sealed class TreasureRewardDefinition
    {
        public string type;
        public string id;
        public string displayName;
        public int amount = 1;

        public string GetDisplayText()
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            return amount > 1 ? $"{name} ×{amount}" : name;
        }
    }
}
