using System.Collections.Generic;
using SpringAutumn.Config;
using SpringAutumn.Core.Utils;

namespace SpringAutumn.Runtime
{
    /// <summary>
    /// 由只读配置创建运行时世界（需求 3.3、3.4、3.6）。
    /// 模板值以副本写入 Runtime，保证 Runtime 与 Config 数据隔离（需求 3.5）。
    /// </summary>
    public class WorldFactory
    {
        public WorldRuntime CreateNewWorld(ConfigDatabase config, uint seed = 1u)
        {
            var world = new WorldRuntime
            {
                Time = new GameTimeState(1, 1),
                Random = new DeterministicRandom(seed)
            };

            CreateNations(config, world);
            CreateSettlements(config, world);
            CreateRegions(config, world);
            InitializeDiplomacy(config, world);

            GameLogger.Log(LogModule.Runtime,
                $"new world created: nations={world.Nations.Count} regions={world.Regions.Count} settlements={world.Settlements.Count}");

            return world;
        }

        private static void CreateNations(ConfigDatabase config, WorldRuntime world)
        {
            foreach (var nc in config.Nations.Values)
            {
                world.Nations.Add(new NationState(nc.id)
                {
                    TreasuryGrain = 0,
                    TreasuryMoney = 0,
                    AIState = NationAIState.Developing,
                    WarStatus = WarStatus.Peace
                });
            }
        }

        private static void CreateSettlements(ConfigDatabase config, WorldRuntime world)
        {
            foreach (var sc in config.Settlements.Values)
            {
                var tpl = config.Templates[sc.templateId];
                var s = new SettlementState(sc.id)
                {
                    Type = ConfigDatabase.ParseSettlementType(tpl.type),
                    RegionId = sc.regionId,
                    OwnerId = sc.ownerId,
                    Households = tpl.households,
                    Population = tpl.population,
                    PopulationCap = tpl.population,
                    Land = tpl.land,
                    Grain = tpl.grain,
                    Money = tpl.money,
                    Loyalty = tpl.loyalty,
                    Garrison = tpl.garrison,
                    NeighborSettlementIds = new List<string>(sc.neighborSettlementIds ?? new List<string>())
                };
                world.Settlements.Add(s);
            }
        }

        private static void CreateRegions(ConfigDatabase config, WorldRuntime world)
        {
            foreach (var rc in config.Regions.Values)
            {
                world.Regions.Add(new RegionState(rc.id)
                {
                    OwnerId = rc.ownerId,
                    IsFrontier = rc.isFrontier,
                    CityId = rc.cityId,
                    VillageIds = new List<string>(rc.villageIds ?? new List<string>()),
                    NeighborRegionIds = new List<string>(rc.neighborRegionIds ?? new List<string>())
                });
            }
        }

        private static void InitializeDiplomacy(ConfigDatabase config, WorldRuntime world)
        {
            // 初始化所有势力两两关系为 0（中立）。
            var ids = new List<string>();
            foreach (var n in config.Nations.Values)
            {
                ids.Add(n.id);
            }
            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = i + 1; j < ids.Count; j++)
                {
                    world.Diplomacy.SetRelation(ids[i], ids[j], 0);
                }
            }
        }
    }
}
