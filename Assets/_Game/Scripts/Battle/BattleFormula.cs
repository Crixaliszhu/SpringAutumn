using SpringAutumn.Config;
using SpringAutumn.Core.Utils;
using SpringAutumn.Runtime;

namespace SpringAutumn.Battle
{
    public static class BattleFormula
    {
        public static float AttackPower(int soldiers, BattleConfig config)
        {
            return soldiers * config.attackCoefficient;
        }

        public static float DefensePower(int soldiers, float defenseBonus, BattleConfig config)
        {
            return soldiers * config.defenseCoefficient * (1f + defenseBonus);
        }

        public static BattleResult Resolve(int attackers, int defenders, float defenseBonus,
            BattleConfig config, DeterministicRandom rng)
        {
            float attackPower = AttackPower(attackers, config);
            float defensePower = DefensePower(defenders, defenseBonus, config);
            float ratio = defensePower <= 0f ? 999f : attackPower / defensePower;

            bool attackerWon;
            float minLoss;
            float maxLoss;

            if (ratio > 2f)
            {
                attackerWon = true;
                minLoss = 0.10f;
                maxLoss = 0.20f;
            }
            else if (ratio >= 1.2f)
            {
                attackerWon = rng.Chance(0.80);
                minLoss = 0.20f;
                maxLoss = 0.40f;
            }
            else if (ratio >= 0.8f)
            {
                attackerWon = rng.Chance(0.50);
                minLoss = 0.30f;
                maxLoss = 0.50f;
            }
            else
            {
                attackerWon = rng.Chance(0.20);
                minLoss = 0.40f;
                maxLoss = 0.80f;
            }

            float lossRate = minLoss + (maxLoss - minLoss) * rng.NextFloat();
            int attackerLosses = ClampLoss((int)(attackers * lossRate), attackers);
            int defenderLosses = attackerWon ? defenders : ClampLoss((int)(defenders * 0.25f), defenders);

            return new BattleResult
            {
                Outcome = attackerWon ? BattleOutcome.AttackerWon : BattleOutcome.DefenderWon,
                AttackerLosses = attackerLosses,
                DefenderLosses = defenderLosses,
                PowerRatio = ratio
            };
        }

        private static int ClampLoss(int losses, int soldiers)
        {
            if (losses < 0) return 0;
            if (losses > soldiers) return soldiers;
            return losses;
        }
    }
}
