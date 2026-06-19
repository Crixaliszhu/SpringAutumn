《春秋问鼎》Unity C# Class Diagram V2（区域化详细类图设计）

    

    
    
    一、设计目标

    本版本基于 Region 战略区域模型，对 V1 C# 类结构进行升级。

    核心变化：

    增加 Region 作为战略层对象；

    Nation 通过 Region 判断势力范围；

    AI 以 Region 作为战略目标；

    Settlement 专注经营、生产和战斗；

    World 采用 Repository 统一管理所有实体。

    设计目标：

    支撑 24 Region 战略地图；

    支撑 21 城 + 68 村 Settlement；

    支撑国家扩张、区域战争和势力变化；

    支撑未来大地图扩展。

    

    
    
    二、核心架构关系

                             GameEngine
                              |
                            World
                              |
 ----------------------------------------------------------------
 |                |                 |               |             |
NationRepo   RegionRepo     SettlementRepo      ArmyRepo   EventRepo
 |                |                 |
Nation         Region          Settlement
                  |
                  |
            +-------------+
            |             |
          City         Village

    说明：

    Nation：国家战略层；

    Region：地图战略层；

    Settlement：经营和战斗层；

    Army：军事行动层。

    

    
    
    三、Entity 基类

    所有世界实体统一继承 Entity。

    public abstract class Entity
{
    public string Id;
}

    ID 示例：

    Nation:
QIN

Region:
QIN_R01

Settlement:
CITY_QIN_01
VILLAGE_QIN_01

Army:
ARMY_0001

    

    
    
    四、Repository 泛型设计

    使用 Dictionary 实现 O(1) 查询。

    public class Repository<T> where T : Entity
{
    private Dictionary<string, T> Data;

    public T Get(string id);

    public void Add(T entity);

    public void Remove(string id);

    public IEnumerable<T> GetAll();
}

    具体 Repository：

    NationRepository
RegionRepository
SettlementRepository
ArmyRepository
EventRepository

    

    
    
    五、World 根对象

    World 保存全部动态世界状态。

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

    

    
    
    六、Nation 国家类

    设计原则：

    Nation 不保存：

    List<Region>
List<Settlement>

    避免区域变更产生数据同步问题。

    通过 Repository 查询：

    当前控制 Region；

    总人口；

    军事实力；

    经济能力。

    

    public class Nation : Entity
{
    public string Name;

    public NationType Type;

    public int TreasuryGrain;

    public int TreasuryMoney;

    public AIState AIState;

    public int GetRegionCount(World world);

    public int GetPopulation(World world);

    public int GetMilitaryPower(World world);

    public bool IsPlayer();
}

    

    
    
    七、Region 战略区域类

    Region 是 V2 最关键新增对象。

    职责：

    势力边界；

    战略价值；

    邻接关系；

    战争目标。

    

    public class Region : Entity
{
    public string Name;

    // 当前控制国家
    public string OwnerId;

    // 核心城市
    public string CityId;

    // 附属村庄
    public List<string> VillageIds;

    // 战略邻接
    public List<string> NeighborRegionIds;

    public bool IsBorder(World world);

    public int CalculateValue(World world);
}

    

    
    
    八、Settlement 经营单位

    包括：

    City；

    Village。

    

    public class Settlement : Entity
{
    public string Name;

    public SettlementType Type;

    // 所属 Region
    public string RegionId;

    // 当前势力
    public string OwnerId;

    // 人口
    public int Households;

    public int Population;

    // 土地
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

    public List<BuildingInstance> Buildings;

    public int CalculateFood();

    public int CalculateTax();

    public int CalculateDefense();
}

    

    
    
    九、Army 军队类

    V1.0 只有单一兵种。

    public class Army : Entity
{
    public string NationId;

    public string CurrentSettlementId;

    public string TargetSettlementId;

    public int Soldiers;

    public int Morale;

    public ArmyStatus Status;

    public int CalculatePower();

    public void Move();

    public bool IsDestroyed();
}

    

    
    
    十、Region 占领规则实现

    核心原则：

    城决定 Region 归属。

    因此需要 Region 的占领方法：

    public class Region : Entity
{
    public void ChangeOwner(
        string nationId,
        World world)
    {
        OwnerId = nationId;

        foreach(var villageId in VillageIds)
        {
            world.Settlements
                 .Get(villageId)
                 .OwnerId = nationId;
        }

        world.Settlements
            .Get(CityId)
            .OwnerId = nationId;
    }
}

    

    
    
    十一、Command 系统

    玩家和 AI 使用统一行为接口。

    结构：

    GameCommand
 |
 ├── BuildCommand
 ├── RecruitCommand
 ├── MoveArmyCommand
 ├── AttackCommand
 └── DiplomacyCommand

    

    public abstract class GameCommand
{
    public string NationId;

    public abstract void Execute(World world);
}

    

    
    
    十二、Simulation System

    System 不保存状态。

    所有计算：

    读取 World
    ↓
规则计算
    ↓
修改 World

    主要系统：

    TickSystem

EconomySystem

PopulationSystem

ArmySystem

BattleSystem

AISystem

DiplomacySystem

SaveSystem

    

    
    
    十三、Unity 与 GameEngine 关系

    严格限制：

    Unity 不能直接修改 World。

    正确流程：

    UI / 3D地图
      ↓
Controller
      ↓
GameEngine
      ↓
Command
      ↓
System
      ↓
World 更新
      ↓
Unity 刷新显示

    

    
    
    十四、最终 V2 类结构

    Nation
  ↑
Region（战略层）
  ↑
Settlement（经营层）
  ↑
Army（军事层）

World 管理所有数据
System 推动世界变化
Command 统一所有行为
Unity 负责表现

    

    
    
    十五、V2 架构总结

    新的 C# Class Diagram 最大升级：

    增加 RegionRepository；

    Nation → Region → Settlement 三层世界结构；

    AI 从 Settlement 级提升到 Region 级；

    城市成为区域控制核心；

    村庄成为经济削弱目标。

    该架构已经可以支撑《春秋问鼎》长期发展：

    V1.0：

    24 Region；

    89 Settlement；

    五国争霸。

    V2：

    更大地图；

    将领；

    多兵种；

    道路、地形和贸易系统。

    V3：

    大规模 3D 春秋世界模拟。