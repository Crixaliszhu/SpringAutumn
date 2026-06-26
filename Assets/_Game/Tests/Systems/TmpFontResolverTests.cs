using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using SpringAutumn.Presentation.UI;

namespace SpringAutumn.Tests.Systems
{
    public class TmpFontResolverTests
    {
        [Test]
        public void Apply_KeepsExistingPrimaryFontAsset()
        {
            var go = new GameObject("TMP Test");
            try
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                TMP_FontAsset original = text.font;

                TmpFontResolver.Apply(text);

                Assert.AreSame(original, text.font, "主字体不能被 CJK fallback 替换，否则数字和英文可能缺字。");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Apply_ReplacesUnusablePrimaryFontWithDefaultFontAsset()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            Assert.NotNull(defaultFont, "测试需要 TMP 默认字体资产。");
            TMP_FontAsset brokenFont = CreateBrokenDynamicFontAsset();

            var go = new GameObject("TMP CJK Primary Test");
            try
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = brokenFont;

                TmpFontResolver.Apply(text);

                Assert.AreSame(defaultFont, text.font, "场景里被序列化为 CJK 主字体的 TMP 文本需要恢复为默认主字体。");
            }
            finally
            {
                Object.DestroyImmediate(brokenFont);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Apply_ReplacesMissingPrimaryFontWithDefaultFontAsset()
        {
            TMP_FontAsset cjkFont = Resources.Load<TMP_FontAsset>("Fonts/SpringAutumn CJK SDF");
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            Assert.NotNull(cjkFont, "测试需要项目内的 CJK TMP 字体资产。");
            Assert.NotNull(defaultFont, "测试需要 TMP 默认字体资产。");

            var go = new GameObject("TMP Missing Primary Test");
            try
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                typeof(TMP_Text)
                    .GetField("m_fontAsset", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(text, null);
                Assert.IsNull(text.font);

                TmpFontResolver.Apply(text);

                Assert.AreSame(defaultFont, text.font, "场景里丢失主字体引用的 TMP 文本需要恢复为默认主字体。");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Apply_RemovesUnusableFallbackFontAssets()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            Assert.NotNull(defaultFont, "测试需要 TMP 默认字体资产。");
            TMP_FontAsset brokenFont = CreateBrokenDynamicFontAsset();

            var go = new GameObject("TMP Broken Fallback Test");
            try
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                text.font = defaultFont;
                if (defaultFont.fallbackFontAssetTable == null)
                    defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                defaultFont.fallbackFontAssetTable.Add(brokenFont);

                TmpFontResolver.Apply(text);

                CollectionAssert.DoesNotContain(defaultFont.fallbackFontAssetTable, brokenFont);
            }
            finally
            {
                if (defaultFont != null && defaultFont.fallbackFontAssetTable != null)
                    defaultFont.fallbackFontAssetTable.Remove(brokenFont);
                Object.DestroyImmediate(brokenFont);
                Object.DestroyImmediate(go);
            }
        }

        private static TMP_FontAsset CreateBrokenDynamicFontAsset()
        {
            TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = "Broken Runtime TMP Font";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            return fontAsset;
        }
    }
}
