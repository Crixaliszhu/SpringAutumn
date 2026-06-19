using System.Collections.Generic;
using System.Linq;

namespace SpringAutumn.Config
{
    /// <summary>配置校验结果。</summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public void Add(string error) => Errors.Add(error);
        public override string ToString() => string.Join("\n", Errors);
    }

    /// <summary>
    /// 校验配置合法性（需求 2.4、2.5）：重复 ID、无效引用、非法数值、Region 邻接对称。
    /// 直接作用于原始 DTO 列表，可在构建 ConfigDatabase 前检出重复 ID。
    /// </summary>
    public class ConfigValidator
    {
        public ValidationResult Validate(IConfigSource source)
        {
            var result = new ValidationResult();

            var nations = source.LoadNations() ?? new List<NationConfig>();
            var regions = source.LoadRegions() ?? new List<RegionConfig>();
            var templates = source.LoadTemplates() ?? new List<SettlementTemplateConfig>();
            var settlements = source.LoadSettlements() ?? new List<SettlementInstanceConfig>();
            var buildings = source.LoadBuildings() ?? new List<BuildingConfig>();

            var nationIds = CheckDuplicates(nations.Select(n => n.id), "Nation", result);
            var regionIds = CheckDuplicates(regions.Select(r => r.id), "Region", result);
            var templateIds = CheckDuplicates(templates.Select(t => t.id), "Template", result);
            var settlementIds = CheckDuplicates(settlements.Select(s => s.id), "Settlement", result);
            CheckDuplicates(buildings.Select(b => b.id), "Building", result);

            ValidateNationRefs(nations, regionIds, result);
            ValidateRegionRefs(regions, nationIds, settlementIds, result);
            ValidateRegionSymmetry(regions, result);
            ValidateSettlementRefs(settlements, nationIds, regionIds, templateIds, result);
            ValidateTemplateNumbers(templates, result);
            ValidateBuildingNumbers(buildings, result);

            return result;
        }

        private static HashSet<string> CheckDuplicates(
            IEnumerable<string> ids, string label, ValidationResult result)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id))
                {
                    result.Add($"{label} 存在空 ID");
                    continue;
                }
                if (!seen.Add(id))
                {
                    result.Add($"{label} 存在重复 ID: '{id}'");
                }
            }
            return seen;
        }

        private static void ValidateNationRefs(
            IReadOnlyList<NationConfig> nations, HashSet<string> regionIds, ValidationResult result)
        {
            foreach (var n in nations)
            {
                if (!string.IsNullOrEmpty(n.capitalRegionId) && !regionIds.Contains(n.capitalRegionId))
                {
                    result.Add($"Nation '{n.id}' 的 capitalRegionId '{n.capitalRegionId}' 不存在");
                }
            }
        }

        private static void ValidateRegionRefs(
            IReadOnlyList<RegionConfig> regions, HashSet<string> nationIds,
            HashSet<string> settlementIds, ValidationResult result)
        {
            foreach (var r in regions)
            {
                if (!nationIds.Contains(r.ownerId))
                {
                    result.Add($"Region '{r.id}' 的 ownerId '{r.ownerId}' 不存在");
                }
                if (!string.IsNullOrEmpty(r.cityId) && !settlementIds.Contains(r.cityId))
                {
                    result.Add($"Region '{r.id}' 的 cityId '{r.cityId}' 不存在");
                }
                foreach (var v in r.villageIds ?? new List<string>())
                {
                    if (!settlementIds.Contains(v))
                    {
                        result.Add($"Region '{r.id}' 的 villageId '{v}' 不存在");
                    }
                }
            }
        }

        private static void ValidateRegionSymmetry(
            IReadOnlyList<RegionConfig> regions, ValidationResult result)
        {
            var neighborMap = regions.ToDictionary(
                r => r.id,
                r => new HashSet<string>(r.neighborRegionIds ?? new List<string>()));

            foreach (var r in regions)
            {
                foreach (var nb in r.neighborRegionIds ?? new List<string>())
                {
                    if (!neighborMap.TryGetValue(nb, out var nbNeighbors))
                    {
                        result.Add($"Region '{r.id}' 的邻接 '{nb}' 不存在");
                        continue;
                    }
                    if (!nbNeighbors.Contains(r.id))
                    {
                        result.Add($"Region 邻接不对称: '{r.id}' -> '{nb}' 但 '{nb}' 未邻接 '{r.id}'");
                    }
                }
            }
        }

        private static void ValidateSettlementRefs(
            IReadOnlyList<SettlementInstanceConfig> settlements, HashSet<string> nationIds,
            HashSet<string> regionIds, HashSet<string> templateIds, ValidationResult result)
        {
            foreach (var s in settlements)
            {
                if (!nationIds.Contains(s.ownerId))
                {
                    result.Add($"Settlement '{s.id}' 的 ownerId '{s.ownerId}' 不存在");
                }
                if (!regionIds.Contains(s.regionId))
                {
                    result.Add($"Settlement '{s.id}' 的 regionId '{s.regionId}' 不存在");
                }
                if (!templateIds.Contains(s.templateId))
                {
                    result.Add($"Settlement '{s.id}' 的 templateId '{s.templateId}' 不存在");
                }
            }
        }

        private static void ValidateTemplateNumbers(
            IReadOnlyList<SettlementTemplateConfig> templates, ValidationResult result)
        {
            foreach (var t in templates)
            {
                if (t.households <= 0) result.Add($"Template '{t.id}' households 必须 > 0");
                if (t.population <= 0) result.Add($"Template '{t.id}' population 必须 > 0");
                if (t.land < 0) result.Add($"Template '{t.id}' land 不能为负");
                if (t.grain < 0) result.Add($"Template '{t.id}' grain 不能为负");
                if (t.money < 0) result.Add($"Template '{t.id}' money 不能为负");
                if (t.garrison < 0) result.Add($"Template '{t.id}' garrison 不能为负");
                if (t.loyalty < 0 || t.loyalty > 100) result.Add($"Template '{t.id}' loyalty 必须在 0-100");
            }
        }

        private static void ValidateBuildingNumbers(
            IReadOnlyList<BuildingConfig> buildings, ValidationResult result)
        {
            foreach (var b in buildings)
            {
                if (b.cost < 0) result.Add($"Building '{b.id}' cost 不能为负");
                if (b.buildMonths < 0) result.Add($"Building '{b.id}' buildMonths 不能为负");
            }
        }
    }
}
