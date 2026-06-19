《春秋问鼎》World Runtime State Design（运行时世界状态设计）

    

    
    
    一、设计目标

    World Runtime State（世界运行时状态）用于表示当前这一局游戏真实存在的春秋天下。

    它不是世界初始配置，而是游戏运行过程中不断变化的动态数据。

    例如：

    游戏开始：

    秦国拥有咸阳地区；

    晋国拥有绛都地区；

    玩家只有一个流民村。

    十年之后：

    秦国可能已经灭亡；

    玩家可能占领多个 Region；

    城市人口、粮食、守军、建筑全部发生变化。

    这些变化不会修改 WorldConfig，而是记录在 World Runtime State 中。

    

    
    
    二、设计目的

    World Runtime State 负责：

    保存当前天下格局；

    作为所有 Game System 的数据中心；

    作为 Game Tick Pipeline 的输入和输出；

    作为 Save/Load System 的核心数据；

    支撑玩家和 AI 的所有行为计算。

    它是整个 GameEngine 的“单一真实来源（Single Source of Truth）”。

    

    
    
    三、整体架构位置

    《春秋问鼎》采用三层数据架构：

                   Config Layer
                    |
              WorldConfig
                （只读）
                    |
             New Game 创建
                    |
                    V
              Runtime Layer
                    |
        World Runtime State
              （动态变化）
                    |
              Save / Load
                    |
                    V
                SaveData
             （序列化文件）

    

    
    
    四、WorldConfig 与 Runtime State 的区别

    项目
WorldConfig
World Runtime State
作用
世界初始化模板
当前游戏世界状态
生命周期
游戏安装后固定存在
每局游戏独立存在
是否变化
否
是
是否存档
否
是
数据来源
设计者配置
Game Tick 持续计算
修改者
策划
GameEngine、玩家、AI

    

    
    
    五、World Runtime 顶层结构

    运行时世界由多个 State 组成：

    WorldRuntime
│
├── GameTimeState
│
├── NationState
│
├── RegionState
│
├── SettlementState
│
├── ArmyState
│
├── DiplomacyState
│
├── CommandQueue
│
└── EventState

    所有系统都围绕 WorldRuntime 运作。

    

    
    
    六、GameTimeState（时间状态）

    保存当前游戏时间。

    例如：

    {
    "year": 1,
    "month": 1
}

    作用：

    控制 Game Tick 推进；

    触发年度事件；

    记录战争持续时间；

    计算建筑、征兵剩余时间。

    

    
    
    七、NationState（国家状态）

    保存一个势力当前状态。

    包括：

    国库粮食；

    国库铜钱；

    当前 AI 状态；

    国家战略状态。

    示例：

    {
    "id": "QIN",
    "grain": 500000,
    "money": 30000,
    "aiState": "Developing",
    "warStatus": "Peace"
}

    

    
    
    八、RegionState（区域状态）

    Region 是战略层单位。

    保存：

    当前控制者；

    边境状态；

    战争状态；

    发展状态。

    例如：

    {
    "id": "QIN_R01",
    "ownerId": "QIN",
    "isFrontier": true
}

    动态变化：

    初始：

    QIN_R01.owner = QIN

    战争后：

    QIN_R01.owner = PLAYER

    

    
    
    九、SettlementState（城村状态）

    Settlement 是最重要的经济与军事单位。

    保存：

    
    人口

    户数；

    人口数量；

    人口上限。

    

    
    
    土地与生产

    土地面积；

    粮食储备；

    铜钱储备。

    

    
    
    社会状态

    民心；

    治安（未来版本）。

    

    
    
    军事状态

    守军数量；

    防御等级。

    

    
    
    建筑状态

    例如：

    {
    "buildings": [
        {
            "type": "Wall",
            "level": 3
        },
        {
            "type": "Barracks",
            "level": 2
        }
    ]
}

    

    
    
    
    十、ArmyState（军队状态）

    表示地图上的野战军。

    V1.0：

    守军属于 Settlement；

    野战军属于 Army。

    记录：

    所属国家；

    当前位置；

    目标位置；

    士兵数量；

    士气；

    行军状态。

    例如：

    {
    "id": "ARMY_001",
    "nationId": "QIN",
    "currentRegion": "QIN_R02",
    "targetRegion": "JIN_R01",
    "soldiers": 3000,
    "morale": 80
}

    

    
    
    十一、DiplomacyState（外交状态）

    保存国家之间关系。

    例如：

    {
    "nationA": "QIN",
    "nationB": "JIN",
    "relation": -80,
    "status": "WAR"
}

    关系影响：

    是否宣战；

    是否停战；

    AI 威胁判断。

    

    
    
    十二、CommandQueue（命令队列）

    用于保存等待执行的行为。

    来源：

    玩家操作；

    AI 决策。

    例如：

    BuildCommand
RecruitCommand
MoveArmyCommand
AttackCommand
DiplomacyCommand

    设计原则：

    玩家和 AI 必须使用同一套 Command 系统。

    

    
    
    十三、EventState（世界事件）

    保存当前持续生效的事件。

    例如：

    {
    "eventId": "DROUGHT_001",
    "region": "CHU_R03",
    "duration": 6,
    "effect": "FoodProduction -50%"
}

    未来可扩展：

    丰收；

    饥荒；

    流民；

    瘟疫；

    叛乱。

    

    
    
    十四、World Runtime C# 结构示例

    public class WorldRuntime
{
    public GameTimeState Time;

    public NationStateRepository Nations;

    public RegionStateRepository Regions;

    public SettlementStateRepository Settlements;

    public ArmyStateRepository Armies;

    public DiplomacyStateRepository Diplomacy;

    public CommandQueue Commands;

    public EventRepository Events;
}

    它是整个游戏运行期间最核心的对象。

    

    
    
    十五、Game Tick 与 Runtime 的关系

    每个月：

    Game Tick 开始
        |
        V
读取 WorldRuntime
        |
        V
人口增长
经济计算
军事维护
建筑完成
军队移动
战斗结算
AI 决策
        |
        V
修改 WorldRuntime
        |
        V
进入下个月

    所有变化都作用于 Runtime。

    

    
    
    十六、存档结构设计

    SaveData 本质上是：

    SaveData
    |
    V
WorldRuntime

    保存：

    当前年月；

    国家状态；

    Region 控制权；

    城村数据；

    军队状态；

    外交关系；

    建筑与征兵队列；

    世界事件。

    读取存档：

    SaveData
      |
      V
恢复 WorldRuntime
      |
      V
继续 Game Tick

    

    
    
    十七、Unity 项目中的位置

    推荐结构：

    Assets
│
├── Configs
│   └── WorldConfig.json
│
├── Scripts
│   ├── GameEngine
│   ├── Systems
│   └── Domain
│
└── Saves
    ├── Save001.json
    └── Save002.json

    流程：

    
    新游戏

    读取 WorldConfig
        |
        V
创建 WorldRuntime
        |
        V
开始游戏

    
    
    读取存档

    读取 SaveData
        |
        V
恢复 WorldRuntime
        |
        V
继续游戏

    

    
    
    
    十八、与其他核心文档的关系

    World Runtime State 位于整个架构中心：

    WorldConfig
（天下初始模板）
          |
          V
World Runtime State
（当前天下）
          |
          ├── Game Tick Pipeline
          ├── Economy System
          ├── Population System
          ├── Battle System
          ├── AI System
          ├── Command System
          └── Save System

    

    
    
    十九、最终设计理念

    如果说：

    WorldConfig 定义天下从哪里开始；

    Game Tick 定义时间如何流动；

    AI Decision Rules 定义诸侯如何行动；

    那么：

    World Runtime State 定义这一刻真实存在的春秋天下。

    它记录：

    谁拥有土地；

    哪座城池繁荣；

    哪个国家衰弱；

    哪支军队正在出征；

    哪场战争正在发生。

    

    
    
    二十、V1.0 游戏核心数据闭环

    至此，《春秋问鼎》V1.0 的底层数据架构形成完整闭环：

    WorldConfig
     |
     | 创建新游戏
     V
World Runtime State
     |
     | Game Tick 每月推进
     V
Game Systems
     |
     | 修改世界状态
     V
SaveData
     |
     | Load
     └──────────返回 World Runtime State

    这一架构完全符合 Unity 大型策略游戏的开发模式，可以支撑 V1.0 微信小游戏版本，同时为未来 V2/V3 的人物、将领、科技、地形、贸易、朝廷政治等系统预留扩展空间。