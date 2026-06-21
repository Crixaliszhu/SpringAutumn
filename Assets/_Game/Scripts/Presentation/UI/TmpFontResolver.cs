using TMPro;
using UnityEngine;

namespace SpringAutumn.Presentation.UI
{
    /// <summary>Provides a runtime TMP font that can render Chinese UI text in editor and player.</summary>
    public static class TmpFontResolver
    {
        private const string CjkFontResourcePath = "Fonts/SpringAutumn CJK SDF";
        private static TMP_FontAsset _fontAsset;
        private static bool _warnedMissingFont;

        public static void ApplyToScene()
        {
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
                Apply(text);
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset != null)
                text.font = fontAsset;

            text.extraPadding = true;
            text.richText = true;
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            if (_fontAsset != null)
                return _fontAsset;

            _fontAsset = Resources.Load<TMP_FontAsset>(CjkFontResourcePath);
            if (_fontAsset == null && !_warnedMissingFont)
            {
                _warnedMissingFont = true;
                Debug.LogWarning("[UI] Missing TMP CJK font resource. Rebuild scenes via SpringAutumn/Build Scenes/Stage 1-2 Bootstrap HUD.");
            }

            return _fontAsset;
        }
    }
}
