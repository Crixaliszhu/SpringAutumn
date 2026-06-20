namespace SpringAutumn.Core.Engine
{
    /// <summary>GameEngine 状态机（需求 4.1）。</summary>
    public enum GameState
    {
        None,
        Initializing,
        Running,
        Paused,
        Saving,
        GameOver
    }
}
