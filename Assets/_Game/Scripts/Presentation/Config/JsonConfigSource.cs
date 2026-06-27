using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SpringAutumn.Config;

namespace SpringAutumn.Presentation.Config
{
    /// <summary>
    /// 基于 Unity JsonUtility 的配置数据源实现。负责把 JSON 文本解析为 Core DTO，
    /// Core 侧 ConfigLoader/ConfigValidator 再据此工作（保持 Core 不依赖 Unity）。
    /// </summary>
    public class JsonConfigSource : IConfigSource
    {
        [Serializable] private class NationList { public List<NationConfig> nations; }
        [Serializable] private class RegionList { public List<RegionConfig> regions; }
        [Serializable] private class TemplateList { public List<SettlementTemplateConfig> templates; }
        [Serializable] private class SettlementList { public List<SettlementInstanceConfig> settlements; }
        [Serializable] private class BuildingList { public List<BuildingConfig> buildings; }

        private readonly List<NationConfig> _nations;
        private readonly List<RegionConfig> _regions;
        private readonly List<SettlementTemplateConfig> _templates;
        private readonly List<SettlementInstanceConfig> _settlements;
        private readonly List<BuildingConfig> _buildings;
        private readonly EconomyConfig _economy;
        private readonly BattleConfig _battle;
        private readonly AIConfig _ai;

        public JsonConfigSource(
            string nationsJson, string regionsJson, string templatesJson, string settlementsJson,
            string buildingsJson, string economyJson, string battleJson, string aiJson)
        {
            _nations = JsonUtility.FromJson<NationList>(nationsJson).nations;
            _regions = JsonUtility.FromJson<RegionList>(regionsJson).regions;
            _templates = JsonUtility.FromJson<TemplateList>(templatesJson).templates;
            _settlements = JsonUtility.FromJson<SettlementList>(settlementsJson).settlements;
            _buildings = JsonUtility.FromJson<BuildingList>(buildingsJson).buildings;
            _economy = JsonUtility.FromJson<EconomyConfig>(economyJson);
            _battle = JsonUtility.FromJson<BattleConfig>(battleJson);
            _ai = JsonUtility.FromJson<AIConfig>(aiJson);
        }

        /// <summary>从目录读取 8 个 JSON 文件构建数据源（编辑器/桌面端可用）。</summary>
        public static JsonConfigSource FromDirectory(string dir)
        {
            string Read(string file) => File.ReadAllText(Path.Combine(dir, file));
            return new JsonConfigSource(
                Read("Nations.json"), Read("Regions.json"), Read("SettlementTemplates.json"),
                Read("Settlements.json"), Read("Buildings.json"), Read("Economy.json"),
                Read("Battle.json"), Read("AI.json"));
        }

        /// <summary>
        /// 从 Resources 目录加载 8 个 JSON 文本资源构建数据源。
        /// 跨平台同步可用（含微信小游戏/WebGL 沙盒），文件需置于
        /// <c>Assets/_Game/Resources/&lt;resourceDir&gt;</c> 下，且不带扩展名引用。
        /// </summary>
        public static JsonConfigSource FromResources(string resourceDir)
        {
            string Read(string file)
            {
                string path = string.IsNullOrEmpty(resourceDir) ? file : resourceDir + "/" + file;
                var asset = Resources.Load<TextAsset>(path);
                if (asset == null)
                    throw new FileNotFoundException("缺少配置资源（Resources）：" + path);
                return asset.text;
            }

            return new JsonConfigSource(
                Read("Nations"), Read("Regions"), Read("SettlementTemplates"),
                Read("Settlements"), Read("Buildings"), Read("Economy"),
                Read("Battle"), Read("AI"));
        }

        public IReadOnlyList<NationConfig> LoadNations() => _nations;
        public IReadOnlyList<RegionConfig> LoadRegions() => _regions;
        public IReadOnlyList<SettlementTemplateConfig> LoadTemplates() => _templates;
        public IReadOnlyList<SettlementInstanceConfig> LoadSettlements() => _settlements;
        public IReadOnlyList<BuildingConfig> LoadBuildings() => _buildings;
        public EconomyConfig LoadEconomy() => _economy;
        public BattleConfig LoadBattle() => _battle;
        public AIConfig LoadAI() => _ai;
    }
}
