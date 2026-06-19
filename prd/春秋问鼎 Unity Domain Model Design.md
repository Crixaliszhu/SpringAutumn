《春秋问鼎》Unity Domain Model Design V2（区域化领域对象模型设计）

    

    
    
    一、设计目标

    本版本在 V1 领域模型基础上，引入 Region（区域）概念。

    核心变化：

    Region 成为战略地图单位；

    Settlement 成为经营和战斗单位；

    Nation 不再直接关注单个城村，而通过 Region 进行战略决策；

    解决城村独立导致的势力范围碎片化问题。

    设计目标：

    支撑 V1.0 的 24 个 Region 世界；

    支撑 21 城 + 68 村的地图结构；

    支撑区域战争与国家扩张；

    为未来郡县、道路、地形、贸易系统预留扩展能力。

    

    
    
    二、领域模型整体结构

    新的世界结构：

    World
│
├── GameTime
│
├── Nation
│
├── Region
│
├── Settlement
│    ├── City
│    └── Village
│
├── Army
│
├── Diplomacy
│
├── Command
│
└── GameEvent

    其中：

    World 是整个游戏世界的根对象；

    Nation 表示国家或势力；

    Region 表示战略区域；

    Settlement 表示具体城市和村庄；

    Army 表示地图上的军事力量。

    

    
    
    三、World（世界）

    
    作用

    World 是游戏运行时的全部动态数据容器。

    负责保存：

    当前游戏时间；

    所有势力；

    所有区域；

    所有据点；

    所有军队；

    外交关系；

    世界事件。

    同时也是：

    存档系统的根对象；

    所有 Simulation System 的输入和输出。

    

    
    
    数据结构

    public class World
{
    public GameTime Time;

    public NationRepository Nations;

    public RegionRepository Regions;

    public SettlementRepository Settlements;

    public ArmyRepository Armies;

    public DiplomacyRepository Diplomacy;

    public EventRepository Events;
}

    

    
    
    
    四、Nation（势力）

    
    作用

    表示一个政治势力。

    包括：

    五大诸侯国；

    周王室；

    玩家势力；

    中立势力。

    

    
    
    设计原则

    Nation 不直接保存：

    List<Settlement>

    原因：

    城池可能频繁易主。

    真正的归属关系由：

    Settlement.OwnerId
Region.OwnerId

    进行记录。

    Nation 通过查询 Region 获取自己的势力范围。

    

    
    
    核心数据

    public class Nation
{
    public string Id;

    public string Name;

    public int TreasuryGrain;

    public int TreasuryMoney;

    public NationAIState AIState;
}

    

    
    
    
    五、Region（区域）

    
    作用

    Region 是 V2 新增的核心领域对象。

    它代表一个完整的战略区域。

    例如：

    秦西部 Region

       城
       |
 ┌─────┼─────┐
 村A   村B   村C

    

    
    
    Region 负责

    战略层功能：

    地图势力范围；

    AI 战争目标；

    边境判断；

    国家扩张；

    区域归属。

    

    
    
    数据结构

    public class Region
{
    public string Id;

    public string Name;

    // 当前控制势力
    public string OwnerId;

    // 核心城市
    public string CityId;

    // 附属村庄
    public List<string> VillageIds;

    // 相邻区域
    public List<string> NeighborRegionIds;
}

    

    
    
    
    六、Settlement（据点）

    
    作用

    Settlement 是具体经营对象。

    包括：

    城；

    村。

    玩家在 Settlement 上执行：

    建设；

    征兵；

    驻军；

    生产。

    

    
    
    数据结构

    public class Settlement
{
    public string Id;

    public string Name;

    public SettlementType Type;

    // 所属区域
    public string RegionId;

    // 当前归属势力
    public string OwnerId;

    // 人口数据
    public int Households;

    public int Population;

    public int Land;

    // 资源
    public int Grain;

    public int Money;

    // 社会状态
    public int Loyalty;

    // 军事
    public int Garrison;

    // 战术邻接
    public List<string> NeighborSettlementIds;

    // 建筑
    public List<BuildingInstance> Buildings;
}

    

    
    
    
    七、Region 与 Settlement 的关系

    关系如下：

    Region
│
├── Core City
│
└── Villages

    规则：

    一个 Region 只能有一个核心城市；

    一个 Settlement 只能属于一个 Region；

    一个 Region 可以拥有多个村庄。

    

    
    
    八、区域占领规则

    V1.0 采用：

    村庄削弱，城市决定归属。

    战争流程：

    进攻 Region
     ↓
夺取外围村庄
     ↓
降低区域经济能力
     ↓
围攻核心城市
     ↓
占领核心城市
     ↓
Region 易主
     ↓
所有 Settlement 同步更换 Owner

    

    
    
    九、Army（军队）

    
    作用

    代表地图上的军事单位。

    V1.0：

    和平时期主要是守军；

    战争时期从各 Settlement 抽调士兵组成野战军。

    

    
    
    数据结构

    public class Army
{
    public string Id;

    public string NationId;

    public string CurrentSettlementId;

    public string TargetSettlementId;

    public int Soldiers;

    public int Morale;

    public ArmyStatus Status;
}

    

    
    
    
    十、Command（命令模型）

    原则：

    玩家和 AI 必须使用同一套规则。

    所有行为转化为 Command。

    例如：

    建设；

    征兵；

    调兵；

    攻击；

    外交。

    执行流程：

    Player / AI
      ↓
 GameCommand
      ↓
 GameEngine
      ↓
 System
      ↓
 修改 World

    

    
    
    十一、外交模型

    外交关系存在于 Nation 与 Nation 之间。

    例如：

    秦 —— 晋
秦 —— 楚
晋 —— 齐

    关系影响：

    是否宣战；

    是否结盟；

    AI 威胁判断。

    

    
    数据结构

    public class DiplomacyRelation
{
    public string NationA;

    public string NationB;

    public int RelationValue;
}

    关系范围：

    -100 ~ 100

    

    
    
    
    十二、GameEvent（世界事件）

    用于记录长期影响世界的事件。

    例如：

    丰收；

    饥荒；

    流民；

    叛乱；

    战争。

    事件会影响：

    人口；

    粮食；

    民心；

    外交。

    

    
    
    十三、存档结构变化

    由于增加 Region，存档结构更新为：

    SaveData
│
└── World
     │
     ├── GameTime
     ├── Nation
     ├── Region
     ├── Settlement
     ├── Army
     ├── Diplomacy
     └── Event

    只要恢复 World，即可恢复完整天下状态。

    

    
    
    十四、未来版本扩展接口

    
    V2

    增加：

    Person（人物）；

    General（将领）；

    Technology（科技）；

    Terrain（地形）；

    Road（道路）；

    Trade（贸易）。

    

    
    
    V3

    增加：

    大型战场；

    天气系统；

    补给系统；

    家族系统；

    朝廷政治。

    

    
    
    
    十五、最终领域关系图

    World
│
├── Nation
│
├── Region（战略层）
│       │
│       ├── City
│       └── Village
│
├── Settlement（经营层）
│
├── Army（军事层）
│
├── Diplomacy（关系层）
│
└── Command（行为层）

    

    
    
    十六、V2 领域模型核心思想

    《春秋问鼎》的世界模型由原来的：

    国家 → 城/村

    升级为：

    国家
 ↓
区域（战略）
 ↓
城、村（经营与战斗）

    这一层 Region 的加入，使游戏形成了明确的三层结构：

    Nation 决定天下格局；

    Region 决定战略版图；

    Settlement 决定经济、人口和战争细节。

    它不仅解决 V1 地图碎片化问题，也为未来扩展大型春秋世界奠定稳定的数据基础。