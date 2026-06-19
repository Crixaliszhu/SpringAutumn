一、核心设计思想
整个项目遵循一个原则：
数据驱动 + 领域分层 + 表现与逻辑分离
架构：
             Unity View
          (Map/UI/Animation)
                   |
                   ↓
            Application Layer
          (Command/Event)
                   |
                   ↓
             GameEngine
          (System/Tick)
                   |
                   ↓
              Domain Model
      (WorldRuntime/State)
                   |
                   ↓
              Config Data

二、Namespace 规范
2.1 顶级 Namespace
统一使用：
SpringAutumn
例如：
SpringAutumn
|
├── Core
├── Config
├── Runtime
├── Systems
├── Commands
├── AI
├── Battle
├── Save
├── Map
├── UI
├── Utils
└── Tests

示例：
GameEngine
namespace SpringAutumn.Core
{
    public class GameEngine
    {
    }
}

Runtime
namespace SpringAutumn.Runtime
{
    public class WorldRuntime
    {
    }
}

三、目录与 Namespace 一致
必须保持：
Assets/_Game/Scripts
|
└── Runtime
    |
    └── WorldRuntime.cs
对应：
namespace SpringAutumn.Runtime
不要出现：
Scripts/Runtime
namespace Game
这种混乱情况。

四、类命名规范
4.1 PascalCase
所有类：
GameEngine
WorldRuntime
NationState
RegionState
ArmyState
禁止：
gameEngine
world_runtime

五、接口命名
必须：
I + 名称
例如：
IGameSystem

ICommand

ISaveManager

IAIController

六、抽象类命名
推荐：
Base + 名称
例如：
BaseCommand

BaseSystem

BaseAI

七、枚举命名
Enum 使用单数
正确：
public enum NationType
{
    Qin,
    Chu,
    Jin,
    Qi,
    Zhou
}
不要：
Nations

枚举值：
PascalCase：
Peace
War
Alliance
不要：
PEACE
WAR

八、字段命名规范
private 字段
使用：
private int _population;
private int _grain;

public Property
使用：
public int Population { get; private set; }

public int Grain { get; private set; }

不要：
public int population;

九、常量命名
const
使用：
public const int MaxArmySize = 10000;

不要：
MAX_ARMY_SIZE

十、ID 命名规范（非常重要）
因为《春秋问鼎》大量使用数据驱动。
例如：
Nation ID
QIN
JIN
QI
CHU
ZHOU
PLAYER

Region ID
QIN_R01
QIN_R02

JIN_R01

Settlement ID
城市：
CITY_QIN_01
CITY_JIN_03
村庄：
VILLAGE_QIN_001

Building ID
FARM
MARKET
BARRACK
WALL

Command ID
RECRUIT
ATTACK
BUILD

十一、Config 类命名
所有配置：
后缀：
Config
例如：
WorldConfig

NationConfig

RegionConfig

BattleConfig

十二、Runtime 类命名
运行状态：
后缀：
State
例如：
NationState

RegionState

SettlementState

不要：
NationData
原因：
Data 容易与 SaveData 混淆。

十三、存档类命名
统一：
SaveData
例如：
WorldSaveData

NationSaveData

ArmySaveData

十四、View 类命名
Unity 表现对象：
后缀：
View
例如：
RegionView

CityView

ArmyView

不要：
RegionManager
除非它真的负责管理。

十五、Manager 使用规则（重点）
不要滥用：
错误：
BattleManager
PlayerManager
NationManager
为什么？
Manager 不表达职责。

只有全局协调者允许：
GameManager
SystemManager
SaveManager

业务逻辑使用：
BattleSystem
PopulationSystem
EconomySystem

十六、MonoBehaviour 使用规范（非常重要）
原则：
GameEngine 不依赖 Unity。

正确：
GameEngine
    |
    |
纯 C# 类

Unity:
ArmyView : MonoBehaviour
负责：
显示模型 
播放动画 
响应点击 

不要：
public class Army : MonoBehaviour
{
    public int Soldier;
}
这是典型错误。

正确：
ArmyState
       |
       |
ArmyView

十七、事件命名
使用过去式：
例如：
OnArmyCreated

OnBattleFinished

OnRegionCaptured

OnMonthChanged
不要：
OnCreateArmy
因为事件表示：
已经发生的事情。

十八、方法命名
使用：
动词 + 名词
例如：
CalculateBattle()

CreateArmy()

CollectTax()

ExecuteTick()

SaveGame()

不要：
Battle()

Tax()

十九、文件命名
一个文件一个类：
例如：
GameEngine.cs

WorldRuntime.cs

NationState.cs
不要：
World.cs
里面包含：
Nation
Region
Army

二十、日志规范
统一：
GameLogger.Log(
    "[Economy] Qin collect grain 50000"
);
模块前缀：
[Config]
[Runtime]
[AI]
[Battle]
[Save]

二十一、测试目录
独立：
Tests
|
├── RuntimeTests
├── EconomyTests
├── BattleTests
└── AITests

二十二、Git 分支建议
长期项目建议：
main
|
develop
|
feature/*
例如：
feature/economy-system

feature/battle-system

feature/save-system

二十三、最重要的五条规则
我认为《春秋问鼎》最关键的是：
规则
原因
Config 后缀
区分静态配置
State 后缀
区分运行数据
SaveData 后缀
区分存档
View 后缀
区分 Unity 表现
GameEngine 不继承 MonoBehaviour
保持核心逻辑可测试

最终规范一句话总结
Config 定义世界，State 运行世界，System 改变世界，Command 请求改变世界，View 展示世界，Save 保存世界。