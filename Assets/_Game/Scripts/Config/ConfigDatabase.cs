using System;
using System.Collections.Generic;

namespace SpringAutumn.Config
{
    /// <summary>
    /// 配置中心：以字典提供 O(1) 查询。运行期只读，不被任何系统修改（需求 2.3、2.6）。
    /// </summary>
    public class ConfigDatabase
    {
        public IReadOnlyDictionary<string, NationConfig> Nations { get; }
        public IReadOnlyDictionary<string, RegionConfig> Regions { get; }
        public IReadOnlyDictionary<string, SettlementTemplateConfig> Templates { get; }
        public IReadOnlyDictionary<string, SettlementInstanceConfig> Settlements { get; }
        public IReadOnlyDictionary<string, BuildingConfig> Buildings { get; }
        public EconomyConfig Economy { get; }
        public BattleConfig Battle { get; }
        public AIConfig AI { get; }

        public ConfigDatabase(
            Dictionary<string, NationConfig> nations,
            Dictionary<string, RegionConfig> regions,
            Dictionary<string, SettlementTemplateConfig> templates,
            Dictionary<string, SettlementInstanceConfig> settlements,
            Dictionary<string, BuildingConfig> buildings,
            EconomyConfig economy,
            BattleConfig battle,
            AIConfig ai)
        {
            Nations = nations;
            Regions = regions;
            Templates = templates;
            Settlements = settlements;
            Buildings = buildings;
            Economy = economy;
            Battle = battle;
            AI = ai;
        }

        public NationConfig GetNation(string id) => Nations[id];
        public RegionConfig GetRegion(string id) => Regions[id];
        public SettlementInstanceConfig GetSettlement(string id) => Settlements[id];

        /// <summary>将 NationConfig.type 字符串映射为强类型枚举。</summary>
        public static NationType ParseNationType(string value)
        {
            return Enum.TryParse(value, ignoreCase: true, out NationType t)
                ? t
                : throw new FormatException($"未知 NationType: '{value}'");
        }

        /// <summary>将 type 字符串映射为强类型 SettlementType。</summary>
        public static SettlementType ParseSettlementType(string value)
        {
            return Enum.TryParse(value, ignoreCase: true, out SettlementType t)
                ? t
                : throw new FormatException($"未知 SettlementType: '{value}'");
        }
    }
}
