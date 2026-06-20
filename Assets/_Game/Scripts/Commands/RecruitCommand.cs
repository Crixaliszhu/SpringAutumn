using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>
    /// 征兵命令：请求在指定据点征召士兵（需求 6.1、6.3）。
    /// 命令提交当月仅入训练队列，守军不变；训练期结束后守军增加，新兵当月不可参战。
    /// </summary>
    public class RecruitCommand : GameCommand
    {
        /// <summary>目标据点 Id。</summary>
        public string SettlementId;

        /// <summary>征兵数量。</summary>
        public int Count;

        private ConfigDatabase _config;

        public RecruitCommand() { }

        public RecruitCommand(string nationId, string settlementId, int count, ConfigDatabase config)
        {
            NationId = nationId;
            SettlementId = settlementId;
            Count = count;
            _config = config;
        }

        public override bool Validate(WorldRuntime world)
        {
            // 检查据点存在且归属正确
            if (!world.Settlements.TryGet(SettlementId, out var settlement))
                return false;
            if (settlement.OwnerId != NationId)
                return false;

            // 检查征兵数量合法
            if (Count <= 0)
                return false;

            // 计算征兵费用（每人 10 钱 + 100 斤粮）
            int costMoney = Count * 10;
            int costGrain = Count * 100;

            // 检查资源充足
            if (settlement.Money < costMoney)
                return false;
            if (settlement.Grain < costGrain)
                return false;

            // 检查人口基数（至少 1 人/10 士兵）
            int minPop = Count / 10;
            if (settlement.Population < minPop)
                return false;

            return true;
        }

        public override void Execute(WorldRuntime world)
        {
            if (!world.Settlements.TryGet(SettlementId, out var settlement))
                return;

            // 扣除资源
            int costMoney = Count * 10;
            int costGrain = Count * 100;
            settlement.Money -= costMoney;
            settlement.Grain -= costGrain;

            // 入训练队列（默认训练期 2 个月）
            int trainMonths = 2;
            // 检查是否有训练场加速
            foreach (var b in settlement.Buildings)
            {
                if (b.BuildingId == "TRAINING")
                {
                    // 训练场减少训练时间（每级 -0.5 月，最少 1 月）
                    trainMonths = System.Math.Max(1, trainMonths - b.Level / 2);
                }
            }

            var task = new RecruitTask(Count, trainMonths);
            settlement.RecruitQueue.Add(task);
        }
    }
}
