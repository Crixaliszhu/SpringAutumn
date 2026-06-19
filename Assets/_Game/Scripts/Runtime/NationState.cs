namespace SpringAutumn.Runtime
{
    /// <summary>国家运行时状态。势力归属通过 Region/Settlement 的 OwnerId 查询，不在此直接持有。</summary>
    public class NationState : Entity
    {
        public int TreasuryGrain;
        public int TreasuryMoney;
        public NationAIState AIState = NationAIState.Developing;
        public WarStatus WarStatus = WarStatus.Peace;

        public NationState() { }

        public NationState(string id) : base(id) { }
    }
}
