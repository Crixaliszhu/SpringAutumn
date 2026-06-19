一、设计目标
《春秋问鼎》的世界由 WorldRuntime 保存。
所有游戏行为：
人口增长； 
农业生产； 
税收； 
建筑建造； 
征兵训练； 
军队移动； 
战争； 
AI 决策； 
都不能直接由 UI 或对象之间相互调用完成。
采用统一的：
Game Tick → Systems → 修改 WorldRuntime
架构。

二、系统设计原则

2.1 System 只处理一种业务
错误：
BattleSystem
    |
    + 计算战斗
    + 修改税收
    + 人口增长
正确：
BattleSystem
    |
    + 战斗计算

EconomySystem
    |
    + 税收
    + 粮食消耗
每个系统拥有明确职责。

2.2 System 之间不直接调用
错误：
AISystem
    |
    调用 BattleSystem.Attack()
正确：
AISystem
    |
  创建 AttackCommand
    |
CommandQueue
    |
BattleSystem 执行
所有系统通过：
WorldRuntime 
CommandQueue 
进行数据交换。

2.3 所有修改必须进入 Game Tick
例如：
玩家点击：
征兵
不会立即增加士兵。
流程：
UI
 |
创建 RecruitCommand
 |
CommandQueue
 |
下一个 Game Tick
 |
RecruitSystem 执行
 |
修改 SettlementState
这样可以保证：
逻辑一致； 
方便存档； 
支持 AI 与玩家统一规则。 

三、GameEngine 与 System 关系
整体结构：
GameEngine
 |
 |--- GameTickEngine
 |
 |--- SystemManager
          |
          |
          + PopulationSystem
          |
          + EconomySystem
          |
          + ConstructionSystem
          |
          + RecruitSystem
          |
          + ArmySystem
          |
          + BattleSystem
          |
          + DiplomacySystem
          |
          + AISystem
          |
          + EventSystem

四、IGameSystem 统一接口设计
所有系统实现统一接口。
public interface IGameSystem
{
    void Execute(WorldRuntime world);
}

例如：
public class EconomySystem : IGameSystem
{
    public void Execute(WorldRuntime world)
    {
        CollectTax(world);

        ConsumeArmyFood(world);
    }
}

优势：
解耦； 
容易增加新系统； 
方便测试。 

五、SystemManager 设计
负责维护所有 System。

结构：
public class SystemManager
{
    private List<IGameSystem> systems;

    public void ExecuteTick(WorldRuntime world)
    {
        foreach(var system in systems)
        {
            system.Execute(world);
        }
    }
}

六、Game Tick 执行顺序
由于系统之间存在依赖，执行顺序固定。

1. EventSystem
处理：
饥荒； 
丰收； 
特殊事件。 
修改世界环境。

2. PopulationSystem
处理：
人口增长； 
流民产生； 
死亡。 
输入：
SettlementState
输出：
Population变化

3. EconomySystem
处理：
粮食生产； 
粮食税； 
铜钱税； 
粮食消耗； 
军饷支付。 

4. ConstructionSystem
处理：
建筑进度； 
建筑完成； 
建筑效果生效。 

5. RecruitSystem
处理：
征兵进度； 
新士兵加入守军。 

6. ArmySystem
处理：
野战军创建； 
行军； 
到达目标。 

7. BattleSystem
处理：
野战； 
攻城； 
占领 Region。 
修改：
RegionState.Owner
SettlementState.Owner

8. DiplomacySystem
处理：
国家关系变化； 
停战； 
联盟。 

9. AISystem
最后执行。
原因：
AI 应该看到本月所有最新状态。
例如：
粮食不足
↓
AI减少征兵

敌国兵力下降
↓
AI决定进攻

七、完整月度 Tick 流程
NextMonth()

       |
       V

EventSystem

       |
       V

PopulationSystem

       |
       V

EconomySystem

       |
       V

ConstructionSystem

       |
       V

RecruitSystem

       |
       V

ArmySystem

       |
       V

BattleSystem

       |
       V

DiplomacySystem

       |
       V

AISystem

       |
       V

Month + 1

八、Command 与 System 关系
玩家和 AI 都不会直接修改世界。
统一流程：
Player
  |
Command
  |
CommandQueue
  |
System
  |
WorldRuntime

AI
 |
Command
 |
CommandQueue
 |
System
 |
WorldRuntime

九、System 输入输出规范
System
输入
输出
Event
EventState
世界环境变化
Population
Settlement
人口变化
Economy
Settlement/Nation
粮钱变化
Construction
Command
建筑完成
Recruit
Command
士兵增加
Army
ArmyState
行军结果
Battle
Army/Settlement
战争结果
Diplomacy
Nation
外交状态
AI
WorldRuntime
AI Command

十、未来系统扩展能力
V2 可以增加：
CharacterSystem
TechnologySystem
TradeSystem
ReligionSystem
PoliticsSystem
只需要：
实现 IGameSystem
        |
加入 SystemManager
无需修改已有代码。

十一、Unity 工程目录建议
Scripts
 |
 ├── GameEngine
 |
 ├── Systems
 |     |
 |     ├ PopulationSystem.cs
 |     ├ EconomySystem.cs
 |     ├ ConstructionSystem.cs
 |     ├ RecruitSystem.cs
 |     ├ ArmySystem.cs
 |     ├ BattleSystem.cs
 |     ├ DiplomacySystem.cs
 |     └ AISystem.cs
 |
 └── Commands

十二、与整体架构关系
             Config
               |
               V
        WorldFactory
               |
               V
         WorldRuntime
               |
               V
          GameEngine
               |
               V
          SystemManager
               |
    +----------+----------+
    |          |          |
 Population Economy   Battle...
    |
    V
       修改 WorldRuntime
               |
               V
            SaveData

十三、设计总结
《春秋问鼎》的 System Architecture 核心原则：
所有游戏规则都由独立 System 在 Game Tick 中执行，并且所有变化最终写入 WorldRuntime。
形成统一数据流：
Command
    |
    V
Game Tick
    |
    V
Systems
    |
    V
WorldRuntime
    |
    V
SaveData
其优势：
玩家和 AI 使用同一套规则； 
所有行为可追踪； 
便于测试； 
便于扩展； 
适合大型策略游戏架构。