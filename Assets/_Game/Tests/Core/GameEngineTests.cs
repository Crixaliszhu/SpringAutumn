using System.Collections.Generic;
using NUnit.Framework;
using SpringAutumn.Core.Engine;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;
using SpringAutumn.Systems;

namespace SpringAutumn.Tests.Core
{
    /// <summary>GameEngine + SystemManager + Tick 框架测试（需求 4.3、4.4、4.5、4.6）。</summary>
    public class GameEngineTests
    {
        private static GameEngine CreateRunning()
        {
            var engine = new GameEngine();
            engine.Initialize(new WorldRuntime(), new SystemManager());
            return engine;
        }

        [Test]
        public void NextMonth_12Times_AdvancesFromYear1Month1_ToYear2Month1()
        {
            var engine = CreateRunning();
            for (int i = 0; i < 12; i++)
            {
                engine.NextMonth();
            }
            Assert.AreEqual(2, engine.World.Time.Year);
            Assert.AreEqual(1, engine.World.Time.Month);
        }

        [Test]
        public void NextMonth_WithEmptySystemList_DoesNotThrow()
        {
            var engine = CreateRunning();
            Assert.DoesNotThrow(() => engine.NextMonth());
            Assert.AreEqual(1, engine.World.Time.Year);
            Assert.AreEqual(2, engine.World.Time.Month);
        }

        [Test]
        public void NextMonth_WhenPaused_DoesNotAdvanceTime()
        {
            var engine = CreateRunning();
            engine.Pause();
            engine.NextMonth();
            Assert.AreEqual(1, engine.World.Time.Month, "Paused engine should not advance time");
        }

        [Test]
        public void NextMonth_PlayerOwnsAllSettlements_EndsWithVictory()
        {
            var world = new WorldRuntime();
            world.Nations.Add(new NationState("PLAYER"));
            world.Settlements.Add(new SettlementState("S1") { OwnerId = "PLAYER" });
            world.Settlements.Add(new SettlementState("S2") { OwnerId = "PLAYER" });

            var events = new EventBus();
            bool? playerWon = null;
            events.Subscribe<GameEnded>(e => playerWon = e.PlayerWon);

            var engine = new GameEngine();
            engine.Initialize(world, new SystemManager(), events);
            engine.NextMonth();

            Assert.AreEqual(GameState.GameOver, engine.State);
            Assert.IsTrue(playerWon.HasValue && playerWon.Value, "占领全部据点应判定为胜利");
        }

        [Test]
        public void NextMonth_PlayerOwnsNoSettlements_EndsWithDefeat()
        {
            var world = new WorldRuntime();
            world.Nations.Add(new NationState("PLAYER"));
            world.Nations.Add(new NationState("QIN"));
            world.Settlements.Add(new SettlementState("S1") { OwnerId = "QIN" });

            var events = new EventBus();
            bool? playerWon = null;
            events.Subscribe<GameEnded>(e => playerWon = e.PlayerWon);

            var engine = new GameEngine();
            engine.Initialize(world, new SystemManager(), events);
            engine.NextMonth();

            Assert.AreEqual(GameState.GameOver, engine.State);
            Assert.IsTrue(playerWon.HasValue && !playerWon.Value, "失去全部据点应判定为失败");
        }

        [Test]
        public void NextMonth_MixedOwnership_DoesNotEndGame()
        {
            var world = new WorldRuntime();
            world.Nations.Add(new NationState("PLAYER"));
            world.Nations.Add(new NationState("QIN"));
            world.Settlements.Add(new SettlementState("S1") { OwnerId = "PLAYER" });
            world.Settlements.Add(new SettlementState("S2") { OwnerId = "QIN" });

            bool ended = false;
            var events = new EventBus();
            events.Subscribe<GameEnded>(_ => ended = true);

            var engine = new GameEngine();
            engine.Initialize(world, new SystemManager(), events);
            engine.NextMonth();

            Assert.AreEqual(GameState.Running, engine.State);
            Assert.IsFalse(ended, "仍有他国据点时游戏不应结束");
        }

        [Test]
        public void SystemManager_ExecutesInRegistrationOrder()
        {
            var order = new List<string>();
            var sm = new SystemManager();
            sm.Register(new TrackingSystem("A", order));
            sm.Register(new TrackingSystem("B", order));
            sm.Register(new TrackingSystem("C", order));

            sm.ExecuteTick(new WorldRuntime());

            Assert.AreEqual(3, order.Count);
            Assert.AreEqual("A", order[0]);
            Assert.AreEqual("B", order[1]);
            Assert.AreEqual("C", order[2]);
        }

        [Test]
        public void GameState_Transitions_AreCorrect()
        {
            var engine = new GameEngine();
            Assert.AreEqual(GameState.None, engine.State);

            engine.Initialize(new WorldRuntime(), new SystemManager());
            Assert.AreEqual(GameState.Running, engine.State);

            engine.Pause();
            Assert.AreEqual(GameState.Paused, engine.State);

            engine.Resume();
            Assert.AreEqual(GameState.Running, engine.State);
        }

        private sealed class TrackingSystem : IGameSystem
        {
            private readonly string _name;
            private readonly List<string> _order;

            public TrackingSystem(string name, List<string> order)
            {
                _name = name;
                _order = order;
            }

            public void Execute(WorldRuntime world) => _order.Add(_name);
        }
    }
}
