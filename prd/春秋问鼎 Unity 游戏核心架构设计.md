《春秋问鼎》Unity 游戏核心架构设计（C# GameEngine Architecture）

    

    
    
    一、设计目标

    《春秋问鼎》计划长期迭代：

    V1.0：21城68村的简化春秋大战略；

    V2.0：真实地图、地形、将领、多兵种、外交；

    V3.0：大型3D战场、实时战争、更加复杂的国家模拟。

    因此核心架构原则：

    游戏规则（Simulation）与 Unity 表现（Presentation）彻底分离。

    Unity 负责显示世界。

    GameEngine 负责推动世界运行。

    这样可以保证：

    游戏规则不会受到画面变化影响；

    后续增加3D表现时无需重写核心逻辑；

    便于扩展、测试和维护。

    

    
    
    二、总体架构设计

    整体采用五层架构：

    Unity Scene
     |
3D地图 / UI
     |
Game Controller
     |
GameEngine API
     |
Simulation Layer
     |
World State
     |
Config Data / Save Data

    

    
    
    三、Config Layer（静态配置层）

    
    3.1 作用

    用于定义游戏的初始规则和基础数据。

    例如：

    世界地图；

    城池信息；

    建筑数据；

    士兵属性；

    AI参数；

    事件配置。

    配置文件位置：

    Assets/Configs/

    包含：

    WorldConfig.json
BuildingConfig.json
UnitConfig.json
AIConfig.json
EventConfig.json

    

    
    
    3.2 特点

    Config 数据：

    创建新游戏时读取；

    游戏过程中不修改；

    作为世界生成模板。

    例如：

    洛邑：

    类型：国都；

    所属：周；

    初始人口：2500；

    初始守军：100；

    邻接城池：秦、晋、齐、楚边境区域。

    

    
    
    
    四、World State（世界状态层）

    
    4.1 作用

    World State 表示当前天下的真实状态。

    例如：

    某年某月：

    秦国：

    人口 10000；

    粮食 300万；

    士兵 800。

    秦都：

    人口 2600；

    守军 120。

    玩家：

    拥有 3 个村庄；

    300 名士兵。

    这些数据会随着游戏运行不断变化。

    

    
    
    4.2 核心对象

    World 包含：

    GameTime（时间）；

    Nation（国家）；

    Settlement（城池、村庄）；

    Army（军队）；

    Diplomacy（外交关系）；

    Event（事件）。

    结构：

    World
├── Time
├── Nations
├── Settlements
├── Armies
├── Diplomacy
└── Events

    

    
    
    
    五、Simulation Layer（世界模拟层）

    Simulation 是游戏的核心。

    负责按照月度 Tick 推动世界变化。

    包含以下系统。

    

    
    5.1 TickSystem（时间系统）

    负责推进游戏时间。

    每月执行：

    农业生产；

    税收；

    军队维护；

    人口变化；

    建筑进度；

    外交变化；

    AI决策；

    执行命令；

    军队行军；

    战斗结算；

    事件处理；

    存档。

    

    
    
    5.2 EconomySystem（经济系统）

    负责：

    粮食生产；

    粮税；

    铜钱税；

    粮食消耗；

    军饷支付。

    

    
    
    5.3 PopulationSystem（人口系统）

    负责：

    人口增长；

    流民迁入；

    饥荒死亡；

    人口上限。

    

    
    
    5.4 BuildingSystem（建筑系统）

    负责：

    建筑建造；

    建筑队列；

    建筑完成；

    建筑效果计算。

    

    
    
    5.5 AISystem（国家AI系统）

    负责国家行为：

    发展建设；

    征兵；

    调整驻军；

    外交判断；

    宣战；

    组织野战军。

    

    
    
    5.6 ArmySystem（军队系统）

    负责：

    征兵；

    训练；

    军队编组；

    行军；

    解散。

    

    
    
    5.7 BattleSystem（战斗系统）

    负责：

    输入：

    攻击军队；

    防守军队；

    城防加成。

    输出：

    胜负；

    伤亡；

    占领结果；

    领土变化。

    

    
    
    5.8 DiplomacySystem（外交系统）

    负责：

    国家之间的关系。

    关系范围：

    -100 到 100。

    例如：

    秦 ↔ 晋
秦 ↔ 齐
秦 ↔ 玩家

    影响：

    是否敌视；

    是否备战；

    是否发动战争。

    

    
    
    5.9 EventSystem（事件系统）

    负责：

    丰收；

    灾害；

    流民；

    叛乱；

    历史事件。

    V1.0 可保留接口。

    

    
    
    
    六、Command 命令系统

    
    6.1 设计原则

    玩家和 AI 使用完全相同的规则。

    禁止：

    点击征兵后直接增加士兵数量。

    正确流程：

    玩家点击征兵
        ↓
创建 RecruitCommand
        ↓
进入命令队列
        ↓
Tick执行
        ↓
开始训练
        ↓
训练完成
        ↓
成为正式士兵

    

    
    
    6.2 命令类型

    统一抽象：

    GameCommand。

    具体包括：

    BuildCommand（建设）；

    RecruitCommand（征兵）；

    MoveArmyCommand（移动军队）；

    AttackCommand（攻击）；

    DiplomacyCommand（外交）。

    

    
    
    6.3 优势

    Command 系统带来：

    玩家与 AI 规则统一；

    方便记录历史；

    方便回放；

    未来支持多人模式；

    降低系统耦合。

    

    
    
    
    七、GameEngine（游戏核心入口）

    GameEngine 是 Unity 与模拟世界之间的唯一入口。

    职责：

    管理 World；

    调用各个 System；

    接收玩家命令；

    推进游戏时间。

    数据流：

    Unity UI
   ↓
Controller
   ↓
GameEngine
   ↓
Command
   ↓
World State

    原则：

    Unity 不允许直接修改世界数据。

    例如：

    禁止：

    Settlement.population += 100

    必须通过：

    UI操作
 ↓
GameEngine
 ↓
Command
 ↓
System处理
 ↓
World变化

    

    
    
    八、Presentation Layer（Unity表现层）

    由 Unity 负责。

    包括：

    
    8.1 3D战略地图

    表现：

    地形；

    城池模型；

    道路；

    军队模型；

    行军动画；

    战争效果。

    

    
    
    8.2 UI系统

    包括：

    主界面；

    城池界面；

    国家界面；

    建筑界面；

    军队界面；

    外交界面；

    战报；

    事件窗口。

    

    
    
    
    九、存档架构

    存档只保存：

    World State。

    不保存：

    Unity 场景；

    模型；

    动画状态。

    流程：

    World State
     ↓
Serialize
     ↓
Save File

    读取：

    Save File
     ↓
恢复 World State
     ↓
刷新 Unity 场景

    

    
    
    十、Unity 项目建议目录

    Assets
│
├── Scripts
│
├── Core
│   ├── GameEngine
│   └── World
│
├── Config
│   ├── WorldConfig
│   ├── BuildingConfig
│   ├── UnitConfig
│   ├── AIConfig
│   └── EventConfig
│
├── Systems
│   ├── TickSystem
│   ├── EconomySystem
│   ├── PopulationSystem
│   ├── ArmySystem
│   ├── BattleSystem
│   ├── AISystem
│   └── EventSystem
│
├── Commands
│
├── UI
│
├── Map
│
└── Art

    

    
    
    十一、未来版本扩展方向

    该架构支持：

    
    V2.0

    增加：

    将领系统；

    多兵种；

    科技系统；

    贸易系统；

    真实春秋地图；

    更复杂外交。

    只需要：

    增加 Config；

    扩展 World State；

    新增 System。

    无需推翻已有架构。

    

    
    
    V3.0

    进一步支持：

    3D战场；

    实时战争；

    季节系统；

    天气系统；

    河流运输；

    更复杂AI。

    

    
    
    
    十二、架构核心思想总结

    整个《春秋问鼎》的运行逻辑：

    Config 创建世界
       ↓
World 保存状态
       ↓
System 推动世界变化
       ↓
Command 描述玩家与AI行为
       ↓
GameEngine 统一调度
       ↓
Unity负责3D表现

    最终目标：

    构建一个“数据驱动的春秋大战略模拟内核 + Unity 3D战略表现层”的长期可扩展架构。