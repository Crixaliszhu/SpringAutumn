using NUnit.Framework;
using SpringAutumn.Presentation.UI;
using SpringAutumn.Runtime;

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
    }
}
