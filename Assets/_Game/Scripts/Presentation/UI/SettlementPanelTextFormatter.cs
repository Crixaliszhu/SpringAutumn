using System.Globalization;
using System.Text;
using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.UI
{
    public static class SettlementPanelTextFormatter
    {
        public static string FormatBody(SettlementState settlement, ConfigDatabase config, string playerNationId)
        {
            if (settlement == null)
                return string.Empty;

            string ownerLine = settlement.OwnerId == playerNationId ? "可操作" : "仅可查看";
            var body = new StringBuilder();
            body.AppendLine($"所属：{settlement.OwnerId}（{ownerLine}）");
            body.AppendLine($"人口：{settlement.Population}");
            body.AppendLine($"粮：{settlement.Grain}  钱：{settlement.Money}");
            body.AppendLine($"民心：{settlement.Loyalty}  守军：{settlement.Garrison}");
            body.AppendLine(FormatBuildings(settlement, config));
            body.AppendLine(FormatConstructionQueue(settlement, config));
            body.Append($"征兵：{settlement.RecruitQueue.Count}");
            return body.ToString();
        }

        public static string GetBuildingName(ConfigDatabase config, string buildingId)
        {
            if (config != null
                && !string.IsNullOrEmpty(buildingId)
                && config.Buildings.TryGetValue(buildingId, out var building)
                && !string.IsNullOrEmpty(building.name))
            {
                return building.name;
            }

            return buildingId;
        }

        private static string FormatBuildings(SettlementState settlement, ConfigDatabase config)
        {
            if (settlement.Buildings == null || settlement.Buildings.Count == 0)
                return "建筑：无";

            var text = new StringBuilder("建筑：");
            for (int i = 0; i < settlement.Buildings.Count; i++)
            {
                var building = settlement.Buildings[i];
                if (i > 0)
                    text.Append("；");

                text.Append(FormatBuilding(building, config));
            }

            return text.ToString();
        }

        private static string FormatBuilding(BuildingInstance building, ConfigDatabase config)
        {
            string name = GetBuildingName(config, building.BuildingId);
            string effect = FormatEffect(config, building.BuildingId, building.Level);
            return string.IsNullOrEmpty(effect)
                ? $"{name} Lv.{building.Level}"
                : $"{name} Lv.{building.Level}：{effect}";
        }

        private static string FormatConstructionQueue(SettlementState settlement, ConfigDatabase config)
        {
            if (settlement.ConstructionQueue == null || settlement.ConstructionQueue.Count == 0)
                return "建设：无";

            var text = new StringBuilder("建设：");
            for (int i = 0; i < settlement.ConstructionQueue.Count; i++)
            {
                var task = settlement.ConstructionQueue[i];
                if (i > 0)
                    text.Append("；");

                text.Append($"{GetBuildingName(config, task.BuildingId)} 剩余 {task.RemainingMonths} 月");
            }

            return text.ToString();
        }

        private static string FormatEffect(ConfigDatabase config, string buildingId, int level)
        {
            if (config == null || !config.Buildings.TryGetValue(buildingId, out var building))
                return string.Empty;

            float value = building.effectValue * level;
            switch (building.effectType)
            {
                case "GRAIN_TAX":
                    return $"粮税 +{FormatPercent(value)}";
                case "MONEY_TAX":
                    return $"钱税 +{FormatPercent(value)}";
                case "POP_CAP":
                    return $"人口上限 +{FormatNumber(value)}";
                case "GRAIN_STORE":
                    return $"粮仓容量 +{FormatNumber(value)}";
                case "RECRUIT_SPEED":
                    return $"征兵速度 +{FormatPercent(value)}";
                case "DEFENSE":
                case "CITY_DEFENSE":
                    return $"防御 +{FormatPercent(value)}";
                case "GARRISON_CAP":
                    return $"驻军上限 +{FormatNumber(value)}";
                case "GOVERNANCE":
                    return $"治理 +{FormatNumber(value)}";
                default:
                    return string.Empty;
            }
        }

        private static string FormatPercent(float value)
        {
            return (value * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
