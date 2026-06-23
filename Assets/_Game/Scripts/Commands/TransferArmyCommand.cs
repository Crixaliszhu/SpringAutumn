using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>在己方据点之间调拨驻军。</summary>
    public class TransferArmyCommand : GameCommand
    {
        public string SourceSettlementId;
        public string TargetSettlementId;
        public int Soldiers;

        private ConfigDatabase _config;

        public TransferArmyCommand() { }

        public TransferArmyCommand(string nationId, string sourceSettlementId,
            string targetSettlementId, int soldiers, ConfigDatabase config)
        {
            NationId = nationId;
            SourceSettlementId = sourceSettlementId;
            TargetSettlementId = targetSettlementId;
            Soldiers = soldiers;
            _config = config;
        }

        public override bool Validate(WorldRuntime world)
        {
            if (_config == null || world == null || Soldiers <= 0)
                return false;
            if (SourceSettlementId == TargetSettlementId)
                return false;
            if (!world.Settlements.TryGet(SourceSettlementId, out var source)
                || !world.Settlements.TryGet(TargetSettlementId, out var target))
                return false;
            if (source.OwnerId != NationId || target.OwnerId != NationId)
                return false;
            if (!MilitaryCommandRules.CanReach(world, source, target))
                return false;
            if (!MilitaryCommandRules.HasArmyCapacity(world, _config, NationId))
                return false;

            return Soldiers <= MilitaryCommandRules.GetMaxDeployableSoldiers(_config, source);
        }

        public override void Execute(WorldRuntime world)
        {
            if (!world.Settlements.TryGet(SourceSettlementId, out var source)
                || !world.Settlements.TryGet(TargetSettlementId, out var target))
                return;

            source.Garrison -= Soldiers;

            var army = new ArmyState(CreateArmyId(world))
            {
                NationId = NationId,
                SourceSettlementId = SourceSettlementId,
                CurrentRegionId = source.RegionId,
                TargetRegionId = target.RegionId,
                TargetSettlementId = TargetSettlementId,
                Soldiers = Soldiers,
                Morale = 100,
                Mission = ArmyMission.Transfer,
                Status = source.RegionId == target.RegionId ? ArmyStatus.Idle : ArmyStatus.Marching
            };

            world.Armies.Add(army);
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
