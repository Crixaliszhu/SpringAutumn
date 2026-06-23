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
            // 数量限制已临时取消：不限制同时在外的军队数量（后续再恢复 maxArmyCount 限制）。
            return true;
        }

        public static int GetMaxDeployableSoldiers(ConfigDatabase config, SettlementState source)
        {
            if (config == null || source == null)
                return 0;

            // 数量限制已临时取消：可全量派出守军。
            // 后续再恢复抽调比例(maxConscriptRate)与最低驻军(minGarrison)限制（见下方保留的计算逻辑）。
            return source.Garrison > 0 ? source.Garrison : 0;
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
