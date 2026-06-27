using UnityEngine;

namespace SpringAutumn.Presentation.UI
{
    /// <summary>表现层面板布局小工具：在运行时统一把面板吸附到屏幕角并按比例缩放。</summary>
    public static class UiPanelLayout
    {
        /// <summary>将面板吸附到画布右上角。anchoredPosition 为相对右上角的偏移（x 向左为负、y 向下为负）。</summary>
        public static void AnchorTopRight(RectTransform rect, Vector2 anchoredPosition, float scale)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            if (scale > 0f)
                rect.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>将面板吸附到画布左上角。anchoredPosition 为相对左上角的偏移（x 向右为正、y 向下为负）。</summary>
        public static void AnchorTopLeft(RectTransform rect, Vector2 anchoredPosition, float scale)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            if (scale > 0f)
                rect.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// 将面板设为画布左侧的纵向列：从顶部下方 <paramref name="topOffset"/> 处一直拉伸到
        /// 距底部 <paramref name="bottomMargin"/> 处，宽度固定 <paramref name="width"/>。
        /// 用于把弹窗放在左侧日志区下方并向下拉长到接近屏幕底部（适配不同机型高度）。
        /// </summary>
        public static void AnchorLeftColumn(RectTransform rect, float topOffset, float leftMargin, float width, float bottomMargin)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(leftMargin, bottomMargin);
            rect.offsetMax = new Vector2(leftMargin + width, -topOffset);
            rect.localScale = Vector3.one;
        }
    }
}
