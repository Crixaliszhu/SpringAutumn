using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SpringAutumn.Save
{
    [DataContract]
    public class SaveData
    {
        [DataMember] public int version = 1;
        [DataMember] public SaveInfo info = new SaveInfo();
        [DataMember] public GameTimeData time = new GameTimeData();
        [DataMember] public List<NationSaveData> nations = new List<NationSaveData>();
        [DataMember] public List<RegionSaveData> regions = new List<RegionSaveData>();
        [DataMember] public List<SettlementSaveData> settlements = new List<SettlementSaveData>();
        [DataMember] public List<ArmySaveData> armies = new List<ArmySaveData>();
        [DataMember] public List<DiplomacyData> diplomacy = new List<DiplomacyData>();
        [DataMember] public CommandQueueData commandQueue = new CommandQueueData();
        [DataMember] public List<EventData> events = new List<EventData>();
        [DataMember] public RandomData random = new RandomData();
    }

    [DataContract]
    public class SaveInfo
    {
        [DataMember] public int slot;
        [DataMember] public string displayName;
        [DataMember] public int year;
        [DataMember] public int month;
    }

    [DataContract]
    public class GameTimeData
    {
        [DataMember] public int year;
        [DataMember] public int month;
    }

    [DataContract]
    public class NationSaveData
    {
        [DataMember] public string id;
        [DataMember] public int treasuryGrain;
        [DataMember] public int treasuryMoney;
        [DataMember] public string aiState;
        [DataMember] public string warStatus;
    }

    [DataContract]
    public class RegionSaveData
    {
        [DataMember] public string id;
        [DataMember] public string ownerId;
        [DataMember] public bool isFrontier;
        [DataMember] public string cityId;
        [DataMember] public List<string> villageIds = new List<string>();
        [DataMember] public List<string> neighborRegionIds = new List<string>();
    }

    [DataContract]
    public class SettlementSaveData
    {
        [DataMember] public string id;
        [DataMember] public string type;
        [DataMember] public string regionId;
        [DataMember] public string ownerId;
        [DataMember] public int households;
        [DataMember] public int population;
        [DataMember] public int populationCap;
        [DataMember] public int land;
        [DataMember] public int grain;
        [DataMember] public int money;
        [DataMember] public int loyalty;
        [DataMember] public int garrison;
        [DataMember] public List<BuildingData> buildings = new List<BuildingData>();
        [DataMember] public List<ConstructionTaskData> constructionQueue = new List<ConstructionTaskData>();
        [DataMember] public List<RecruitTaskData> recruitQueue = new List<RecruitTaskData>();
        [DataMember] public List<string> neighborSettlementIds = new List<string>();
    }

    [DataContract]
    public class BuildingData
    {
        [DataMember] public string buildingId;
        [DataMember] public int level;
    }

    [DataContract]
    public class ConstructionTaskData
    {
        [DataMember] public string buildingId;
        [DataMember] public int remainingMonths;
    }

    [DataContract]
    public class RecruitTaskData
    {
        [DataMember] public int count;
        [DataMember] public int remainingMonths;
    }

    [DataContract]
    public class ArmySaveData
    {
        [DataMember] public string id;
        [DataMember] public string nationId;
        [DataMember] public string sourceSettlementId;
        [DataMember] public string currentRegionId;
        [DataMember] public string targetRegionId;
        [DataMember] public string targetSettlementId;
        [DataMember] public int soldiers;
        [DataMember] public int morale;
        [DataMember] public string status;
        [DataMember] public string mission;
        [DataMember] public int moveProgress;
    }

    [DataContract]
    public class DiplomacyData
    {
        [DataMember] public string key;
        [DataMember] public int relationValue;
    }

    [DataContract]
    public class CommandQueueData
    {
        [DataMember] public List<CommandData> commands = new List<CommandData>();
    }

    [DataContract]
    public class CommandData
    {
        [DataMember] public string type;
        [DataMember] public string nationId;
        [DataMember] public string settlementId;
        [DataMember] public string buildingId;
        [DataMember] public int count;
        [DataMember] public string sourceSettlementId;
        [DataMember] public string targetRegionId;
        [DataMember] public int soldiers;
        [DataMember] public string armyId;
        [DataMember] public string targetSettlementId;
        [DataMember] public string targetNationId;
        [DataMember] public int relationDelta;
        [DataMember] public bool declareWar;
    }

    [DataContract]
    public class EventData
    {
        [DataMember] public string eventId;
        [DataMember] public string targetId;
        [DataMember] public int remainingMonths;
    }

    [DataContract]
    public class RandomData
    {
        [DataMember] public uint x;
        [DataMember] public uint y;
        [DataMember] public uint z;
        [DataMember] public uint w;
    }
}
