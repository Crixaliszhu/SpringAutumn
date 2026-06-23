using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    public static class MilitaryCommandRules
    {
        public static bool CanReach(WorldRuntime world, SettlementState source, SettlementState target)
        {
            if (world == null || source == null || target == null)
                return false;
            if (source.RegionId == target.RegionId)
                return true;
            return world.Regions.TryGet(source.RegionId, out var sourceRegion)
                && sourceRegion.NeighborRegionIds.Contains(target.RegionId);
        }

        public static int CountActiveArmies(WorldRuntime world, string nationId)
        {
            if (world == null)
                return 0;

            int activeArmies = 0;
            foreach (var army in world.Armies.GetAll())
            {
                if (army.NationId == nationId && army.Status != ArmyStatus.Disbanded)
                    activeArmies++;
            }

            return activeArmies;
        }

        public static bool HasArmyCapacity(WorldRuntime world, ConfigDatabase config, string nationId)
        {
            return config != null && CountActiveArmies(world, nationId) < config.AI.maxArmyCount;
        }

        public static int GetMaxDeployableSoldiers(ConfigDatabase config, SettlementState source)
        {
            if (config == null || source == null)
                return 0;

            int minGarrison = GetMinGarrison(config.Battle, source);
            int maxDraw = (int)(source.Garrison * config.Battle.maxConscriptRate);
            int maxAfterReserve = source.Garrison - minGarrison;
            int maxSoldiers = maxDraw < maxAfterReserve ? maxDraw : maxAfterReserve;
            return maxSoldiers > 0 ? maxSoldiers : 0;
        }

        public static int GetMinGarrison(BattleConfig battle, SettlementState source)
        {
            if (battle == null || source == null)
                return 0;
            if (source.Type == SettlementType.Capital)
                return battle.minGarrisonCapital;
            if (source.IsCity)
                return battle.minGarrisonCity;
            return battle.minGarrisonVillage;
        }
    }
}
