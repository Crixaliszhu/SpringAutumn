using SpringAutumn.Battle;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>自动结算野战军对据点的攻击，并在核心城失守时同步 Region 归属。</summary>
    public class BattleSystem : IGameSystem
    {
        private readonly ConfigDatabase _config;
        private readonly EventBus _eventBus;

        public BattleSystem(ConfigDatabase config, EventBus eventBus = null)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var army in world.Armies.GetAll())
            {
                if (army.Status != ArmyStatus.Sieging || army.Soldiers <= 0)
                    continue;
                if (string.IsNullOrEmpty(army.TargetSettlementId))
                    continue;
                if (!world.Settlements.TryGet(army.TargetSettlementId, out var target))
                    continue;
                if (target.OwnerId == army.NationId)
                    continue;
                if (target.RegionId != army.CurrentRegionId)
                    continue;

                ResolveBattle(world, army, target);
            }
        }

        private void ResolveBattle(WorldRuntime world, ArmyState army, SettlementState target)
        {
            string oldOwner = target.OwnerId;
            float defenseBonus = GetDefenseBonus(target);
            BattleResult result = BattleFormula.Resolve(
                army.Soldiers, target.Garrison, defenseBonus, _config.Battle, world.Random);

            army.Soldiers -= result.AttackerLosses;
            if (army.Soldiers < 0) army.Soldiers = 0;

            target.Garrison -= result.DefenderLosses;
            if (target.Garrison < 0) target.Garrison = 0;

            _eventBus?.Publish(new BattleFinished
            {
                AttackerNationId = army.NationId,
                DefenderNationId = oldOwner,
                SettlementId = target.Id,
                AttackerWon = result.AttackerWon
            });

            if (result.AttackerWon)
            {
                CaptureSettlement(world, target, army.NationId, oldOwner);
                army.Status = ArmyStatus.Disbanded;
            }
            else
            {
                army.Status = army.Soldiers <= 0 ? ArmyStatus.Disbanded : ArmyStatus.Retreating;
            }
        }

        private void CaptureSettlement(WorldRuntime world, SettlementState target,
            string newOwnerId, string oldOwnerId)
        {
            target.OwnerId = newOwnerId;
            target.Garrison = 0;
            target.Loyalty = _config.Battle.captureLoyalty;

            if (!world.Regions.TryGet(target.RegionId, out var region))
                return;

            if (region.CityId == target.Id || !region.HasCity)
            {
                region.ChangeOwner(world, newOwnerId, _config.Battle.captureLoyalty);

                _eventBus?.Publish(new RegionCaptured
                {
                    RegionId = region.Id,
                    OldOwnerId = oldOwnerId,
                    NewOwnerId = newOwnerId
                });
            }
        }

        private float GetDefenseBonus(SettlementState target)
        {
            float bonus = 0f;
            foreach (var building in target.Buildings)
            {
                if (_config.Buildings.TryGetValue(building.BuildingId, out var cfg)
                    && (cfg.effectType == "DEFENSE" || cfg.effectType == "CITY_DEFENSE"))
                {
                    bonus += cfg.effectValue * building.Level;
                }
            }
            return bonus;
        }
    }
}
