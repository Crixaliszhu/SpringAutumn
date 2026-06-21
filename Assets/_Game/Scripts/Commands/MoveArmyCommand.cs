using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>从据点抽调守军组建野战军，并指定行军目标 Region。</summary>
    public class MoveArmyCommand : GameCommand
    {
        public string SourceSettlementId;
        public string TargetRegionId;
        public int Soldiers;

        private ConfigDatabase _config;

        public MoveArmyCommand() { }

        public MoveArmyCommand(string nationId, string sourceSettlementId, string targetRegionId,
            int soldiers, ConfigDatabase config)
        {
            NationId = nationId;
            SourceSettlementId = sourceSettlementId;
            TargetRegionId = targetRegionId;
            Soldiers = soldiers;
            _config = config;
        }

        public override bool Validate(WorldRuntime world)
        {
            if (_config == null || Soldiers <= 0)
                return false;
            if (!world.Settlements.TryGet(SourceSettlementId, out var source))
                return false;
            if (!world.Regions.TryGet(source.RegionId, out var currentRegion))
                return false;
            if (!world.Regions.Contains(TargetRegionId))
                return false;
            if (source.OwnerId != NationId)
                return false;
            if (!currentRegion.NeighborRegionIds.Contains(TargetRegionId) && source.RegionId != TargetRegionId)
                return false;

            int activeArmies = 0;
            foreach (var army in world.Armies.GetAll())
            {
                if (army.NationId == NationId && army.Status != ArmyStatus.Disbanded)
                    activeArmies++;
            }

            if (activeArmies >= _config.AI.maxArmyCount)
                return false;

            int minGarrison = GetMinGarrison(source, _config.Battle);
            int maxDraw = (int)(source.Garrison * _config.Battle.maxConscriptRate);
            return Soldiers <= maxDraw && source.Garrison - Soldiers >= minGarrison;
        }

        public override void Execute(WorldRuntime world)
        {
            if (!world.Settlements.TryGet(SourceSettlementId, out var source))
                return;

            source.Garrison -= Soldiers;

            var army = new ArmyState(CreateArmyId(world))
            {
                NationId = NationId,
                SourceSettlementId = SourceSettlementId,
                CurrentRegionId = source.RegionId,
                TargetRegionId = TargetRegionId,
                Soldiers = Soldiers,
                Morale = 100,
                Status = source.RegionId == TargetRegionId ? ArmyStatus.Idle : ArmyStatus.Marching
            };

            world.Armies.Add(army);
        }

        private static int GetMinGarrison(SettlementState source, BattleConfig config)
        {
            if (source.Type == SettlementType.Capital) return config.minGarrisonCapital;
            if (source.IsCity) return config.minGarrisonCity;
            return config.minGarrisonVillage;
        }

        private static string CreateArmyId(WorldRuntime world)
        {
            int index = world.Armies.Count + 1;
            string id;
            do
            {
                id = "ARMY_" + index.ToString("0000");
                index++;
            }
            while (world.Armies.Contains(id));
            return id;
        }
    }
}
