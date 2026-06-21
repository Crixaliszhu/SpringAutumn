using System.Collections.Generic;
using UnityEngine;

namespace SpringAutumn.Presentation.Map
{
    public static class NationColorPalette
    {
        private static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "QIN", Color.black },
            { "JIN", new Color(0.1f, 0.35f, 0.9f) },
            { "QI", new Color(0.95f, 0.78f, 0.1f) },
            { "CHU", new Color(0.15f, 0.65f, 0.25f) },
            { "ZHOU", new Color(0.85f, 0.1f, 0.1f) },
            { "PLAYER", new Color(0.55f, 0.25f, 0.85f) },
            { "NEUTRAL", Color.gray }
        };

        public static Color Get(string nationId)
        {
            return !string.IsNullOrEmpty(nationId) && Colors.TryGetValue(nationId, out var color)
                ? color
                : Color.gray;
        }
    }
}
