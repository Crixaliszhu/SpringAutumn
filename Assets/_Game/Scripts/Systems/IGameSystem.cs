using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>
    /// 统一游戏系统接口。每个系统在 Game Tick 中按固定注册顺序执行，
    /// 读取并修改 WorldRuntime（需求 4.2）。
    /// </summary>
    public interface IGameSystem
    {
        void Execute(WorldRuntime world);
    }
}
