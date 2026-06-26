using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Config;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Runtime;
using SpringAutumn.Save;

namespace SpringAutumn.Tests.Core
{
    public class BootstrapIntegrationTests
    {
        private static ConfigDatabase LoadConfig()
            => new ConfigLoader().Load(JsonConfigSource.FromDirectory(
                Path.Combine(Application.dataPath, "_Game", "Config")));

        [Test]
        public void M9_NewGame_Run12_Save_Load_Run12_IsContinuous()
        {
            var config = LoadConfig();
            var storage = new MemorySaveStorage();
            var app = CreateApp(config, storage);

            app.NewGame();
            int ticks = app.Update(GameApplication.DefaultSecondsPerMonth * 12f);
            Assert.AreEqual(12, ticks);
            Assert.AreEqual(2, app.World.Time.Year);
            Assert.AreEqual(1, app.World.Time.Month);
            Assert.IsTrue(app.Save(2));

            var loadedApp = CreateApp(config, storage);
            Assert.IsNotNull(loadedApp.LoadGame(2));
            Assert.AreEqual(2, loadedApp.World.Time.Year);
            Assert.AreEqual(1, loadedApp.World.Time.Month);

            loadedApp.Update(GameApplication.DefaultSecondsPerMonth * 12f);
            Assert.AreEqual(3, loadedApp.World.Time.Year);
            Assert.AreEqual(1, loadedApp.World.Time.Month);
        }

        [Test]
        public void M9_SaveLoad_RestoresLoyaltyBuildingsAndConstructionQueue()
        {
            var config = LoadConfig();
            var storage = new MemorySaveStorage();
            var app = CreateApp(config, storage);
            app.NewGame();

            var settlement = app.World.Settlements.Get("V_PLAYER_001");
            settlement.Loyalty = 73;
            settlement.Buildings.Add(new BuildingInstance("FARM", 2));
            settlement.ConstructionQueue.Add(new ConstructionTask("FARM", 1));

            Assert.IsTrue(app.Save(2));

            var loadedApp = CreateApp(config, storage);
            Assert.IsNotNull(loadedApp.LoadGame(2));
            var loaded = loadedApp.World.Settlements.Get("V_PLAYER_001");

            Assert.AreEqual(73, loaded.Loyalty);
            Assert.AreEqual(1, loaded.Buildings.Count);
            Assert.AreEqual("FARM", loaded.Buildings[0].BuildingId);
            Assert.AreEqual(2, loaded.Buildings[0].Level);
            Assert.AreEqual(1, loaded.ConstructionQueue.Count);
            Assert.AreEqual("FARM", loaded.ConstructionQueue[0].BuildingId);
            Assert.AreEqual(1, loaded.ConstructionQueue[0].RemainingMonths);
        }

        [Test]
        public void M9_GameLoop_PauseStopsTick()
        {
            var app = CreateApp(LoadConfig(), new MemorySaveStorage());
            app.NewGame();

            app.Pause();
            int ticks = app.Update(GameApplication.DefaultSecondsPerMonth * 2f);

            Assert.AreEqual(0, ticks);
            Assert.AreEqual(1, app.World.Time.Month);

            app.Resume();
            ticks = app.Update(GameApplication.DefaultSecondsPerMonth);
            Assert.AreEqual(1, ticks);
            Assert.AreEqual(2, app.World.Time.Month);
        }

        [Test]
        public void M9_Headless120Months_HasNoNegativeOrNaNValues()
        {
            var app = CreateApp(LoadConfig(), new MemorySaveStorage());
            app.AutoSaveAfterTick = false;
            app.NewGame();

            Assert.DoesNotThrow(() => app.Update(GameApplication.DefaultSecondsPerMonth * 120f));

            foreach (var settlement in app.World.Settlements.GetAll())
            {
                Assert.GreaterOrEqual(settlement.Grain, 0, settlement.Id + " grain");
                Assert.GreaterOrEqual(settlement.Money, 0, settlement.Id + " money");
                Assert.GreaterOrEqual(settlement.Population, 0, settlement.Id + " population");
                Assert.GreaterOrEqual(settlement.Garrison, 0, settlement.Id + " garrison");
                Assert.GreaterOrEqual(settlement.Loyalty, 0, settlement.Id + " loyalty");
                Assert.LessOrEqual(settlement.Loyalty, 100, settlement.Id + " loyalty");
            }

            foreach (var army in app.World.Armies.GetAll())
            {
                Assert.GreaterOrEqual(army.Soldiers, 0, army.Id + " soldiers");
                Assert.GreaterOrEqual(army.Morale, 0, army.Id + " morale");
            }
        }

        [Test]
        public void M9_ExitGame_AutoSavesAndPauses()
        {
            var storage = new MemorySaveStorage();
            var app = CreateApp(LoadConfig(), storage);
            app.NewGame();

            Assert.IsTrue(app.ExitGame());
            Assert.IsTrue(storage.Exists(GameApplication.AutoSaveSlot));
            Assert.IsTrue(app.IsPaused);
        }

        private static GameApplication CreateApp(ConfigDatabase config, ISaveStorage storage)
        {
            return new GameApplication(config, new SaveManager(config, storage))
            {
                SecondsPerMonth = GameApplication.DefaultSecondsPerMonth
            };
        }

        private class MemorySaveStorage : ISaveStorage
        {
            private readonly Dictionary<int, string> _files = new Dictionary<int, string>();

            public bool Exists(int slot) => _files.ContainsKey(slot);
            public string Read(int slot) => _files[slot];
            public void Write(int slot, string json) => _files[slot] = json;
            public void Delete(int slot) => _files.Remove(slot);
        }
    }
}
