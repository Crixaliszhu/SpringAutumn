一、设计目标
《春秋问鼎》的游戏世界是持续变化的。
例如：
第 1 年：
秦国
人口：10000
粮食：200万
拥有区域：5个
第 30 年：
秦国
人口：30000
粮食：800万
拥有区域：12个
与晋国战争中
这些变化都属于：
WorldRuntime
存档系统需要完成：
WorldRuntime
      |
      | Serialize
      ↓
 SaveData JSON
      |
      | Deserialize
      ↓
WorldRuntime

二、核心设计原则
2.1 Config 不存档
世界配置：
WorldConfig
例如：
地图结构； 
Region 邻接关系； 
建筑规则； 
税率； 
战斗公式； 
AI 参数。 
这些属于固定数据。
因此：
不进入存档

2.2 只保存 Runtime 状态
例如：
SettlementState

ID:
CITY_QIN_001

Owner:
PLAYER

Population:
5000

Grain:
200000
这些会进入：
SaveData

2.3 存档必须可恢复
读取存档后：
Load Save

      ↓

WorldRuntime

      ↓

GameEngine

      ↓

继续运行
不能依赖场景对象。

三、存档整体架构
                WorldConfig
                     |
              ConfigLoader
                     |
              ConfigDatabase
                     |
               WorldFactory
                     |
                     |
              WorldRuntime
                     |
          +----------+----------+
          |                     |
          V                     V
       Save                 Game Tick
          |
          V
      SaveData
          |
          V
     JSON File

四、SaveData 顶层设计
存档文件：
SaveData
包含：
SaveData
|
├── SaveInfo
|
├── GameTimeData
|
├── NationData[]
|
├── RegionData[]
|
├── SettlementData[]
|
├── ArmyData[]
|
├── DiplomacyData[]
|
├── CommandQueueData[]
|
└── EventData[]

五、SaveInfo 设计
用于显示存档列表。
public class SaveInfo
{
    public int Slot;

    public string SaveName;

    public string Version;

    public string CreateTime;

    public string ScreenshotPath;
}
例如：
存档 1

名称：
秦灭六国前夕

时间：
游戏第50年3月

版本：
V1.0.0

六、GameTimeData
保存游戏时间。
public class GameTimeData
{
    public int Year;

    public int Month;
}
例如：
公元前720年 5月

七、NationSaveData
保存国家状态。
public class NationSaveData
{
    public string Id;

    public int Money;

    public int Grain;

    public int Reputation;

    public List<string> RegionIds;
}
保存：
国家财政； 
国家粮仓； 
声望； 
当前控制区域。 

八、RegionSaveData
保存地区状态。
public class RegionSaveData
{
    public string Id;

    public string OwnerId;

    public bool IsCapital;
}
例如：
QIN_R01

归属：
PLAYER

九、SettlementSaveData
保存城池和村庄。
public class SettlementSaveData
{
    public string Id;

    public string OwnerId;

    public int Household;

    public int Population;

    public int Grain;

    public int Money;

    public int Garrison;

    public List<BuildingData> Buildings;
}

BuildingData
public class BuildingData
{
    public string BuildingId;

    public int Level;
}
例如：
城墙 Lv3

农田 Lv5

十、ArmySaveData
保存野战军。
public class ArmySaveData
{
    public string ArmyId;

    public string OwnerId;

    public int SoldierCount;

    public string CurrentRegion;

    public string TargetRegion;

    public int MoveProgress;
}

十一、DiplomacyData
保存国家关系。
public class DiplomacyData
{
    public string NationA;

    public string NationB;

    public RelationType Relation;

    public int RelationValue;
}
例如：
秦 vs 晋

状态：
战争

关系：
-100

十二、CommandQueueData
保存未执行命令。
例如：
玩家：
下月开始征兵
AI：
准备进攻齐国
结构：
public class CommandData
{
    public string CommandType;

    public string Data;
}

十三、EventData
保存持续事件。
例如：
饥荒； 
丰收； 
叛乱； 
疫病。 
结构：
public class EventData
{
    public string EventId;

    public int RemainingMonth;
}

十四、SaveManager 设计
SaveManager 是唯一存档入口。

接口设计
public interface ISaveManager
{
    void Save(int slot);

    WorldRuntime Load(int slot);

    void Delete(int slot);

    List<SaveInfo> GetSaveList();
}

数据流程
保存：
WorldRuntime
      |
      ↓
SaveConverter
      |
      ↓
SaveData
      |
      ↓
JSON
      |
      ↓
File
读取：
File
 |
JSON
 |
SaveData
 |
SaveConverter
 |
WorldRuntime

十五、SaveConverter 设计
负责：
Runtime 与 SaveData 转换。

保存
SaveData Convert(WorldRuntime world);

读取
WorldRuntime Restore(SaveData data);
这样：
SaveManager 只处理文件； 
SaveConverter 处理数据映射； 
WorldRuntime 不依赖 JSON。 

十六、Unity 文件路径设计
使用：
Application.persistentDataPath
例如：
Save/
|
├── Save001.json
├── Save002.json
└── Save003.json
适用于：
微信小游戏； 
Android； 
iOS； 
PC。 

十七、自动存档策略
V1 推荐：
自动存档
触发：
每 3 游戏月； 
重大战争结束； 
玩家占领 Region； 
游戏退出。 

手动存档
玩家：
菜单
 |
保存游戏

十八、存档版本管理
SaveData 顶部：
{
    "version": "1.0.0"
}
作用：
V2 数据兼容； 
旧版本升级； 
防止读取错误。 

十九、异常恢复设计
文件损坏
处理：
读取失败
↓
提示存档损坏

版本不一致
处理：
Version Check
        |
        +-- Compatible
        |
        +-- Migration
        |
        +-- Reject

二十、Unity 工程目录建议
Scripts
|
├── Save
│   |
│   ├── SaveManager.cs
│   ├── SaveConverter.cs
│   ├── SaveData.cs
│   └── SaveInfo.cs
|
├── Runtime
|
└── Config

二十一、与整体架构关系
        Config JSON
             |
       ConfigLoader
             |
       ConfigDatabase
             |
        WorldFactory
             |
        WorldRuntime
             |
      +------+------+
      |             |
  Game Tick       Save
      |             |
      |        SaveConverter
      |             |
      +--------> SaveData
                     |
                     V
                 JSON File

二十二、设计总结
存档系统核心原则：
Config 定义世界，Runtime 运行世界，SaveData 保存世界。
完整生命周期：
New Game

WorldConfig
      |
      V
WorldRuntime
      |
      V
Game Tick
      |
      V
SaveData
      |
      V
JSON

Load Game

JSON
  |
  V
SaveData
  |
  V
WorldRuntime
  |
  V
继续运行