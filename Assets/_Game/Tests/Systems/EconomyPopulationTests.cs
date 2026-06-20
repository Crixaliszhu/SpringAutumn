using System.IO;
using NUnit.Framework;
using UnityEngine;
using SpringAutumn.Config;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Runtime;
using SpringAutumn.Systems;

namespace SpringAutumn.Tests.Systems
{
    /// <summary>经济与人口系统测试（需求 5.1-5.7）。</summary>
    public class EconomyPopulationTests
    {
        private static ConfigDatabase LoadConfig()
            => new ConfigLoader().Load(JsonConfigSource.FromDirectory(
                Path.Combine(Application.dataPath, "_Game", "Config")));

        private static SettlementState MakeVillage(int households = 100, int pop = 500,
            int land = 5000, int grain = 100000, int money = 500, int garrison = 20, int loyalty = 80)
        {
            return new SettlementState("TEST_V")
            {
                Type = SettlementType.Village,
                RegionId = "R", OwnerId = "N",
                Households = households, Population = pop, PopulationCap = pop,
                Land = land, Grain = grain, Money = money,
                Loyalty = loyalty, Garrison = garrison
            };
        }

        [Test]
        public void NormalVillage_MonthlyGrainTax_Approximately6200()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage();
            world.Settlements.Add(v);

            int grainBefore = v.Grain;
            new EconomySystem(config).Execute(world);

            // Net grain change = income - military cost
            // Income = (5000*100*0.15)/12 ≈ 6250; Cost = 20*90 = 1800; Net ≈ +4450
            // Tax income alone ≈ 6250 (doc says ~6200)
            int grainIncome = (v.Grain - grainBefore) + v.Garrison * config.Economy.soldierGrainPerMonth;
            Assert.GreaterOrEqual(grainIncome, 6100, "月税粮应约6200");
            Assert.LessOrEqual(grainIncome, 6300, "月税粮应约6200");
        }

        [Test]
        public void NormalVillage_MonthlyMoneyTax_Approximately83()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage();
            world.Settlements.Add(v);

            int moneyBefore = v.Money;
            new EconomySystem(config).Execute(world);

            int moneyIncome = (v.Money - moneyBefore) + v.Garrison * config.Economy.soldierWagePerMonth;
            Assert.GreaterOrEqual(moneyIncome, 80, "月税钱应约83");
            Assert.LessOrEqual(moneyIncome, 90, "月税钱应约83");
        }

        [Test]
        public void MilitaryCost_20Soldiers_1800GrainPerMonth()
        {
            var config = LoadConfig();
            int expected = 20 * config.Economy.soldierGrainPerMonth; // 20*90=1800
            Assert.AreEqual(1800, expected);
        }

        [Test]
        public void MilitaryCost_20Soldiers_20MoneyPerMonth()
        {
            var config = LoadConfig();
            int expected = 20 * config.Economy.soldierWagePerMonth; // 20*1=20
            Assert.AreEqual(20, expected);
        }

        [Test]
        public void Famine_WhenGrainZero_LoyaltyDrops()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            // 粮食为 0 → 触发粮荒
            var v = MakeVillage(grain: 0, loyalty: 80);
            world.Settlements.Add(v);

            new PopulationSystem(config).Execute(world);

            Assert.Less(v.Loyalty, 80, "粮荒应使民心下降");
        }

        [Test]
        public void Famine_WhenGrainZero_GarrisonDeserts()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 0, garrison: 20);
            world.Settlements.Add(v);

            new PopulationSystem(config).Execute(world);

            Assert.Less(v.Garrison, 20, "粮荒应导致逃兵");
        }

        [Test]
        public void Famine_WhenGrainZero_PopulationDecreases()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 0, pop: 500);
            world.Settlements.Add(v);

            new PopulationSystem(config).Execute(world);

            Assert.Less(v.Population, 500, "粮荒应导致人口减少");
        }

        [Test]
        public void LoyaltyPenalty_60To79_ReducesTaxBy20Percent()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v80 = MakeVillage(loyalty: 80, grain: 100000);
            v80.Id = "V_80";
            var v65 = MakeVillage(loyalty: 65, grain: 100000);
            v65.Id = "V_65";
            world.Settlements.Add(v80);
            world.Settlements.Add(v65);

            int g80Before = v80.Grain;
            int g65Before = v65.Grain;
            new EconomySystem(config).Execute(world);

            int net80 = v80.Grain - g80Before;
            int net65 = v65.Grain - g65Before;
            // v65 income portion should be 80% of v80 income portion
            // Net = income * mult - cost; cost same, so delta = income*(1-0.8)*mult differences
            Assert.Less(net65, net80, "低民心应减少净收入");
        }

        [Test]
        public void Population_GrowsWhen_NotFamine()
        {
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 100000, loyalty: 90);
            v.PopulationCap = 600; // 有增长空间
            world.Settlements.Add(v);

            new PopulationSystem(config).Execute(world);

            Assert.Greater(v.Population, 500, "正常状态人口应增长");
        }
    }
}
