using System;
using System.Collections.Generic;
using SpringAutumn.Core.Utils;

namespace SpringAutumn.Config
{
    /// <summary>配置加载异常。</summary>
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }

    /// <summary>
    /// 从 IConfigSource 读取 DTO 并组装为 ConfigDatabase（需求 2.2、2.3）。
    /// 加载阶段仅负责构建；完整合法性校验由 ConfigValidator 负责。
    /// </summary>
    public class ConfigLoader
    {
        public ConfigDatabase Load(IConfigSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var nations = BuildIndex(source.LoadNations(), n => n.id, "Nation");
            var regions = BuildIndex(source.LoadRegions(), r => r.id, "Region");
            var templates = BuildIndex(source.LoadTemplates(), t => t.id, "SettlementTemplate");
            var settlements = BuildIndex(source.LoadSettlements(), s => s.id, "Settlement");
            var buildings = BuildIndex(source.LoadBuildings(), b => b.id, "Building");

            var economy = source.LoadEconomy() ?? new EconomyConfig();
            var battle = source.LoadBattle() ?? new BattleConfig();
            var ai = source.LoadAI() ?? new AIConfig();

            GameLogger.Log(LogModule.Config,
                $"loaded nations={nations.Count} regions={regions.Count} settlements={settlements.Count}");

            return new ConfigDatabase(nations, regions, templates, settlements, buildings, economy, battle, ai);
        }

        private static Dictionary<string, T> BuildIndex<T>(
            IReadOnlyList<T> items, Func<T, string> keySelector, string label)
        {
            var dict = new Dictionary<string, T>();
            if (items == null)
            {
                return dict;
            }

            foreach (var item in items)
            {
                string key = keySelector(item);
                if (string.IsNullOrEmpty(key))
                {
                    throw new ConfigException($"{label} 存在空 ID");
                }
                if (dict.ContainsKey(key))
                {
                    throw new ConfigException($"{label} 存在重复 ID: '{key}'");
                }
                dict.Add(key, item);
            }
            return dict;
        }
    }
}
