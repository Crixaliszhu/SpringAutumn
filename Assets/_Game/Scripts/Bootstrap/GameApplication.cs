using System;
using SpringAutumn.Config;
using SpringAutumn.Core.Engine;
using SpringAutumn.Core.Events;
using SpringAutumn.Core.Utils;
using SpringAutumn.Runtime;
using SpringAutumn.Save;
using SpringAutumn.Systems;

namespace SpringAutumn.Bootstrap
{
    /// <summary>
    /// 纯 C# 游戏应用层，负责把 Config、WorldRuntime、GameEngine、SystemManager、SaveManager 串成完整启动链路。
    /// Unity 侧 GameLauncher 只负责提供路径与 Update(deltaTime) 驱动。
    /// </summary>
    public class GameApplication
    {
        public const float DefaultSecondsPerMonth = 5f;
        public const int AutoSaveSlot = 1;

        private readonly WorldFactory _worldFactory;
        private float _tickAccumulator;

        public ConfigDatabase Config { get; }
        public SaveManager SaveManager { get; }
        public GameEngine Engine { get; private set; }
        public EventBus Events { get; }
        public float SecondsPerMonth { get; set; } = DefaultSecondsPerMonth;
        public bool AutoSaveAfterTick { get; set; } = true;

        public WorldRuntime World => Engine?.World;
        public bool IsRunning => Engine != null && Engine.State == GameState.Running;
        public bool IsPaused => Engine != null && Engine.State == GameState.Paused;

        public GameApplication(ConfigDatabase config, SaveManager saveManager, EventBus eventBus = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            SaveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            Events = eventBus ?? new EventBus();
            _worldFactory = new WorldFactory();
        }

        public GameEngine NewGame()
        {
            WorldRuntime world = _worldFactory.CreateNewWorld(Config);
            return StartGame(world);
        }

        public GameEngine LoadGame(int slot)
        {
            WorldRuntime world = SaveManager.Load(slot);
            if (world == null)
            {
                GameLogger.Warn(LogModule.Save, SaveManager.LastError ?? "load failed");
                return null;
            }

            return StartGame(world);
        }

        public GameEngine StartGame(WorldRuntime world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            _tickAccumulator = 0f;
            Engine = new GameEngine();
            Engine.Initialize(world, CreateDefaultSystems(Config, Events), Events);
            return Engine;
        }

        public int Update(float deltaSeconds)
        {
            if (!IsRunning || deltaSeconds <= 0f)
                return 0;

            _tickAccumulator += deltaSeconds;
            int ticks = 0;

            while (_tickAccumulator >= SecondsPerMonth)
            {
                Engine.NextMonth();
                ticks++;
                _tickAccumulator -= SecondsPerMonth;

                if (AutoSaveAfterTick)
                    SaveManager.Save(Engine.World, AutoSaveSlot);
            }

            return ticks;
        }

        public void Pause()
        {
            Engine?.Pause();
        }

        public void Resume()
        {
            Engine?.Resume();
        }

        public bool Save(int slot)
        {
            return Engine != null && SaveManager.Save(Engine.World, slot);
        }

        public bool ExitGame(int slot = AutoSaveSlot)
        {
            if (Engine == null)
                return false;

            bool saved = SaveManager.Save(Engine.World, slot);
            Engine.Pause();
            return saved;
        }

        public static SystemManager CreateDefaultSystems(ConfigDatabase config, EventBus eventBus)
        {
            var systems = new SystemManager();
            systems.Register(new PopulationSystem(config));
            systems.Register(new EconomySystem(config));
            systems.Register(new ConstructionSystem(config, eventBus));
            systems.Register(new RecruitSystem(config, eventBus));
            systems.Register(new ArmySystem(eventBus));
            systems.Register(new BattleSystem(config, eventBus));
            systems.Register(new DiplomacySystem());
            systems.Register(new AISystem(config));
            return systems;
        }
    }
}
