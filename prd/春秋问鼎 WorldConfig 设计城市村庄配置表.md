《春秋问鼎》WorldConfig V2（区域化世界静态数据设计）

    

    
    
    一、设计目标

    WorldConfig 用于定义《春秋问鼎》世界的初始静态数据。

    它决定：

    世界有哪些国家；

    地图由哪些区域组成；

    每个区域包含哪些城和村；

    各区域之间如何连接；

    各城村的初始人口、资源、守军等数据。

    WorldConfig 只用于：

    创建新游戏；

    作为世界初始模板。

    游戏运行后，所有变化进入 World State，不再修改 Config。

    

    
    
    二、世界层级结构

    V2 架构下，地图采用区域模型：

    World
 |
 Nation（势力）
 |
 Region（战略区域）
 |
 Settlement（具体据点）
       |
       ├── City（城）
       |
       └── Village（村）

    设计原则：

    Region 是战略单位，Settlement 是经营、生产和战斗单位。

    

    
    
    三、世界规模

    
    3.1 势力与 Region 数量

    势力
Region
周王室
1
秦
5
晋
5
齐
5
楚
5
玩家流民势力
1
中立势力
2
总计
24

    

    
    
    3.2 Settlement 数量

    类型
数量
国都
5
普通城
16
玩家流民村
1
普通村
67
总计
89

    说明：

    五大国家与周王室共有 21 个 Region；

    每个国家 Region 通常由：

    1 座城市；

    3～4 个附属村庄；

    玩家初始拥有一个独立流民 Region；

    中立拥有两个边境 Region。

    

    
    
    
    四、Region 数据模型

    Region 是地图战略单位。

    主要负责：

    势力边界；

    战略价值；

    AI 目标选择；

    地图颜色显示；

    区域归属判定。

    
    RegionConfig 结构

    {
  "id": "QIN_R01",
  "name": "秦西部地区",

  "ownerId": "QIN",

  "capitalSettlementId": "CITY_QIN_001",

  "villageIds": [
    "V_QIN_001",
    "V_QIN_002",
    "V_QIN_003"
  ],

  "neighborRegionIds": [
    "QIN_R02",
    "JIN_R01",
    "ZHOU_R01"
  ]
}

    

    
    
    
    五、Settlement 数据模型

    Settlement 是玩家直接经营和战斗的单位。

    包括：

    城市；

    村庄。

    
    SettlementConfig 结构

    {
  "id":"CITY_QIN_001",

  "name":"秦西城",

  "type":"CITY",

  "regionId":"QIN_R01",

  "ownerId":"QIN",

  "households":300,

  "population":1500,

  "land":15000,

  "grain":300000,

  "money":2000,

  "garrison":50,

  "neighborSettlementIds":[
    "V_QIN_001",
    "V_QIN_002"
  ]
}

    

    
    
    
    六、国家静态数据

    NationConfig 保存国家基础信息。

    例如：

    {
  "id":"QIN",

  "name":"秦",

  "type":"KINGDOM",

  "color":"#AA0000",

  "capitalRegionId":"QIN_R01"
}

    包括：

    势力名称；

    势力颜色；

    初始首都区域；

    AI 类型。

    

    
    
    七、Region 与 Settlement 的关系

    一个 Region 必须包含：

    1 个核心城
+
多个附属村庄

    例如：

    秦东部 Region

          咸阳城
             |
    -----------------
    |       |       |
   村甲    村乙    村丙

    其中：

    城是行政、军事中心；

    村负责农业和人口生产；

    城决定区域最终归属。

    

    
    
    八、战争占领规则

    采用：

    
    村庄削弱，城市决定归属

    战争过程：

    进攻敌方 Region
        ↓
占领外围村庄
        ↓
降低敌方粮食收入
降低征兵能力
降低民心
        ↓
围攻核心城市
        ↓
攻破城市
        ↓
整个 Region 易主

    

    
    
    Region 易主逻辑

    当核心城市被攻占：

    Region.OwnerId 修改
        ↓
区域内所有 Settlement.OwnerId 同步修改
        ↓
地图颜色刷新
        ↓
国家势力范围变化

    

    
    
    
    九、地图邻接规则

    采用双层邻接结构。

    
    9.1 Region 邻接（战略层）

    用于：

    国家 AI；

    外交威胁；

    战争目标选择。

    例如：

    秦西部 Region

邻接：

晋北部 Region
周 Region
秦中部 Region

    

    
    
    9.2 Settlement 邻接（战术层）

    用于：

    军队移动；

    包围城市；

    攻击目标判断。

    例如：

    咸阳城
 |
 ├── 村1
 ├── 村2
 └── 村3

    

    
    
    
    十、WorldConfig 文件组织

    推荐目录：

    Assets/Configs/World/

├── NationConfig.json
├── RegionConfig.json
├── SettlementConfig.json
└── WorldMapConfig.json

    职责：

    NationConfig：

    势力信息。

    RegionConfig：

    战略地图。

    SettlementConfig：

    城村具体数据。

    WorldMapConfig：

    世界初始化参数。

    

    
    
    十一、与旧版本的主要变化

    旧 V1 设计
新 V2 设计
城和村完全独立
增加 Region 管理城村
89 个 Settlement 直接决定地图
24 个 Region 决定战略地图
AI 逐个判断城村
AI 按 Region 思考
占村导致地图碎片化
攻城决定区域归属
势力颜色来源 Settlement
势力颜色来源 Region

    

    
    
    十二、WorldConfig 最终模型

    WorldConfig
│
├── NationConfig
│
├── RegionConfig
│       │
│       ├── Core City
│       │
│       └── Villages
│
└── SettlementConfig
        │
        ├── City
        └── Village

    核心思想：

    Region 负责“天下棋盘”，Settlement 负责“经营与战争”。

    这样既保持了 V1 小规模、高反馈的游戏节奏，又为未来 V2、V3 的郡县、道路、地形、贸易和大型地图扩展提供了稳定的数据结构。