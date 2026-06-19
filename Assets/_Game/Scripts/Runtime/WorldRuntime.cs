using SpringAutumn.Core.Engine;
using SpringAutumn.Core.Utils;

namespace SpringAutumn.Runtime
{
    /// <summary>
    /// 运行时世界状态——整个游戏的单一真实来源（Single Source of Truth）。
    /// 所有 System 读写此对象，存档系统亦以此为根。
    /// </summary>
    public class WorldRuntime
    {
        public GameTimeState Time = new GameTimeState();
        public Repository<NationState> Nations = new Repository<NationState>();
        public Repository<RegionState> Regions = new Repository<RegionState>();
        public Repository<SettlementState> Settlements = new Repository<SettlementState>();
        public Repository<ArmyState> Armies = new Repository<ArmyState>();
        public DiplomacyState Diplomacy = new DiplomacyState();
        public CommandQueue Commands = new CommandQueue();
        public EventState Events = new EventState();
        public DeterministicRandom Random = new DeterministicRandom(1u);
    }
}
