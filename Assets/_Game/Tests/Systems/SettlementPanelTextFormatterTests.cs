using System.Collections.Generic;
using NUnit.Framework;
using SpringAutumn.Config;
using SpringAutumn.Presentation.UI;
using SpringAutumn.Runtime;

namespace SpringAutumn.Tests.Systems
{
    public class SettlementPanelTextFormatterTests
    {
        [Test]
        public void FormatBody_ShowsLoyaltyFarmLevelEffectAndConstructionQueue()
        {
            var config = new ConfigDatabase(
                new Dictionary<string, NationConfig>(),
                new Dictionary<string, RegionConfig>(),
                new Dictionary<string, SettlementTemplateConfig>(),
                new Dictionary<string, SettlementInstanceConfig>(),
                new Dictionary<string, BuildingConfig>
                {
                    ["FARM"] = new BuildingConfig
                    {
                        id = "FARM",
                        name = "农田",
                        effectType = "GRAIN_TAX",
                        effectValue = 0.10f
                    }
                },
                new EconomyConfig(),
                new BattleConfig(),
                new AIConfig());
            var settlement = new SettlementState("V_TEST")
            {
                Type = SettlementType.Village,
                OwnerId = "PLAYER",
                Population = 150,
                Grain = 50000,
                Money = 300,
                Garrison = 10,
                Loyalty = 90
            };
            settlement.Buildings.Add(new BuildingInstance("FARM", 2));
            settlement.ConstructionQueue.Add(new ConstructionTask("FARM", 1));

            string body = SettlementPanelTextFormatter.FormatBody(settlement, config, "PLAYER");

            StringAssert.Contains("民心：90", body);
            StringAssert.Contains("农田 Lv.2：粮税 +20%", body);
            StringAssert.Contains("建设：农田 剩余 1 月", body);
            Assert.LessOrEqual(body.Split('\n').Length, 7, "据点详情不能挤占底部状态文案区域");
        }
    }
}
