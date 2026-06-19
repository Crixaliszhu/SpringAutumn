namespace SpringAutumn.Runtime
{
    /// <summary>国家 AI 战略状态。</summary>
    public enum NationAIState
    {
        Weak,        // 虚弱：粮食不足，停止扩张
        Developing,  // 发展：经济正常
        Strong,      // 强盛：实力超过邻国，准备战争
        War,         // 战争：已存在敌对关系
        Collapsing   // 崩溃：连续饥荒或大量失地
    }

    /// <summary>国家整体战争状态。</summary>
    public enum WarStatus
    {
        Peace,
        War
    }

    /// <summary>野战军状态。</summary>
    public enum ArmyStatus
    {
        Idle,       // 待命
        Marching,   // 行军
        Sieging,    // 围城
        Retreating, // 撤退
        Disbanded   // 已解散
    }
}
