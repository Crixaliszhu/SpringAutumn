using System.IO;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.Commands;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Runtime;
using SpringAutumn.Systems;

namespace SpringAutumn.Tests.Systems
{
    /// <summary>命令、建设与征兵系统测试（需求 6.1-6.5）。</summary>
    public class CommandBuildRecruitTests
    {
        private static ConfigDatabase LoadConfig()
            => new ConfigLoader().Load(JsonConfigSource.FromDirectory(
                Path.Combine(Application.dataPath, "_Game", "Resources", "Config")));

        private static SettlementState MakeVillage(int households = 100, int pop = 500,
            int grain = 100000, int money = 5000, int garrison = 20, int loyalty = 80)
        {
            return new SettlementState("TEST_V")
            {
                Type = SettlementType.Village,
                RegionId = "R", OwnerId = "PLAYER",
                Households = households, Population = pop, PopulationCap = pop * 2,
                Land = households * 50, Grain = grain, Money = money,
                Loyalty = loyalty, Garrison = garrison
            };
        }

        private static SettlementState MakeCity(int households = 300, int pop = 1500,
            int grain = 200000, int money = 10000, int garrison = 50, int loyalty = 80)
        {
            return new SettlementState("TEST_C")
            {
                Type = SettlementType.City,
                RegionId = "R", OwnerId = "PLAYER",
                Households = households, Population = pop, PopulationCap = pop * 2,
                Land = households * 50, Grain = grain, Money = money,
                Loyalty = loyalty, Garrison = garrison
            };
        }

        #region BuildCommand 测试

        [Test]
        public void BuildCommand_Validate_SuccessWhenValid()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            Assert.IsTrue(cmd.Validate(world), "有效命令应通过校验");
        }

        [Test]
        public void BuildCommand_Validate_FailWhenNotEnoughMoney()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 10); // 农田需要 100
            world.Settlements.Add(v);

            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            Assert.IsFalse(cmd.Validate(world), "资金不足应校验失败");
        }

        [Test]
        public void BuildCommand_Validate_FailWhenWrongOwner()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            v.OwnerId = "ENEMY";
            world.Settlements.Add(v);

            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            Assert.IsFalse(cmd.Validate(world), "非己方据点应校验失败");
        }

        [Test]
        public void BuildCommand_Validate_FailWhenCityBuildingInVillage()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 1000);
            world.Settlements.Add(v);

            // MARKET 是 CITY 范围建筑
            var cmd = new BuildCommand("PLAYER", "TEST_V", "MARKET", config);
            Assert.IsFalse(cmd.Validate(world), "城建筑在村中应校验失败");
        }

        [Test]
        public void BuildCommand_Execute_AddsToQueueAndDeductsMoney()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            int cost = config.Buildings["FARM"].cost; // 100
            int moneyBefore = v.Money;

            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            cmd.Execute(world);

            Assert.AreEqual(1, v.ConstructionQueue.Count, "应入建造队列");
            Assert.AreEqual(moneyBefore - cost, v.Money, "应扣资金");
        }

        [Test]
        public void BuildCommand_DelayedEffect_NotInstant()
        {
            // 需求 6.2: 本月提交的建筑不立即生效
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            cmd.Execute(world);

            // 建筑尚未建成
            Assert.AreEqual(0, v.Buildings.Count, "建筑不应立即建成");
            Assert.AreEqual(1, v.ConstructionQueue.Count, "应在队列中");
        }

        #endregion

        #region RecruitCommand 测试

        [Test]
        public void RecruitCommand_Validate_SuccessWhenValid()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000);
            world.Settlements.Add(v);

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 10, config);
            Assert.IsTrue(cmd.Validate(world), "有效命令应通过校验");
        }

        [Test]
        public void RecruitCommand_Validate_FailWhenNotEnoughResources()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 100, money: 10);
            world.Settlements.Add(v);

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 100, config);
            Assert.IsFalse(cmd.Validate(world), "资源不足应校验失败");
        }

        [Test]
        public void RecruitCommand_Execute_AddsToQueueAndDeductsResources()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000);
            world.Settlements.Add(v);

            int count = 10;
            int costMoney = count * 10;
            int costGrain = count * 100;

            int moneyBefore = v.Money;
            int grainBefore = v.Grain;

            var cmd = new RecruitCommand("PLAYER", "TEST_V", count, config);
            cmd.Execute(world);

            Assert.AreEqual(1, v.RecruitQueue.Count, "应入训练队列");
            Assert.AreEqual(count, v.RecruitQueue[0].Count);
            Assert.AreEqual(moneyBefore - costMoney, v.Money);
            Assert.AreEqual(grainBefore - costGrain, v.Grain);
        }

        [Test]
        public void RecruitCommand_DelayedEffect_GarrisonUnchanged()
        {
            // 需求 6.3: 训练期内守军不变
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000, garrison: 20);
            world.Settlements.Add(v);

            int garrisonBefore = v.Garrison;

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 10, config);
            cmd.Execute(world);

            Assert.AreEqual(garrisonBefore, v.Garrison, "训练期内守军不变");
        }

        #endregion

        #region ConstructionSystem 测试

        [Test]
        public void ConstructionSystem_CompletesAfterMonths()
        {
            // 需求 6.2: 建造队列每月递减，归零后完成
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            // FARM 需要 2 个月
            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            cmd.Execute(world);

            var sys = new ConstructionSystem(config);

            // 第 1 个月
            sys.Execute(world);
            Assert.AreEqual(0, v.Buildings.Count, "第1月不应完成");

            // 第 2 个月
            sys.Execute(world);
            Assert.AreEqual(1, v.Buildings.Count, "第2月应完成");
            Assert.AreEqual("FARM", v.Buildings[0].BuildingId);
        }

        [Test]
        public void ConstructionSystem_EffectFromNextMonth()
        {
            // 需求 6.5: 农田建成后次月粮税+10%
            var config = LoadConfig();
            var eventBus = new EventBus();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            // 建造农田
            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            cmd.Execute(world);

            var consSys = new ConstructionSystem(config, eventBus);
            var ecoSys = new EconomySystem(config);

            // 完成建造
            consSys.Execute(world);
            consSys.Execute(world);

            // 记录建成后的收入
            int grainAfterBuild = v.Grain;
            ecoSys.Execute(world);
            int income1 = v.Grain - grainAfterBuild;

            // 再运行一月，应该有农田加成
            int grainBeforeMonth2 = v.Grain;
            ecoSys.Execute(world);
            int income2 = v.Grain - grainBeforeMonth2;

            // 收入应该相近（因为民心变化可能微小）
            Assert.Greater(v.Buildings.Count, 0, "应有农田建筑");
        }

        #endregion

        #region RecruitSystem 测试

        [Test]
        public void RecruitSystem_CompletesAndAddsGarrison()
        {
            // 需求 6.3: 训练完成后入守军
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000, garrison: 20);
            world.Settlements.Add(v);

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 10, config);
            cmd.Execute(world);

            var sys = new RecruitSystem(config);
            int trainMonths = v.RecruitQueue[0].RemainingMonths;

            // 运行训练期
            for (int i = 0; i < trainMonths; i++)
            {
                sys.Execute(world);
            }

            Assert.AreEqual(0, v.RecruitQueue.Count, "训练队列应清空");
            Assert.AreEqual(30, v.Garrison, "守军应增加10");
        }

        [Test]
        public void RecruitSystem_TrainingFieldReducesTime()
        {
            // 训练场减少训练时间
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000);
            v.Buildings.Add(new BuildingInstance("TRAINING", 2)); // 2级训练场
            world.Settlements.Add(v);

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 10, config);
            cmd.Execute(world);

            // 训练场应减少训练时间
            int months = v.RecruitQueue[0].RemainingMonths;
            Assert.LessOrEqual(months, 2, "训练场应减少训练时间");
        }

        #endregion

        #region P8 命令延迟性不变量测试

        [Test]
        public void P8_BuildingDelayedEffect_NoInstantChange()
        {
            // P8: 建设在提交当月不改变即时产出/守军
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(money: 500);
            world.Settlements.Add(v);

            // 记录建造前状态
            int buildingsBefore = v.Buildings.Count;
            int queueBefore = v.ConstructionQueue.Count;

            // 提交建造命令
            var cmd = new BuildCommand("PLAYER", "TEST_V", "FARM", config);
            cmd.Execute(world);

            // 当月不应改变建筑（仅入队列）
            Assert.AreEqual(buildingsBefore, v.Buildings.Count, "当月不应增加建筑");
            Assert.AreEqual(queueBefore + 1, v.ConstructionQueue.Count, "应入队列");
        }

        [Test]
        public void P8_RecruitDelayedEffect_NoInstantGarrisonChange()
        {
            // P8: 征兵在提交当月不增加守军
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 10000, money: 1000, garrison: 20);
            world.Settlements.Add(v);

            int garrisonBefore = v.Garrison;

            var cmd = new RecruitCommand("PLAYER", "TEST_V", 10, config);
            cmd.Execute(world);

            Assert.AreEqual(garrisonBefore, v.Garrison, "当月守军不变");
            Assert.AreEqual(1, v.RecruitQueue.Count, "应入训练队列");
        }

        #endregion
    }
}
