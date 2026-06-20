using SpringAutumn.Commands;
using SpringAutumn.Core.Events;
using SpringAutumn.Core.Utils;
using SpringAutumn.Runtime;

namespace SpringAutumn.Core.Engine
{
    /// <summary>
    /// 游戏核心引擎。纯 C#，不继承 MonoBehaviour。
    /// 管理 WorldRuntime、SystemManager、CommandQueue、EventBus；驱动月度 Tick 循环（需求 4.3、4.5、11.2）。
    /// </summary>
    public class GameEngine
    {
        public GameState State { get; private set; } = GameState.None;
        public WorldRuntime World { get; private set; }
        public SystemManager Systems { get; private set; }
        public EventBus Events { get; private set; } = new EventBus();

        /// <summary>
        /// 初始化引擎。传入已由 WorldFactory 创建的世界与已注册好 System 的 SystemManager。
        /// </summary>
        public void Initialize(WorldRuntime world, SystemManager systems)
        {
            World = world;
            Systems = systems;
            State = GameState.Running;
            GameLogger.Log(LogModule.Engine, "initialized");
        }

        /// <summary>执行一次完整月度 Tick（需求 4.6 固定顺序）。仅 Running 状态下有效。</summary>
        public void NextMonth()
        {
            if (State != GameState.Running)
            {
                return;
            }

            // 1. 推进时间
            World.Time.AdvanceMonth();

            // 2. 执行上月命令队列
            ExecuteCommands();

            // 3-11. SystemManager 按固定注册顺序执行各 System
            Systems.ExecuteTick(World);

            // 发布 MonthChanged 事件（需求 11.2）
            Events.Publish(new MonthChanged { Year = World.Time.Year, Month = World.Time.Month });
        }

        public void Pause()
        {
            if (State == GameState.Running)
            {
                State = GameState.Paused;
            }
        }

        public void Resume()
        {
            if (State == GameState.Paused)
            {
                State = GameState.Running;
            }
        }

        /// <summary>外部提交命令（玩家 UI 或 AI）。进入队列等待下一个 Tick 执行。</summary>
        public void EnqueueCommand(GameCommand command)
        {
            World?.Commands.Enqueue(command);
        }

        private void ExecuteCommands()
        {
            var commands = World.Commands.DrainAll();
            foreach (var cmd in commands)
            {
                if (cmd.Validate(World))
                {
                    cmd.Execute(World);
                }
                else
                {
                    GameLogger.Warn(LogModule.Engine,
                        $"command rejected: {cmd.GetType().Name} from {cmd.NationId}");
                }
            }
        }
    }
}
