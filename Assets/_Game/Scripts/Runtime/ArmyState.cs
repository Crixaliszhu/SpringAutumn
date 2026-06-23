namespace SpringAutumn.Runtime
{
    /// <summary>野战军运行时状态。和平时期不存在，战时从守军抽调组建。</summary>
    public class ArmyState : Entity
    {
        public string NationId;
        public string SourceSettlementId;
        public string CurrentRegionId;
        public string TargetRegionId;
        public string TargetSettlementId;
        public int Soldiers;
        public int Morale = 100;
        public ArmyStatus Status = ArmyStatus.Idle;
        public ArmyMission Mission = ArmyMission.Attack;

        /// <summary>行军进度（已行进的月份/段数）。</summary>
        public int MoveProgress;

        public ArmyState() { }

        public ArmyState(string id) : base(id) { }

        public bool IsDestroyed => Soldiers <= 0;
    }
}
