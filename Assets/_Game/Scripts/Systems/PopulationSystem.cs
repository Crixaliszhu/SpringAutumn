using System;
using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>
    /// 人口与民心系统（需求 5.5、5.6、5.7）。
    /// 每月：检测粮荒并执行后果链（民心↓ → 逃兵 → 人口↓），正常状态下人口增长与民心恢复。
    /// 在 Tick 顺序中位于 EconomySystem 之后执行。
    /// </summary>
    public class PopulationSystem : IGameSystem
    {
        private readonly EconomyConfig _eco;

        // 基础参数
        private const float BaseMonthlyGrowthRate = 0.002f;   // 基础月人口增长率 0.2%
        private const int FamineLoyaltyDrop = 5;              // 粮荒每月民心下降
        private const float FamineDeserterRate = 0.05f;       // 粮荒每月逃兵率 5%
        private const float FaminePopulationLoss = 0.01f;     // 粮荒每月人口减少率 1%
        private const int PeaceLoyaltyRecover = 1;            // 和平时期每月民心恢复

        public PopulationSystem(ConfigDatabase config)
        {
            _eco = config.Economy;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var s in world.Settlements.GetAll())
            {
                ProcessSettlement(s);
            }
        }

        private void ProcessSettlement(SettlementState s)
        {
            bool isFamine = IsFamine(s);

            if (isFamine)
            {
                // 缺粮后果链（需求 5.5）：民心↓ → 逃兵 → 人口↓
                s.Loyalty = Math.Max(0, s.Loyalty - FamineLoyaltyDrop);

                int deserters = (int)(s.Garrison * FamineDeserterRate);
                if (deserters > 0)
                {
                    s.Garrison = Math.Max(0, s.Garrison - deserters);
                }

                int popLoss = Math.Max(1, (int)(s.Population * FaminePopulationLoss));
                s.Population = Math.Max(0, s.Population - popLoss);
                s.Households = s.Population / _eco.householdSize;
            }
            else
            {
                // 正常状态：民心恢复（上限100）
                if (s.Loyalty < 100)
                {
                    s.Loyalty = Math.Min(100, s.Loyalty + PeaceLoyaltyRecover);
                }

                // 人口增长（不超过上限）
                if (s.Population < s.PopulationCap)
                {
                    float growthMult = s.Loyalty >= 80 ? 1.0f : (s.Loyalty >= 60 ? 0.5f : 0.2f);
                    int growth = Math.Max(1, (int)(s.Population * BaseMonthlyGrowthRate * growthMult));
                    s.Population = Math.Min(s.PopulationCap, s.Population + growth);
                    s.Households = s.Population / _eco.householdSize;
                }
            }

            // 土地随户数调整（户均50亩）
            s.Land = s.Households * _eco.landPerHousehold;
        }

        /// <summary>判断是否粮荒：粮仓为 0 且本月军费会超过收入（经济系统已将粮食 clamp 到 0）。</summary>
        private static bool IsFamine(SettlementState s)
        {
            return s.Grain <= 0;
        }
    }
}
