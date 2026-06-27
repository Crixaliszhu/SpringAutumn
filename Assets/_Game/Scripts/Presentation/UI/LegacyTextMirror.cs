using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpringAutumn.Presentation.UI
{
    public static class LegacyTextMirror
    {
        public static Text FromTmp(TMP_Text source, string nameSuffix = "LegacyText")
        {
            if (source == null)
                return null;

            Text target = source.GetComponent<Text>();
            if (target == null)
                target = source.gameObject.AddComponent<Text>();

            target.text = source.text;
            target.fontSize = Mathf.RoundToInt(source.fontSize);
            target.color = source.color;
            target.raycastTarget = source.raycastTarget;
            target.supportRichText = source.richText;
            target.alignment = ConvertAlignment(source.alignment);
            target.horizontalOverflow = source.enableWordWrapping ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            target.verticalOverflow = VerticalWrapMode.Overflow;

            Font font = UiTextFontResolver.GetResolvedFont();
            if (font != null)
                target.font = font;

            source.enabled = false;
            return target;
        }

        public static void SetText(Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static TextAnchor ConvertAlignment(TextAlignmentOptions alignment)
        {
            if ((alignment & TextAlignmentOptions.Top) == TextAlignmentOptions.Top)
            {
                if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
                    return TextAnchor.UpperRight;
                if ((alignment & TextAlignmentOptions.Center) == TextAlignmentOptions.Center)
                    return TextAnchor.UpperCenter;
                return TextAnchor.UpperLeft;
            }

            if ((alignment & TextAlignmentOptions.Bottom) == TextAlignmentOptions.Bottom)
            {
                if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
                    return TextAnchor.LowerRight;
                if ((alignment & TextAlignmentOptions.Center) == TextAlignmentOptions.Center)
                    return TextAnchor.LowerCenter;
                return TextAnchor.LowerLeft;
            }

            if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
                return TextAnchor.MiddleRight;
            if ((alignment & TextAlignmentOptions.Center) == TextAlignmentOptions.Center)
                return TextAnchor.MiddleCenter;
            return TextAnchor.MiddleLeft;
        }
    }
}
