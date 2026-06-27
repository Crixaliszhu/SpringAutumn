using TMPro;
using UnityEngine;

namespace SpringAutumn.Presentation.UI
{
    /// <summary>Provides a runtime TMP font that can render Chinese UI text in editor and player.</summary>
    public static class TmpFontResolver
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGlobalFallback()
        {
            RemoveBrokenFallbacks(TMP_Settings.fallbackFontAssets);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyAfterSceneLoad()
        {
            ApplyToScene();
        }

        public static void ApplyToScene()
        {
            RegisterGlobalFallback();
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
            {
                Apply(text);
                LegacyTextMirror.FromTmp(text);
            }
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null)
                return;

            RemoveBrokenFallbacks(text.font?.fallbackFontAssetTable);
            if (text.font == null || IsUnusableDynamicFont(text.font))
                text.font = ResolvePrimaryFont();
            if (text.font != null && text.font.material != null)
                text.fontSharedMaterial = text.font.material;

            text.extraPadding = true;
            text.richText = true;
        }

        private static TMP_FontAsset ResolvePrimaryFont()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null && !IsUnusableDynamicFont(defaultFont))
                return defaultFont;

            TMP_FontAsset resourceFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return resourceFont;
        }

        private static bool IsUnusableDynamicFont(TMP_FontAsset fontAsset)
        {
            return fontAsset != null
                && fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic
                && fontAsset.sourceFontFile == null
                && (fontAsset.characterTable == null || fontAsset.characterTable.Count == 0);
        }

        private static void RemoveBrokenFallbacks(System.Collections.Generic.List<TMP_FontAsset> fallbackFontAssets)
        {
            if (fallbackFontAssets == null)
                return;

            for (int i = fallbackFontAssets.Count - 1; i >= 0; i--)
            {
                TMP_FontAsset fallback = fallbackFontAssets[i];
                if (fallback == null || IsUnusableDynamicFont(fallback))
                    fallbackFontAssets.RemoveAt(i);
            }
        }
    }
}
