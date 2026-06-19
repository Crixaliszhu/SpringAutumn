using System.Collections.Generic;
using SpringAutumn.Config;

namespace SpringAutumn.Runtime
{
    /// <summary>据点（城/村）运行时状态：经营、生产、驻军与战斗的核心单位。</summary>
    public class SettlementState : Entity
    {
        public SettlementType Type;
        public string RegionId;
        public string OwnerId;

        // 人口
        public int Households;
        public int Population;
        public int PopulationCap;
        public int Land;

        // 资源
        public int Grain;
        public int Money;

        // 社会
        public int Loyalty;

        // 军事
        public int Garrison;

        // 建筑与队列
        public List<BuildingInstance> Buildings = new List<BuildingInstance>();
        public List<ConstructionTask> ConstructionQueue = new List<ConstructionTask>();
        public List<RecruitTask> RecruitQueue = new List<RecruitTask>();

        // 战术邻接
        public List<string> NeighborSettlementIds = new List<string>();

        public SettlementState() { }

        public SettlementState(string id) : base(id) { }

        public bool IsCity => Type == SettlementType.City || Type == SettlementType.Capital;
        public bool IsVillage => Type == SettlementType.Village;
    }
}
