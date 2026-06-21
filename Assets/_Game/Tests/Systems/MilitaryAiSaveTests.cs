using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.AI;
using SpringAutumn.Commands;
using SpringAutumn.Config;
using SpringAutumn.Core.Utils;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Runtime;
using SpringAutumn.Save;
using SpringAutumn.Systems;

namespace SpringAutumn.Tests.Systems
{
    public class MilitaryAiSaveTests
    {
        private static ConfigDatabase LoadConfig()
            => new ConfigLoader().Load(JsonConfigSource.FromDirectory(
                Path.Combine(Application.dataPath, "_Game", "Config")));

        private static WorldRuntime NewWorld()
            => new WorldFactory().CreateNewWorld(LoadConfig());

        [Test]
        public void M6_MoveArmyCommand_RespectsDrawLimitAndMinimumGarrison()
        {
            var config = LoadConfig();
            var world = NewWorld();
            var source = world.Settlements.Get("V_PLAYER_001");
            source.Garrison = 20;

            var tooMany = new MoveArmyCommand("PLAYER", source.Id, "NEU_R01", 11, config);
            Assert.IsFalse(tooMany.Validate(world), "抽调不得超过守军 50% 且必须保留最低驻军");

            var valid = new MoveArmyCommand("PLAYER", source.Id, "NEU_R01", 10, config);
            Assert.IsTrue(valid.Validate(world));
            valid.Execute(world);

            Assert.AreEqual(10, source.Garrison);
            Assert.AreEqual(1, world.Armies.Count);
        }

        [Test]
        public void M6_ArmyMovesOneAdjacentRegionPerMonth()
        {
            var config = LoadConfig();
            var world = NewWorld();
            var source = world.Settlements.Get("CITY_QIN_002");
            source.Garrison = 200;

            var cmd = new MoveArmyCommand("QIN", source.Id, "ZHOU_R01", 80, config);
            Assert.IsTrue(cmd.Validate(world));
            cmd.Execute(world);

            var army = world.Armies.GetAll().First();
            Assert.AreEqual("QIN_R02", army.CurrentRegionId);

            new ArmySystem().Execute(world);

            Assert.AreEqual("ZHOU_R01", army.CurrentRegionId);
            Assert.AreEqual(ArmyStatus.Idle, army.Status);
        }

        [Test]
        public void M6_BattleCapturesCoreCityAndSynchronizesRegionOwner()
        {
            var config = LoadConfig();
            var world = NewWorld();
            var source = world.Settlements.Get("CITY_QIN_002");
            source.Garrison = 400;
            var target = world.Settlements.Get("CITY_ZHOU_001");
            target.Garrison = 10;

            var move = new MoveArmyCommand("QIN", source.Id, "ZHOU_R01", 150, config);
            move.Execute(world);
            var army = world.Armies.GetAll().First();
            new ArmySystem().Execute(world);

            var attack = new AttackCommand("QIN", army.Id, target.Id);
            Assert.IsTrue(attack.Validate(world));
            attack.Execute(world);

            new BattleSystem(config).Execute(world);

            var region = world.Regions.Get("ZHOU_R01");
            Assert.AreEqual("QIN", region.OwnerId);
            Assert.AreEqual("QIN", target.OwnerId);
            foreach (var villageId in region.VillageIds)
                Assert.AreEqual("QIN", world.Settlements.Get(villageId).OwnerId);
            Assert.AreEqual(config.Battle.captureLoyalty, target.Loyalty);
        }

        [Test]
        public void M7_DiplomacySystem_MarksWarWhenRelationIsVeryLow()
        {
            var world = NewWorld();
            world.Diplomacy.SetRelation("QIN", "JIN", -100);

            new DiplomacySystem().Execute(world);

            Assert.AreEqual(WarStatus.War, world.Nations.Get("QIN").WarStatus);
            Assert.AreEqual(WarStatus.War, world.Nations.Get("JIN").WarStatus);
            Assert.AreEqual(RelationStatus.War, world.Diplomacy.GetStatus("JIN", "QIN"));
        }

        [Test]
        public void M7_AISystem_GeneratesCommandsWithoutDirectlySpendingResources()
        {
            var config = LoadConfig();
            var world = NewWorld();
            var settlement = world.Settlements.Get("V_QIN_001");
            settlement.Money = 2000;
            settlement.Grain = 500000;
            int moneyBefore = settlement.Money;

            new AISystem(config).Execute(world);

            Assert.Greater(world.Commands.Count, 0, "AI 应生成 Command");
            Assert.AreEqual(moneyBefore, settlement.Money, "AI 不应直接修改资源，资源变化必须等命令执行");
        }

        [Test]
        public void M7_AIEvaluator_IsDeterministicForSameWorld()
        {
            var worldA = NewWorld();
            var worldB = NewWorld();

            Assert.AreEqual(
                AIEvaluator.CalculatePower(worldA, "QIN"),
                AIEvaluator.CalculatePower(worldB, "QIN"));
        }

        [Test]
        public void M8_SaveConverter_RoundTripsRuntimeState()
        {
            var config = LoadConfig();
            var world = NewWorld();
            var settlement = world.Settlements.Get("V_PLAYER_001");
            settlement.Grain += 1234;
            settlement.Buildings.Add(new BuildingInstance("FARM", 1));
            settlement.ConstructionQueue.Add(new ConstructionTask("HOUSE", 2));
            settlement.RecruitQueue.Add(new RecruitTask(7, 1));
            world.Diplomacy.SetRelation("PLAYER", "CHU", -75);
            world.Events.Active.Add(new OngoingEvent("DROUGHT", "PLAYER_R01", 3));
            world.Commands.Enqueue(new RecruitCommand("PLAYER", settlement.Id, 5, config));
            world.Random = new DeterministicRandom(42u);

            var converter = new SaveConverter();
            var save = converter.ToSave(world, 1);
            var restored = converter.Restore(save, config);

            var restoredSettlement = restored.Settlements.Get(settlement.Id);
            Assert.AreEqual(world.Time.Year, restored.Time.Year);
            Assert.AreEqual(settlement.Grain, restoredSettlement.Grain);
            Assert.AreEqual(1, restoredSettlement.Buildings.Count);
            Assert.AreEqual(1, restoredSettlement.ConstructionQueue.Count);
            Assert.AreEqual(1, restoredSettlement.RecruitQueue.Count);
            Assert.AreEqual(-75, restored.Diplomacy.GetRelation("CHU", "PLAYER"));
            Assert.AreEqual(1, restored.Events.Active.Count);
            Assert.AreEqual(1, restored.Commands.Count);
        }

        [Test]
        public void M8_SaveManager_UsesThreeSlotsAndHandlesCorruption()
        {
            var config = LoadConfig();
            var storage = new MemorySaveStorage();
            var manager = new SaveManager(config, storage);

            Assert.IsTrue(manager.Save(NewWorld(), 1));
            Assert.IsTrue(manager.Save(NewWorld(), 2));
            Assert.IsTrue(manager.Save(NewWorld(), 3));
            Assert.IsFalse(manager.Save(NewWorld(), 4));
            Assert.AreEqual(3, manager.GetSaveList().Count);

            storage.Write(2, "{ corrupted json");
            Assert.IsNull(manager.Load(2));
            Assert.IsNotNull(manager.LastError);
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
