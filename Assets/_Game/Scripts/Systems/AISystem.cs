using SpringAutumn.AI;
using SpringAutumn.Commands;
using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>国家 AI：评估国力、经济、外交和战争倾向，并只生成 Command。</summary>
    public class AISystem : IGameSystem
    {
        private readonly ConfigDatabase _config;

        public AISystem(ConfigDatabase config)
        {
            _config = config;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var nation in world.Nations.GetAll())
            {
                if (nation.Id == "PLAYER" || nation.Id == "NEUTRAL")
                    continue;

                UpdateState(world, nation);

                if (HasFoodCrisis(world, nation.Id))
                    continue;

                EnqueueGuardRecruitment(world, nation.Id);
                EnqueueDevelopment(world, nation.Id);
                EnqueueWarDecision(world, nation.Id);
            }
        }

        private void UpdateState(WorldRuntime world, NationState nation)
        {
            if (HasFoodCrisis(world, nation.Id))
            {
                nation.AIState = NationAIState.Weak;
            }
            else if (nation.WarStatus == WarStatus.War)
            {
                nation.AIState = NationAIState.War;
            }
            else
            {
                nation.AIState = NationAIState.Developing;
            }
        }

        private bool HasFoodCrisis(WorldRuntime world, string nationId)
        {
            int grain = 0;
            int soldiers = 0;
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != nationId)
                    continue;
                grain += settlement.Grain;
                soldiers += settlement.Garrison;
            }

            int monthlyNeed = soldiers * _config.Economy.soldierGrainPerMonth;
            return monthlyNeed > 0 && grain < monthlyNeed * 6;
        }

        private void EnqueueGuardRecruitment(WorldRuntime world, string nationId)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != nationId)
                    continue;

                int target = GetGuardTarget(settlement);
                if (settlement.Garrison >= target)
                    continue;

                int count = target - settlement.Garrison;
                var cmd = new RecruitCommand(nationId, settlement.Id, count, _config);
                if (cmd.Validate(world))
                    world.Commands.Enqueue(cmd);
                return;
            }
        }

        private void EnqueueDevelopment(WorldRuntime world, string nationId)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != nationId)
                    continue;
                if (settlement.Money <= _config.AI.buildMoneyThreshold)
                    continue;

                string buildingId = ChooseBuilding(settlement);
                if (buildingId == null)
                    continue;

                var cmd = new BuildCommand(nationId, settlement.Id, buildingId, _config);
                if (cmd.Validate(world))
                    world.Commands.Enqueue(cmd);
                return;
            }
        }

        private void EnqueueWarDecision(WorldRuntime world, string nationId)
        {
            if (nationId == "PLAYER" && IsPlayerInRefugeeStage(world))
                return;

            int myPower = AIEvaluator.CalculatePower(world, nationId);
            foreach (var myRegion in world.Regions.GetAll())
            {
                if (myRegion.OwnerId != nationId)
                    continue;

                foreach (var neighborId in myRegion.NeighborRegionIds)
                {
                    if (!world.Regions.TryGet(neighborId, out var targetRegion))
                        continue;
                    string targetNationId = targetRegion.OwnerId;
                    if (targetNationId == nationId || targetNationId == "NEUTRAL")
                        continue;
                    if (targetNationId == "PLAYER" && IsPlayerInRefugeeStage(world))
                        continue;

                    int relation = world.Diplomacy.GetRelation(nationId, targetNationId);
                    int enemyPower = AIEvaluator.CalculatePower(world, targetNationId);
                    bool hasAdvantage = enemyPower <= 0 || myPower >= enemyPower * _config.AI.warPowerThreshold;
                    if (hasAdvantage && relation <= -70 && world.Random.Chance(0.5))
                    {
                        world.Commands.Enqueue(new DiplomacyCommand(nationId, targetNationId, -100, true));
                        return;
                    }
                }
            }
        }

        private int GetGuardTarget(SettlementState settlement)
        {
            if (settlement.Type == SettlementType.Capital) return 100;
            if (settlement.IsCity) return 50;
            return 20;
        }

        private static string ChooseBuilding(SettlementState settlement)
        {
            if (settlement.IsVillage)
                return settlement.Population >= settlement.PopulationCap * 9 / 10 ? "HOUSE" : "FARM";
            if (settlement.IsCity)
                return "MARKET";
            return null;
        }

        private static bool IsPlayerInRefugeeStage(WorldRuntime world)
        {
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId == "PLAYER" && settlement.IsCity)
                    return false;
            }
            return true;
        }
    }
}
