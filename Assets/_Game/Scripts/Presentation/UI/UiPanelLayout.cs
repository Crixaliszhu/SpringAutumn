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
    }
}
