using UnityEngine;
using UnityEngine.UI;

namespace SpringAutumn.Presentation.UI
{
    public static class UiTextFontResolver
    {
        private static readonly string[] FontCandidates =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "SimSun",
            "DengXian"
        };

        private static Font _font;

        public static void ApplyToScene()
        {
            Font font = GetFont();
            if (font == null)
                return;

            Text[] texts = Object.FindObjectsOfType<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].font = font;
            }
        }

        private static Font GetFont()
        {
            if (_font != null)
                return _font;

            for (int i = 0; i < FontCandidates.Length; i++)
            {
                _font = Font.CreateDynamicFontFromOSFont(FontCandidates[i], 18);
                if (_font != null)
                    return _font;
            }

            return null;
        }
    }
}
