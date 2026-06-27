using NUnit.Framework;
using SpringAutumn.Presentation.UI;
using SpringAutumn.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SpringAutumn.Tests.Systems
{
    public class HudMessageTextTests
    {
        [Test]
        public void FormatResources_UsesCompactSingleLineText()
        {
            string text = HudView.FormatResources(299690, 2392, 400, 121, 3);

            Assert.AreEqual("粮:299690 钱:2392 人:400 兵:121 郡:3", text);
            Assert.IsFalse(text.Contains("Region"));
        }

        [Test]
        public void FormatDate_IncludesYearAndMonthValues()
        {
            string text = HudView.FormatDate(new GameTimeState(3, 8));

            Assert.AreEqual("第3年8月", text);
        }

        [Test]
        public void Bind_RepositionsMenuButtonsInsideMenuPanel()
        {
            var hudObject = new GameObject("Hud");
            try
            {
                var hud = hudObject.AddComponent<HudView>();
                var menuPanel = new GameObject("MenuPanel", typeof(RectTransform));
                menuPanel.transform.SetParent(hudObject.transform, false);
                var panelRect = menuPanel.GetComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(260f, 220f);

                Button saveButton = CreateBrokenMenuButton("SaveButton", menuPanel.transform, new Vector2(26f, -58f));
                Button loadButton = CreateBrokenMenuButton("LoadButton", menuPanel.transform, new Vector2(138f, -58f));
                Button closeButton = CreateBrokenMenuButton("CloseMenuButton", menuPanel.transform, new Vector2(82f, -104f));

                SetField(hud, "menuPanel", menuPanel);
                SetField(hud, "saveButton", saveButton);
                SetField(hud, "loadButton", loadButton);
                SetField(hud, "closeMenuButton", closeButton);

                hud.Bind(null);

                AssertMenuButtonRect(saveButton.GetComponent<RectTransform>(), new Vector2(26f, -58f));
                AssertMenuButtonRect(loadButton.GetComponent<RectTransform>(), new Vector2(138f, -58f));
                AssertMenuButtonRect(closeButton.GetComponent<RectTransform>(), new Vector2(82f, -104f));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
            }
        }

        private static Button CreateBrokenMenuButton(string name, Transform parent, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(96f, 34f);
            return buttonObject.GetComponent<Button>();
        }

        private static void AssertMenuButtonRect(RectTransform rect, Vector2 anchoredPosition)
        {
            Assert.AreEqual(new Vector2(0f, 1f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0f, 1f), rect.anchorMax);
            Assert.AreEqual(new Vector2(0f, 1f), rect.pivot);
            Assert.AreEqual(anchoredPosition, rect.anchoredPosition);
            Assert.AreEqual(new Vector2(96f, 34f), rect.sizeDelta);

            float left = rect.anchoredPosition.x;
            float right = rect.anchoredPosition.x + rect.sizeDelta.x;
            Assert.GreaterOrEqual(left, 0f);
            Assert.LessOrEqual(right, 260f);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
