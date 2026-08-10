using UnityEngine;
using UnityEngine.UI;

namespace KiKs.Combat
{
    /// <summary>
    /// Loads card sprites from Resources by the relative path stored in CardSpec.ImagePath.
    /// Paths in JSON look like "Art/Cards/fireaxe.png" — the ".png" extension is stripped for Resources.Load.
    /// </summary>
    public static class CardImageLoader
    {
        /// <summary>
        /// Load a Sprite from Resources given the ImagePath field from a CardSpec.
        /// Returns null if the path is empty or the asset cannot be found.
        /// </summary>
        public static Sprite LoadSprite(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            // Strip file extension for Resources.Load
            var resourcePath = imagePath;
            var dotIndex = resourcePath.LastIndexOf('.');
            if (dotIndex > 0)
                resourcePath = resourcePath.Substring(0, dotIndex);

            var sprite = Resources.Load<Sprite>(resourcePath);

            // spriteMode=2 (Multiple) 时 Load<Sprite> 返回 null，fallback 到 LoadAll
            if (sprite == null)
            {
                var all = Resources.LoadAll<Sprite>(resourcePath);
                if (all != null && all.Length > 0)
                    sprite = all[0];
            }

            return sprite;
        }

        /// <summary>
        /// Convert a normal image path to its upgraded variant.
        /// e.g. "Art/Cards/fireaxe.png" → "Art/Cards/fireaxeE.png"
        /// Returns null if the path is empty or doesn't contain a dot.
        /// </summary>
        public static string ResolveUpgradedPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            var dotIndex = imagePath.LastIndexOf('.');
            if (dotIndex <= 0)
                return null;

            return imagePath.Insert(dotIndex, "E");
        }

        /// <summary>
        /// Load a Sprite and apply it to the given Image component.
        /// If the sprite is null, the Image is disabled so it doesn't show a blank white quad.
        /// </summary>
        public static void ApplyToImage(Image target, string imagePath)
        {
            if (target == null)
                return;

            var sprite = LoadSprite(imagePath);
            target.sprite = sprite;
            target.enabled = sprite != null;
        }
    }
}
