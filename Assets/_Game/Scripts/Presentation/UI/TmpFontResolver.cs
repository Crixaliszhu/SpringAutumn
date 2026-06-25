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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterGlobalFallback()
        {
            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset == null)
                return;

            var fallbackFontAssets = TMP_Settings.fallbackFontAssets;
            if (fallbackFontAssets == null)
                return;

            if (!fallbackFontAssets.Contains(fontAsset))
                fallbackFontAssets.Insert(0, fontAsset);
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
                Apply(text);
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null)
                return;

            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset != null)
            {
                AddFallback(text.font, fontAsset);
                text.font = fontAsset;
            }

            text.extraPadding = true;
            text.richText = true;
        }

        private static void AddFallback(TMP_FontAsset source, TMP_FontAsset fallback)
        {
            if (source == null || fallback == null || source == fallback)
                return;

            if (source.fallbackFontAssetTable == null)
                source.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

            if (!source.fallbackFontAssetTable.Contains(fallback))
                source.fallbackFontAssetTable.Insert(0, fallback);
        }

        private static TMP_FontAsset GetOrCreateFontAsset()
        {
            if (_fontAsset != null)
                return _fontAsset;

            _fontAsset = Resources.Load<TMP_FontAsset>(CjkFontResourcePath);
            if (_fontAsset != null)
                return _fontAsset;

            if (_fontAsset == null && !_warnedMissingFont)
            {
                _warnedMissingFont = true;
                Debug.LogWarning("[UI] Missing TMP CJK font resource. Run SpringAutumn/Build Scenes/Stage 1-10 Bootstrap HUD FinalCheck to generate it.");
            }

            return _fontAsset;
        }
    }
}
