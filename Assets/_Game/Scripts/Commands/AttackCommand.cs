using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>命令野战军攻击同 Region 内的目标据点。</summary>
    public class AttackCommand : GameCommand
    {
        public string ArmyId;
        public string TargetSettlementId;

        public AttackCommand() { }

        public AttackCommand(string nationId, string armyId, string targetSettlementId)
        {
            NationId = nationId;
            ArmyId = armyId;
            TargetSettlementId = targetSettlementId;
        }

        public override bool Validate(WorldRuntime world)
        {
            if (!world.Armies.TryGet(ArmyId, out var army))
                return false;
            if (!world.Settlements.TryGet(TargetSettlementId, out var target))
                return false;
            if (army.NationId != NationId || army.Soldiers <= 0)
                return false;
            if (army.Mission != ArmyMission.Attack)
                return false;
            if (target.OwnerId == NationId)
                return false;
            return army.CurrentRegionId == target.RegionId;
        }

        public override void Execute(WorldRuntime world)
        {
            if (!world.Armies.TryGet(ArmyId, out var army))
                return;

            army.TargetSettlementId = TargetSettlementId;
            army.Status = ArmyStatus.Sieging;
        }
    }
}
