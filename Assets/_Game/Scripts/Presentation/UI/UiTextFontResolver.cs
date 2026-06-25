using UnityEngine;
using UnityEngine.UI;

namespace SpringAutumn.Presentation.UI
{
    public static class UiTextFontResolver
    {
        private const string CjkSample = "春秋问鼎区域地图所属人口粮食铜钱守军建设征兵调兵返回天下";

        private static readonly string[] FontCandidates =
        {
            "PingFang SC",
            "Hiragino Sans GB",
            "Songti SC",
            "Heiti SC",
            "STHeiti",
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "SimHei",
            "SimSun",
            "DengXian",
            "Noto Sans CJK SC",
            "Noto Sans SC",
            "Source Han Sans SC",
            "WenQuanYi Micro Hei"
        };

        private static Font _font;
        private static bool _warnedMissingFont;

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
            return GetResolvedFont();
        }

        public static Font GetResolvedFont()
        {
            if (_font != null)
                return _font;

            for (int i = 0; i < FontCandidates.Length; i++)
            {
                Font candidate = Font.CreateDynamicFontFromOSFont(FontCandidates[i], 18);
                if (CanRenderCjk(candidate))
                {
                    _font = candidate;
                    return _font;
                }
            }

            string[] osFonts = Font.GetOSInstalledFontNames();
            for (int i = 0; i < osFonts.Length; i++)
            {
                Font candidate = Font.CreateDynamicFontFromOSFont(osFonts[i], 18);
                if (CanRenderCjk(candidate))
                {
                    _font = candidate;
                    return _font;
                }
            }

            if (!_warnedMissingFont)
            {
                _warnedMissingFont = true;
                Debug.LogWarning("[UI] No installed CJK UI font can render required Chinese characters.");
            }

            return null;
        }

        private static bool CanRenderCjk(Font font)
        {
            if (font == null)
                return false;

            font.RequestCharactersInTexture(CjkSample, 18, FontStyle.Normal);
            for (int i = 0; i < CjkSample.Length; i++)
            {
                if (!font.HasCharacter(CjkSample[i]))
                    return false;
            }

            return true;
        }
    }
}
