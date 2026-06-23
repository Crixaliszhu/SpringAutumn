using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.Config;
using SpringAutumn.Presentation.Config;

namespace SpringAutumn.Tests.Config
{
    /// <summary>配置加载与校验测试（需求 2.2、2.3、2.4、2.5）。</summary>
    public class ConfigLoaderTests
    {
        private static string ConfigDir =>
            Path.Combine(Application.dataPath, "_Game", "Config");

        private static IConfigSource RealSource() => JsonConfigSource.FromDirectory(ConfigDir);

        [Test]
        public void Load_RealConfig_HasExpectedCounts()
        {
            var db = new ConfigLoader().Load(RealSource());

            Assert.AreEqual(7, db.Nations.Count, "势力数应为 7");
            Assert.AreEqual(24, db.Regions.Count, "Region 数应为 24");
            Assert.AreEqual(89, db.Settlements.Count, "据点数应为 89");

            int cities = db.Settlements.Values.Count(s =>
            {
                var t = db.Templates[s.templateId].type;
                return t == "Capital" || t == "City";
            });
            int villages = db.Settlements.Values.Count(s => db.Templates[s.templateId].type == "Village");

            Assert.AreEqual(21, cities, "城市数应为 21");
            Assert.AreEqual(68, villages, "村庄数应为 68");
        }

        [Test]
        public void Database_QueryById_ReturnsCorrectEntities()
        {
            var db = new ConfigLoader().Load(RealSource());

            Assert.AreEqual("秦", db.GetNation("QIN").name);
            Assert.AreEqual(NationType.Kingdom, ConfigDatabase.ParseNationType(db.GetNation("QIN").type));

            var xianyang = db.GetSettlement("CITY_QIN_001");
            Assert.AreEqual("咸阳", xianyang.name);
            Assert.AreEqual("CAPITAL", xianyang.templateId);
            Assert.AreEqual("QIN_R01", xianyang.regionId);

            var qinR01 = db.GetRegion("QIN_R01");
            Assert.AreEqual("CITY_QIN_001", qinR01.cityId);
            Assert.AreEqual(3, qinR01.villageIds.Count);
        }

        [Test]
        public void Validator_RealConfig_IsValid()
        {
            var result = new ConfigValidator().Validate(RealSource());
            Assert.IsTrue(result.IsValid, "真实配置应通过校验，但有错误:\n" + result);
        }

        [Test]
        public void P1_ConfigScaleInvariant_AllSettlementsAreReferencedExactlyOnce()
        {
            var db = new ConfigLoader().Load(RealSource());
            var referencedSettlementIds = new List<string>();

            foreach (var region in db.Regions.Values)
            {
                if (!string.IsNullOrEmpty(region.cityId))
                    referencedSettlementIds.Add(region.cityId);
                referencedSettlementIds.AddRange(region.villageIds);
            }

            CollectionAssert.AreEquivalent(
                db.Settlements.Keys,
                referencedSettlementIds,
                "所有 Settlement 必须且只应被 Region 的 cityId/villageIds 覆盖");
            Assert.AreEqual(
                referencedSettlementIds.Count,
                referencedSettlementIds.Distinct().Count(),
                "Settlement 不应被多个 Region 重复引用");
        }

        [Test]
        public void P2_RegionNeighborSymmetry_AllNeighborEdgesAreBidirectional()
        {
            var db = new ConfigLoader().Load(RealSource());

            foreach (var region in db.Regions.Values)
            {
                foreach (string neighborId in region.neighborRegionIds)
                {
                    Assert.IsTrue(db.Regions.ContainsKey(neighborId), $"邻接区域不存在: {neighborId}");
                    CollectionAssert.Contains(
                        db.Regions[neighborId].neighborRegionIds,
                        region.id,
                        $"{region.id} -> {neighborId} 必须被 {neighborId} 回指");
                }
            }
        }

        [Test]
        public void Validator_DetectsDuplicateNationId()
        {
            var src = new MutableConfigSource(RealSource());
            src.Nations.Add(new NationConfig { id = "QIN", name = "重复秦", type = "Kingdom" });

            var result = new ConfigValidator().Validate(src);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("重复 ID") && e.Contains("QIN")));
        }

        [Test]
        public void Validator_DetectsInvalidSettlementReference()
        {
            var src = new MutableConfigSource(RealSource());
            src.Settlements.Add(new SettlementInstanceConfig
            {
                id = "CITY_BAD_001", name = "坏城", templateId = "NOPE",
                regionId = "NO_REGION", ownerId = "NO_NATION"
            });

            var result = new ConfigValidator().Validate(src);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("templateId") && e.Contains("NOPE")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("regionId") && e.Contains("NO_REGION")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("ownerId") && e.Contains("NO_NATION")));
        }

        [Test]
        public void Validator_DetectsAsymmetricNeighbor()
        {
            var src = new MutableConfigSource(RealSource());
            // 给 QIN_R01 增加一个未回指的邻接
            var qinR01 = src.Regions.First(r => r.id == "QIN_R01");
            qinR01.neighborRegionIds = new List<string>(qinR01.neighborRegionIds) { "QI_R05" };

            var result = new ConfigValidator().Validate(src);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("邻接不对称")));
        }

        [Test]
        public void Loader_DuplicateId_Throws()
        {
            var src = new MutableConfigSource(RealSource());
            src.Regions.Add(new RegionConfig { id = "QIN_R01", name = "重复", ownerId = "QIN" });

            Assert.Throws<ConfigException>(() => new ConfigLoader().Load(src));
        }

        /// <summary>可变内存配置源，用于构造异常数据进行校验测试。</summary>
        private sealed class MutableConfigSource : IConfigSource
        {
            public readonly List<NationConfig> Nations;
            public readonly List<RegionConfig> Regions;
            public readonly List<SettlementTemplateConfig> Templates;
            public readonly List<SettlementInstanceConfig> Settlements;
            public readonly List<BuildingConfig> Buildings;
            private readonly EconomyConfig _economy;
            private readonly BattleConfig _battle;
            private readonly AIConfig _ai;

            public MutableConfigSource(IConfigSource baseSource)
            {
                Nations = new List<NationConfig>(baseSource.LoadNations());
                Regions = baseSource.LoadRegions().Select(Clone).ToList();
                Templates = new List<SettlementTemplateConfig>(baseSource.LoadTemplates());
                Settlements = new List<SettlementInstanceConfig>(baseSource.LoadSettlements());
                Buildings = new List<BuildingConfig>(baseSource.LoadBuildings());
                _economy = baseSource.LoadEconomy();
                _battle = baseSource.LoadBattle();
                _ai = baseSource.LoadAI();
            }

            private static RegionConfig Clone(RegionConfig r) => new RegionConfig
            {
                id = r.id, name = r.name, ownerId = r.ownerId, cityId = r.cityId,
                isFrontier = r.isFrontier,
                mapX = r.mapX, mapY = r.mapY,
                villageIds = new List<string>(r.villageIds),
                neighborRegionIds = new List<string>(r.neighborRegionIds)
            };

            public IReadOnlyList<NationConfig> LoadNations() => Nations;
            public IReadOnlyList<RegionConfig> LoadRegions() => Regions;
            public IReadOnlyList<SettlementTemplateConfig> LoadTemplates() => Templates;
            public IReadOnlyList<SettlementInstanceConfig> LoadSettlements() => Settlements;
            public IReadOnlyList<BuildingConfig> LoadBuildings() => Buildings;
            public EconomyConfig LoadEconomy() => _economy;
            public BattleConfig LoadBattle() => _battle;
            public AIConfig LoadAI() => _ai;
        }
    }
}
