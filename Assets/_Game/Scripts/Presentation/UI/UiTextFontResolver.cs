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

            // 优先使用打进包里的「非动态预烘焙中文字体」（微信小游戏/WebGL 真机唯一可靠来源）。
            Font baked = Resources.Load<Font>("Fonts/SpringAutumnLocalCJK");
            if (baked != null)
            {
                _font = baked;
                return _font;
            }

            // 回退：编辑器/桌面端可用的 OS 动态字体（真机沙盒不可用，仅便于编辑器内预览）。
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
                Debug.LogWarning("[UI] 未找到可渲染中文的字体（缺少预烘焙字体且无可用 OS 中文字体）。");
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
