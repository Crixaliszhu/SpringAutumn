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
        private const string PlayerNationId = "PLAYER";

        public GameState State { get; private set; } = GameState.None;
        public WorldRuntime World { get; private set; }
        public SystemManager Systems { get; private set; }
        public EventBus Events { get; private set; } = new EventBus();

        /// <summary>
        /// 初始化引擎。传入已由 WorldFactory 创建的世界与已注册好 System 的 SystemManager。
        /// </summary>
        public void Initialize(WorldRuntime world, SystemManager systems, EventBus eventBus = null)
        {
            World = world;
            Systems = systems;
            Events = eventBus ?? Events;
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

            // 月度结算后判定游戏是否结束（玩家一统天下 / 败亡）。
            CheckGameEnd();
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

        /// <summary>
        /// 判定游戏结束：玩家占领全部据点为胜利，玩家失去全部据点为失败。
        /// 仅在世界中存在 PLAYER 势力且有据点时评估，避免空世界（测试）误触发。
        /// </summary>
        private void CheckGameEnd()
        {
            if (State == GameState.GameOver || World == null)
                return;
            if (!World.Nations.Contains(PlayerNationId))
                return;

            int total = 0;
            int playerOwned = 0;
            foreach (var settlement in World.Settlements.GetAll())
            {
                total++;
                if (settlement.OwnerId == PlayerNationId)
                    playerOwned++;
            }

            if (total == 0)
                return;

            bool playerWon;
            if (playerOwned == total)
                playerWon = true;
            else if (playerOwned == 0)
                playerWon = false;
            else
                return;

            State = GameState.GameOver;
            Events.Publish(new GameEnded { PlayerWon = playerWon });
            GameLogger.Log(LogModule.Engine, playerWon ? "game over: player unified the realm" : "game over: player defeated");
        }
    }
}
