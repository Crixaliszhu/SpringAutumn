using System.Collections.Generic;

namespace SpringAutumn.Config
{
    /// <summary>
    /// 配置数据源抽象。Core 为纯 C#，不负责 JSON 解析；
    /// 由表现层（JsonUtility）或测试侧实现，将 JSON 解析为 Core DTO 后提供给 ConfigLoader。
    /// </summary>
    public interface IConfigSource
    {
        IReadOnlyList<NationConfig> LoadNations();
        IReadOnlyList<RegionConfig> LoadRegions();
        IReadOnlyList<SettlementTemplateConfig> LoadTemplates();
        IReadOnlyList<SettlementInstanceConfig> LoadSettlements();
        IReadOnlyList<BuildingConfig> LoadBuildings();
        EconomyConfig LoadEconomy();
        BattleConfig LoadBattle();
        AIConfig LoadAI();
    }
}
