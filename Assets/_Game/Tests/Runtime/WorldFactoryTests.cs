using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.Config;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Tests.Runtime
{
    /// <summary>WorldFactory 与运行时状态测试（需求 3.3、3.4、3.5、3.6）。</summary>
    public class WorldFactoryTests
    {
        private static string ConfigDir => Path.Combine(Application.dataPath, "_Game", "Config");

        private static ConfigDatabase LoadConfig()
            => new ConfigLoader().Load(JsonConfigSource.FromDirectory(ConfigDir));

        private static WorldRuntime NewWorld() => new WorldFactory().CreateNewWorld(LoadConfig());

        [Test]
        public void NewWorld_HasExpectedEntityCounts()
        {
            var w = NewWorld();
            Assert.AreEqual(7, w.Nations.Count);
            Assert.AreEqual(24, w.Regions.Count);
            Assert.AreEqual(89, w.Settlements.Count);
            Assert.AreEqual(1, w.Time.Year);
            Assert.AreEqual(1, w.Time.Month);
        }

        [Test]
        public void PlayerVillage_HasInitialValues()
        {
            var w = NewWorld();
            var v = w.Settlements.Get("V_PLAYER_001");

            Assert.AreEqual(30, v.Households);
            Assert.AreEqual(150, v.Population);
            Assert.AreEqual(1500, v.Land);
            Assert.AreEqual(50000, v.Grain);
            Assert.AreEqual(300, v.Money);
            Assert.AreEqual(10, v.Garrison);
            Assert.AreEqual(90, v.Loyalty);
            Assert.AreEqual("PLAYER", v.OwnerId);
            Assert.IsTrue(v.IsVillage);
        }

        [Test]
        public void Capital_City_Village_MatchTemplates()
        {
            var w = NewWorld();

            var capital = w.Settlements.Get("CITY_QIN_001"); // 咸阳 CAPITAL
            Assert.AreEqual(SettlementType.Capital, capital.Type);
            Assert.AreEqual(500, capital.Households);
            Assert.AreEqual(2500, capital.Population);
            Assert.AreEqual(100, capital.Garrison);

            var city = w.Settlements.Get("CITY_QIN_002"); // 雍城 CITY
            Assert.AreEqual(SettlementType.City, city.Type);
            Assert.AreEqual(300, city.Households);
            Assert.AreEqual(1500, city.Population);
            Assert.AreEqual(50, city.Garrison);

            var village = w.Settlements.Get("V_QIN_001"); // 普通村 VILLAGE
            Assert.AreEqual(SettlementType.Village, village.Type);
            Assert.AreEqual(100, village.Households);
            Assert.AreEqual(500, village.Population);
            Assert.AreEqual(20, village.Garrison);
        }

        [Test]
        public void OwnerConsistency_SettlementOwnerMatchesRegionOwner()
        {
            var w = NewWorld();
            foreach (var region in w.Regions.GetAll())
            {
                if (region.HasCity)
                {
                    Assert.AreEqual(region.OwnerId, w.Settlements.Get(region.CityId).OwnerId,
                        $"城 {region.CityId} 的 Owner 应与区域 {region.Id} 一致");
                }
                foreach (var vid in region.VillageIds)
                {
                    Assert.AreEqual(region.OwnerId, w.Settlements.Get(vid).OwnerId,
                        $"村 {vid} 的 Owner 应与区域 {region.Id} 一致");
                }
            }
        }

        [Test]
        public void P3_NewWorldOwnerConsistency_AllSettlementsMatchRegionOwner()
        {
            var w = NewWorld();

            foreach (var settlement in w.Settlements.GetAll())
            {
                var region = w.Regions.Get(settlement.RegionId);
                Assert.AreEqual(
                    region.OwnerId,
                    settlement.OwnerId,
                    $"{settlement.Id} 的 Owner 应与所属区域 {region.Id} 一致");
            }
        }

        [Test]
        public void Runtime_IsIsolatedFrom_Config()
        {
            var config = LoadConfig();
            var w = new WorldFactory().CreateNewWorld(config);

            int templateGrain = config.Templates["PLAYER_VILLAGE"].grain;

            // 修改运行时不应影响配置模板
            var v = w.Settlements.Get("V_PLAYER_001");
            v.Grain += 99999;

            Assert.AreEqual(templateGrain, config.Templates["PLAYER_VILLAGE"].grain,
                "修改 Runtime 不应改变 Config 模板数据");
            Assert.AreNotEqual(templateGrain, v.Grain);
        }

        [Test]
        public void Diplomacy_InitializedNeutral_AndSymmetric()
        {
            var w = NewWorld();
            Assert.AreEqual(0, w.Diplomacy.GetRelation("QIN", "JIN"));
            // 对称性：键与顺序无关
            w.Diplomacy.SetRelation("QIN", "JIN", -50);
            Assert.AreEqual(-50, w.Diplomacy.GetRelation("JIN", "QIN"));
        }

        [Test]
        public void Repository_DuplicateId_Throws()
        {
            var repo = new Repository<NationState>();
            repo.Add(new NationState("X"));
            Assert.Throws<System.ArgumentException>(() => repo.Add(new NationState("X")));
        }
    }
}
