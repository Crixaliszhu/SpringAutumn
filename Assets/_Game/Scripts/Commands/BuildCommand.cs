using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Commands
{
    /// <summary>
    /// 建造命令：请求在指定据点建造建筑（需求 6.1、6.2）。
    /// 命令提交当月仅入队列，不立即生效；建造队列每月递减，归零后完成，效果从下月起生效。
    /// </summary>
    public class BuildCommand : GameCommand
    {
        /// <summary>目标据点 Id。</summary>
        public string SettlementId;

        /// <summary>建筑配置 Id。</summary>
        public string BuildingId;

        private ConfigDatabase _config;
        private EventBus _eventBus;

        public BuildCommand() { }

        public BuildCommand(string nationId, string settlementId, string buildingId,
            ConfigDatabase config, EventBus eventBus = null)
        {
            NationId = nationId;
            SettlementId = settlementId;
            BuildingId = buildingId;
            _config = config;
            _eventBus = eventBus;
        }

        public override bool Validate(WorldRuntime world)
        {
            // 检查据点存在且归属正确
            if (!world.Settlements.TryGet(SettlementId, out var settlement))
                return false;
            if (settlement.OwnerId != NationId)
                return false;

            // 检查建筑配置存在
            if (!_config.Buildings.TryGetValue(BuildingId, out var buildingCfg))
                return false;

            // 检查建筑范围匹配（VILLAGE/CITY）
            if (buildingCfg.scope == "CITY" && !settlement.IsCity)
                return false;
            if (buildingCfg.scope == "VILLAGE" && !settlement.IsVillage)
                return false;

            // 检查资金充足
            if (settlement.Money < buildingCfg.cost)
                return false;

            return true;
        }

        public override void Execute(WorldRuntime world)
        {
            if (!world.Settlements.TryGet(SettlementId, out var settlement))
                return;

            if (!_config.Buildings.TryGetValue(BuildingId, out var buildingCfg))
                return;

            // 扣除资金
            settlement.Money -= buildingCfg.cost;

            // 入建造队列（延迟生效）
            var task = new ConstructionTask(BuildingId, buildingCfg.buildMonths);
            settlement.ConstructionQueue.Add(task);
        }
    }
}
