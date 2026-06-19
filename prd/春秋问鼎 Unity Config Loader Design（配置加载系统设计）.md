一、设计目标
Config Loader 是《春秋问鼎》Unity 工程启动后的第一个核心模块。
它的职责是：
将设计阶段的 WorldConfig JSON 静态数据加载到内存，并通过 WorldFactory 创建游戏运行时世界 WorldRuntime。
它是连接：
游戏设计数据
      |
      V
JSON Config
      |
      V
Config Loader
      |
      V
Config Database
      |
      V
World Factory
      |
      V
World Runtime
      |
      V
Game Engine

二、设计原则
1. Config 永远只读
Config 是游戏的基础模板。
例如：
QIN_R01
咸阳地区
初始归属：秦国
初始人口：2500
这些数据：
新游戏时读取； 
创建 Runtime 时复制； 
游戏过程中永远不修改。 
因此：
Config ≠ Runtime

2. Runtime 独立变化
例如：
游戏第 20 年：
QIN_R01

Owner = PLAYER

Population = 6000

Wall Level = 5
这些变化只能存在：
WorldRuntime
而不能写回：
WorldConfig

3. Config 作为数据库使用
运行时：
ConfigDatabase
       |
       +-- NationConfig
       |
       +-- RegionConfig
       |
       +-- SettlementConfig
       |
       +-- BuildingConfig
       |
       +-- BattleConfig
       |
       +-- AIConfig
所有系统可以查询配置。
例如：
BuildingConfig["Wall"]
获得：
防御加成 +50%
升级成本 800钱

三、Unity 配置目录结构
推荐：
Assets
└── Configs
    ├── World
    │    ├── NationConfig.json
    │    ├── RegionConfig.json
    │    ├── SettlementTemplate.json
    │    └── SettlementInstance.json
    │
    ├── Economy
    │    ├── TaxConfig.json
    │    └── PopulationConfig.json
    │
    ├── Military
    │    ├── SoldierConfig.json
    │    └── BattleConfig.json
    │
    ├── Building
    │    └── BuildingConfig.json
    │
    └── AI
         └── AIDecisionConfig.json
这样方便未来扩展：
V2：
CharacterConfig.json

TechnologyConfig.json

TradeConfig.json

四、Config DTO 数据模型设计
Config 只负责保存数据，不包含行为。

4.1 NationConfig
public class NationConfig
{
    public string Id;

    public string Name;

    public NationType Type;
}
例如：
{
    "id":"QIN",
    "name":"秦",
    "type":"Kingdom"
}

4.2 RegionConfig
public class RegionConfig
{
    public string Id;

    public string Name;

    public string CapitalCityId;

    public List<string> SettlementIds;

    public List<string> NeighborRegionIds;
}
例如：
{
 "id":"QIN_R01",
 "name":"咸阳地区",
 "capitalCityId":"CITY_QIN_001",
 "neighborRegionIds":[
     "QIN_R02"
 ]
}

4.3 SettlementTemplateConfig
用于模板。
例如：
普通村：
public class SettlementTemplateConfig
{
    public string Id;

    public int Household;

    public int Population;

    public int Land;

    public int Grain;

    public int Money;

    public int Garrison;
}

模板：
VILLAGE_NORMAL
对应：
100户
500人口
20守军

4.4 SettlementInstanceConfig
具体某一个城村。
例如：
CITY_QIN_001
咸阳
结构：
public class SettlementInstanceConfig
{
    public string Id;

    public string Name;

    public string RegionId;

    public string TemplateId;
}

4.5 BuildingConfig
例如：
public class BuildingConfig
{
    public string Id;

    public string Name;

    public int Cost;

    public float EffectValue;
}
例如：
Wall

Cost = 800

DefenseBonus = 50%

4.6 BattleConfig
例如：
public class BattleConfig
{
    public float AttackCoefficient;

    public float DefenseCoefficient;

    public float CasualtyRate;
}

4.7 AIConfig
保存 AI 行为参数。
例如：
public class AIConfig
{
    public int RecruitThreshold;

    public int AttackThreshold;
}

五、ConfigDatabase 设计
ConfigDatabase 是整个游戏的配置中心。

结构
public class ConfigDatabase
{
    public Dictionary<string, NationConfig> Nations;

    public Dictionary<string, RegionConfig> Regions;

    public Dictionary<string, SettlementTemplateConfig> Templates;

    public Dictionary<string, SettlementInstanceConfig> Settlements;

    public Dictionary<string, BuildingConfig> Buildings;
}

查询方式
例如：
获得秦国：
ConfigDatabase.Instance
    .Nations["QIN"];
获得咸阳：
ConfigDatabase.Instance
    .Settlements["CITY_QIN_001"];

六、ConfigLoader 设计
主要职责
负责：
JSON 文件
      |
      |
反序列化
      |
      V
Config DTO
      |
      V
ConfigDatabase

类设计
public class ConfigLoader
{

    public ConfigDatabase Load()
    {
        LoadNation();

        LoadRegion();

        LoadSettlement();

        LoadBuilding();

        LoadAI();

        return database;
    }

}

七、WorldFactory 设计
Config Loader 不创建游戏世界。
它只负责加载模板。
真正创建游戏：
ConfigDatabase
       |
       V
WorldFactory
       |
       V
WorldRuntime

WorldFactory 职责
创建：
WorldRuntime
|
+-- NationState
|
+-- RegionState
|
+-- SettlementState
|
+-- DiplomacyState
|
+-- GameTime

主要流程
CreateNewGame()
{
    CreateNations();

    CreateRegions();

    CreateSettlements();

    InitializeDiplomacy();

    InitializePlayer();

    return WorldRuntime;
}

八、新游戏完整时序
玩家点击：
【开始游戏】
流程：
GameLauncher
      |
      V
ConfigLoader.Load()
      |
      V
ConfigDatabase
      |
      V
WorldFactory.Create()
      |
      V
WorldRuntime
      |
      V
GameEngine.Initialize()
      |
      V
进入游戏

九、配置校验系统
为了避免配置错误，启动时执行：
ConfigValidator

检查内容：
ID 唯一
例如：
错误：
两个 CITY_QIN_001

引用有效
例如：
Settlement.RegionId
必须存在：
RegionConfig

邻接关系正确
例如：
QIN_R01
邻接 QIN_R02
则：
QIN_R02
也应该邻接 QIN_R01

数值合法
例如：
人口 > 0

粮食 >= 0

守军 >= 0

十、配置版本管理
增加：
{
 "version":"1.0.0"
}
目的：
支持未来升级； 
检测旧配置； 
支持 DLC 或扩展包。 

十一、Unity 工程目录建议
Assets
|
├── Scripts
│
│   ├── Config
│   │     ├── ConfigLoader.cs
│   │     ├── ConfigDatabase.cs
│   │     └── ConfigValidator.cs
│   │
│   ├── Factory
│   │     └── WorldFactory.cs
│
│   └── Runtime
│
└── Configs

十二、与整体架构关系
                 JSON Config
                      |
                ConfigLoader
                      |
                ConfigDatabase
                      |
                 WorldFactory
                      |
                 WorldRuntime
                      |
                  GameEngine
                      |
                  Game Tick

十三、设计总结
Config Loader 的核心思想：
配置负责定义世界，Factory 负责创建世界，Runtime 负责运行世界。
三者严格分离：
WorldConfig
    |
    | 创建
    V
WorldRuntime
    |
    | 运行变化
    V
SaveData
其优势：
方便调整游戏平衡； 
支持多地图； 
支持未来增加人物、科技、贸易系统； 
避免存档污染原始世界数据。