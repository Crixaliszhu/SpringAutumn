using System;
using System.Collections.Generic;

namespace SpringAutumn.Config
{
    // 说明：以下 DTO 均为 [Serializable] + 公有字段，兼容 Unity JsonUtility 反序列化。
    // 枚举字段以 string 存储，由 ConfigLoader 映射为强类型枚举，避免 JsonUtility 枚举按整数解析的问题。

    [Serializable]
    public class NationConfig
    {
        public string id;
        public string name;
        public string type;       // 映射 NationType
        public string color;      // 势力颜色，如 "#AA0000"
        public string capitalRegionId;
    }

    [Serializable]
    public class RegionConfig
    {
        public string id;
        public string name;
        public string ownerId;
        public string cityId;             // 核心城；中立/玩家区域可为空
        public List<string> villageIds = new List<string>();
        public List<string> neighborRegionIds = new List<string>();
        public bool isFrontier;
        // 天下地图布局坐标（格子单位，x 向右、y 向上）。用于让显示位置贴合拓扑邻接关系。
        // 全部区域坐标均为 0 时，视为未配置，地图回退到旧的序号网格布局。
        public float mapX;
        public float mapY;
    }

    [Serializable]
    public class SettlementTemplateConfig
    {
        public string id;          // 如 CAPITAL / CITY / VILLAGE / PLAYER_VILLAGE / NEUTRAL_VILLAGE
        public string type;        // 映射 SettlementType
        public int households;
        public int population;
        public int land;
        public int grain;
        public int money;
        public int garrison;
        public int loyalty;        // 默认民心
    }

    [Serializable]
    public class SettlementInstanceConfig
    {
        public string id;
        public string name;
        public string templateId;
        public string regionId;
        public string ownerId;
        public List<string> neighborSettlementIds = new List<string>();
    }

    [Serializable]
    public class BuildingConfig
    {
        public string id;          // FARM / HOUSE / GRANARY / TRAINING / WOODWALL / MARKET / OFFICE / BARRACKS / STONEWALL
        public string name;
        public string scope;       // VILLAGE / CITY
        public int cost;
        public int buildMonths;    // 建造所需月数
        public float effectValue;  // 效果数值（如 0.10 表示 +10%）
        public string effectType;  // GRAIN_TAX / MONEY_TAX / POP_CAP / GRAIN_STORE / RECRUIT_SPEED / DEFENSE / CITY_DEFENSE / GARRISON_CAP / GOVERNANCE
    }

    [Serializable]
    public class EconomyConfig
    {
        public int householdSize = 5;
        public int landPerHousehold = 50;
        public int grainPerLandPerYear = 100;
        public float grainTaxRate = 0.15f;
        public int moneyTaxPerPersonPerYear = 2;
        public int soldierGrainPerMonth = 90;
        public int soldierWagePerMonth = 1;
    }

    [Serializable]
    public class BattleConfig
    {
        public float attackCoefficient = 1.0f;
        public float defenseCoefficient = 1.0f;
        // 最低驻军红线
        public int minGarrisonVillage = 10;
        public int minGarrisonCity = 30;
        public int minGarrisonCapital = 50;
        // 抽调上限比例
        public float maxConscriptRate = 0.5f;
        // 占领后民心
        public int captureLoyalty = 60;
        public int loyaltyRecoverPerMonth = 2;
    }

    [Serializable]
    public class AIConfig
    {
        public float warPowerThreshold = 1.3f;
        public int maxArmyCount = 3;
        public float recruitPopulationRate = 0.1f;
        public int buildMoneyThreshold = 500;
        public int foodSafeMonths = 12;
    }
}
