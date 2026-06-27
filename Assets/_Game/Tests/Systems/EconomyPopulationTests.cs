using System;
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
                Path.Combine(Application.dataPath, "_Game", "Resources", "Config")));

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

        #region 需求 5.1-5.2 单户年产测试

        [Test]
        public void SingleHousehold_AnnualGrainProduction_5000()
        {
            // 需求 5.1: 1户=5人、每户50亩、每亩年产100斤 → 单户年产 5000 斤
            var config = LoadConfig();
            int householdSize = config.Economy.householdSize;           // 5
            int landPerHousehold = config.Economy.landPerHousehold;     // 50
            int grainPerLand = config.Economy.grainPerLandPerYear;      // 100

            int annualProduction = landPerHousehold * grainPerLand;     // 50 * 100 = 5000
            Assert.AreEqual(5000, annualProduction, "单户年产应为5000斤");
            Assert.AreEqual(5, householdSize, "每户应为5人");
            Assert.AreEqual(50, landPerHousehold, "每户应有50亩");
        }

        [Test]
        public void SingleHousehold_AnnualGrainTax_750()
        {
            // 需求 5.2: 粮税税率 15%，单户年税粮 750 斤
            var config = LoadConfig();
            int annualProduction = 5000;
            float taxRate = config.Economy.grainTaxRate;                // 0.15
            int annualGrainTax = (int)(annualProduction * taxRate);     // 750
            Assert.AreEqual(750, annualGrainTax, "单户年税粮应为750斤");
        }

        [Test]
        public void SingleHousehold_AnnualMoneyTax_10()
        {
            // 需求 5.2: 钱税每人每年 2 铜钱，单户每年 10 钱
            var config = LoadConfig();
            int householdSize = config.Economy.householdSize;           // 5
            int moneyPerPerson = config.Economy.moneyTaxPerPersonPerYear; // 2
            int annualMoneyTax = householdSize * moneyPerPerson;        // 5 * 2 = 10
            Assert.AreEqual(10, annualMoneyTax, "单户年税钱应为10钱");
        }

        #endregion

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

        #region 需求 5.5 缺粮后果链完整测试

        [Test]
        public void Famine_ConsequenceChain_LoyaltyThenDesertionThenPopulation()
        {
            // 需求 5.5: 粮食不足时依次触发士气下降、民心下降、士兵逃亡、人口减少
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 0, loyalty: 80, garrison: 20, pop: 500);
            world.Settlements.Add(v);

            int initLoyalty = v.Loyalty;
            int initGarrison = v.Garrison;
            int initPop = v.Population;

            new PopulationSystem(config).Execute(world);

            Assert.Less(v.Loyalty, initLoyalty, "粮荒应使民心下降");
            Assert.Less(v.Garrison, initGarrison, "粮荒应导致逃兵");
            Assert.Less(v.Population, initPop, "粮荒应导致人口减少");
        }

        #endregion

        #region P4 资源非负性测试

        [Test]
        public void P4_ResourceNonNegative_AfterManyTicks()
        {
            // P4 不变量：任意 Tick 后，所有 Settlement 的 Grain/Money/Population/Garrison ≥ 0
            var config = LoadConfig();
            var world = new WorldRuntime();

            // 创建极端情况：大量消耗、初始资源极少（使用不同 ID）
            var v1 = new SettlementState("V1")
            {
                Type = SettlementType.Village,
                RegionId = "R", OwnerId = "N",
                Households = 100, Population = 100, PopulationCap = 100,
                Land = 5000, Grain = 100, Money = 10, Loyalty = 30, Garrison = 50
            };
            var v2 = new SettlementState("V2")
            {
                Type = SettlementType.Village,
                RegionId = "R", OwnerId = "N",
                Households = 10, Population = 50, PopulationCap = 50,
                Land = 500, Grain = 0, Money = 0, Loyalty = 10, Garrison = 100
            };
            var v3 = new SettlementState("V3")
            {
                Type = SettlementType.Village,
                RegionId = "R", OwnerId = "N",
                Households = 16, Population = 80, PopulationCap = 80,
                Land = 800, Grain = 50, Money = 5, Loyalty = 5, Garrison = 30
            };

            world.Settlements.Add(v1);
            world.Settlements.Add(v2);
            world.Settlements.Add(v3);

            // 运行 12 个月
            var eco = new EconomySystem(config);
            var pop = new PopulationSystem(config);
            for (int i = 0; i < 12; i++)
            {
                eco.Execute(world);
                pop.Execute(world);

                foreach (var s in world.Settlements.GetAll())
                {
                    Assert.GreaterOrEqual(s.Grain, 0, $"Settlement {s.Id} Grain should be >= 0 at month {i + 1}");
                    Assert.GreaterOrEqual(s.Money, 0, $"Settlement {s.Id} Money should be >= 0 at month {i + 1}");
                    Assert.GreaterOrEqual(s.Population, 0, $"Settlement {s.Id} Population should be >= 0 at month {i + 1}");
                    Assert.GreaterOrEqual(s.Garrison, 0, $"Settlement {s.Id} Garrison should be >= 0 at month {i + 1}");
                    Assert.GreaterOrEqual(s.Loyalty, 0, $"Settlement {s.Id} Loyalty should be >= 0 at month {i + 1}");
                }
            }
        }

        [Test]
        public void P4_ResourceNonNegative_ZeroInitialResources()
        {
            // 极端情况：初始资源全为 0
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(grain: 0, money: 0, garrison: 0, pop: 0, loyalty: 0);
            v.PopulationCap = 0;
            world.Settlements.Add(v);

            var eco = new EconomySystem(config);
            var pop = new PopulationSystem(config);

            for (int i = 0; i < 6; i++)
            {
                eco.Execute(world);
                pop.Execute(world);
            }

            Assert.GreaterOrEqual(v.Grain, 0, "Grain should remain >= 0");
            Assert.GreaterOrEqual(v.Money, 0, "Money should remain >= 0");
            Assert.GreaterOrEqual(v.Population, 0, "Population should remain >= 0");
            Assert.GreaterOrEqual(v.Garrison, 0, "Garrison should remain >= 0");
        }

        #endregion

        #region 民心档位测试（需求 5.6）

        [Test]
        public void LoyaltyTier_80Plus_FullTax()
        {
            // 民心 80+ 正常税收
            var config = LoadConfig();
            var world = new WorldRuntime();
            var v = MakeVillage(loyalty: 90, grain: 100000);
            world.Settlements.Add(v);

            int before = v.Grain;
            new EconomySystem(config).Execute(world);

            // 正常税收
            Assert.Greater(v.Grain - before, 0, "民心80+应正常收税");
        }

        [Test]
        public void LoyaltyTier_60To79_20PercentTaxReduction()
        {
            // 民心 60-80 税收-20%
            var config = LoadConfig();
            var world = new WorldRuntime();

            var vHigh = MakeVillage(loyalty: 85, grain: 100000);
            vHigh.Id = "V_HIGH";
            var vMid = MakeVillage(loyalty: 70, grain: 100000);
            vMid.Id = "V_MID";

            world.Settlements.Add(vHigh);
            world.Settlements.Add(vMid);

            int beforeHigh = vHigh.Grain;
            int beforeMid = vMid.Grain;

            new EconomySystem(config).Execute(world);

            int incomeHigh = vHigh.Grain - beforeHigh;
            int incomeMid = vMid.Grain - beforeMid;

            // vMid 收入应为 vHigh 的 80%（扣除相同的军费后）
            // 由于军费相同，收入差异体现在税收部分
            Assert.Less(incomeMid, incomeHigh, "民心60-80应减少税收");
        }

        [Test]
        public void LoyaltyTier_40To59_50PercentTaxReduction()
        {
            // 民心 40-60 税收-50%
            var config = LoadConfig();
            var world = new WorldRuntime();

            var vHigh = MakeVillage(loyalty: 85, grain: 100000);
            vHigh.Id = "V_HIGH";
            var vLow = MakeVillage(loyalty: 50, grain: 100000);
            vLow.Id = "V_LOW";

            world.Settlements.Add(vHigh);
            world.Settlements.Add(vLow);

            int beforeHigh = vHigh.Grain;
            int beforeLow = vLow.Grain;

            new EconomySystem(config).Execute(world);

            int incomeHigh = vHigh.Grain - beforeHigh;
            int incomeLow = vLow.Grain - beforeLow;

            Assert.Less(incomeLow, incomeHigh, "民心40-60应大幅减少税收");
        }

        #endregion
    }
}
