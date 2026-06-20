using System;
using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>
    /// 经济系统：每月为每个据点计算粮税收入、钱税收入、军粮军饷消耗（需求 5.1-5.4、5.6）。
    /// 民心低于 80 时税收按档位打折。建筑加成（FARM +粮税、MARKET +钱税）。
    /// 如果结算后粮食不足，标记为粮荒状态（由 PopulationSystem 处理后果）。
    /// </summary>
    public class EconomySystem : IGameSystem
    {
        private readonly EconomyConfig _eco;
        private readonly ConfigDatabase _config;

        public EconomySystem(ConfigDatabase config)
        {
            _config = config;
            _eco = config.Economy;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                ProcessSettlement(settlement);
            }
        }

        private void ProcessSettlement(SettlementState s)
        {
            float loyaltyMult = GetLoyaltyMultiplier(s.Loyalty);

            // 粮税收入：(土地 * 亩产/年 * 税率) / 12 * 建筑加成 * 民心系数
            float baseGrainIncome = (float)s.Land * _eco.grainPerLandPerYear * _eco.grainTaxRate / 12f;
            float grainBonus = 1f + GetBuildingBonus(s, "GRAIN_TAX");
            int grainIncome = (int)(baseGrainIncome * grainBonus * loyaltyMult);

            // 钱税收入：(人口 * 每人每年钱税) / 12 * 建筑加成 * 民心系数
            float baseMoneyIncome = (float)s.Population * _eco.moneyTaxPerPersonPerYear / 12f;
            float moneyBonus = 1f + GetBuildingBonus(s, "MONEY_TAX");
            int moneyIncome = (int)(baseMoneyIncome * moneyBonus * loyaltyMult);

            // 军粮消耗
            int grainCost = s.Garrison * _eco.soldierGrainPerMonth;
            // 军饷消耗
            int moneyCost = s.Garrison * _eco.soldierWagePerMonth;

            // 结算
            s.Grain += grainIncome - grainCost;
            s.Money += moneyIncome - moneyCost;

            // 防止负数越界（粮荒状态标记留给 PopulationSystem 处理）
            if (s.Grain < 0) s.Grain = 0;
            if (s.Money < 0) s.Money = 0;
        }

        /// <summary>民心档位税收乘数（需求 5.6）。</summary>
        private static float GetLoyaltyMultiplier(int loyalty)
        {
            if (loyalty >= 80) return 1.0f;
            if (loyalty >= 60) return 0.8f;
            if (loyalty >= 40) return 0.5f;
            return 0.5f; // <40 同 40-60 倍率，另有叛乱风险
        }

        /// <summary>累计指定效果类型的建筑加成。</summary>
        private float GetBuildingBonus(SettlementState s, string effectType)
        {
            float bonus = 0f;
            foreach (var b in s.Buildings)
            {
                if (_config.Buildings.TryGetValue(b.BuildingId, out var cfg) && cfg.effectType == effectType)
                {
                    bonus += cfg.effectValue * b.Level;
                }
            }
            return bonus;
        }
    }
}
