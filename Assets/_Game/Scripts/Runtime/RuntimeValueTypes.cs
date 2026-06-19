namespace SpringAutumn.Runtime
{
    /// <summary>据点已建成的建筑实例。</summary>
    public class BuildingInstance
    {
        public string BuildingId;
        public int Level;

        public BuildingInstance() { }

        public BuildingInstance(string buildingId, int level = 1)
        {
            BuildingId = buildingId;
            Level = level;
        }
    }

    /// <summary>进行中的建造任务（命令延迟生效，按月递减）。</summary>
    public class ConstructionTask
    {
        public string BuildingId;
        public int RemainingMonths;

        public ConstructionTask() { }

        public ConstructionTask(string buildingId, int remainingMonths)
        {
            BuildingId = buildingId;
            RemainingMonths = remainingMonths;
        }
    }

    /// <summary>进行中的征兵任务（命令延迟生效，按月递减）。</summary>
    public class RecruitTask
    {
        public int Count;
        public int RemainingMonths;

        public RecruitTask() { }

        public RecruitTask(int count, int remainingMonths)
        {
            Count = count;
            RemainingMonths = remainingMonths;
        }
    }
}
