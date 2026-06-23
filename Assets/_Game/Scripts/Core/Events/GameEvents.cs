namespace SpringAutumn.Core.Events
{
    /// <summary>月份推进。</summary>
    public struct MonthChanged : IGameEvent
    {
        public int Year;
        public int Month;
    }

    /// <summary>区域被占领。</summary>
    public struct RegionCaptured : IGameEvent
    {
        public string RegionId;
        public string OldOwnerId;
        public string NewOwnerId;
    }

    /// <summary>战斗结束。</summary>
    public struct BattleFinished : IGameEvent
    {
        public string AttackerNationId;
        public string DefenderNationId;
        public string SettlementId;
        public bool AttackerWon;
    }

    /// <summary>建筑完成。</summary>
    public struct BuildingFinished : IGameEvent
    {
        public string SettlementId;
        public string BuildingId;
    }

    /// <summary>野战军创建。</summary>
    public struct ArmyCreated : IGameEvent
    {
        public string ArmyId;
        public string NationId;
    }

    /// <summary>征兵完成。</summary>
    public struct RecruitFinished : IGameEvent
    {
        public string SettlementId;
        public int Count;
    }

    /// <summary>宣战。</summary>
    public struct WarDeclared : IGameEvent
    {
        public string AttackerNationId;
        public string DefenderNationId;
    }

    /// <summary>国家灭亡。</summary>
    public struct NationDestroyed : IGameEvent
    {
        public string NationId;
    }

    /// <summary>游戏结束。PlayerWon 为 true 表示玩家一统天下，false 表示玩家败亡。</summary>
    public struct GameEnded : IGameEvent
    {
        public bool PlayerWon;
    }
}
