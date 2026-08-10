using TMPro;
using UnityEngine;

namespace KiKs.UI
{
    /// <summary>Provides the project's shared Chinese font for TMP and legacy UI text.</summary>
    public static class ChineseFontProvider
    {
        private const string FontResourcePath = "Fonts & Materials/站酷文艺体 SDF";

        private static TMP_FontAsset _tmpFont;

        public static TMP_FontAsset TmpFont
        {
            get
            {
                if (_tmpFont == null)
                {
                    _tmpFont = Resources.Load<TMP_FontAsset>(FontResourcePath);
                    if (_tmpFont != null)
                        _tmpFont.isMultiAtlasTexturesEnabled = true;
                }

                return _tmpFont;
            }
        }

        public static Font LegacyFont
        {
            get
            {
                var tmpFont = TmpFont;
                return tmpFont != null && tmpFont.sourceFontFile != null
                    ? tmpFont.sourceFontFile
                    : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        public static void Apply(TMP_Text text)
        {
            if (text != null && TmpFont != null)
                text.font = TmpFont;
        }
    }
}
