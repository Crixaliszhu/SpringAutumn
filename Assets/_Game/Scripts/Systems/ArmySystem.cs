using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Systems
{
    /// <summary>处理野战军行军与战后解散。</summary>
    public class ArmySystem : IGameSystem
    {
        private readonly EventBus _eventBus;

        public ArmySystem(EventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void Execute(WorldRuntime world)
        {
            foreach (var army in world.Armies.GetAll())
            {
                if (army.Status == ArmyStatus.Marching)
                {
                    MoveOneRegion(world, army);
                }
                else if (army.Status == ArmyStatus.Disbanded)
                {
                    ReturnSurvivors(world, army);
                }
            }
        }

        private static void MoveOneRegion(WorldRuntime world, ArmyState army)
        {
            if (string.IsNullOrEmpty(army.TargetRegionId) || army.CurrentRegionId == army.TargetRegionId)
            {
                army.Status = ArmyStatus.Idle;
                return;
            }

            if (!world.Regions.TryGet(army.CurrentRegionId, out var current))
                return;

            if (current.NeighborRegionIds.Contains(army.TargetRegionId))
            {
                army.CurrentRegionId = army.TargetRegionId;
                army.MoveProgress++;
                army.Status = ArmyStatus.Idle;
            }
        }

        private static void ReturnSurvivors(WorldRuntime world, ArmyState army)
        {
            if (army.Soldiers <= 0)
                return;
            if (string.IsNullOrEmpty(army.SourceSettlementId))
                return;
            if (world.Settlements.TryGet(army.SourceSettlementId, out var source)
                && source.OwnerId == army.NationId)
            {
                source.Garrison += army.Soldiers;
                army.Soldiers = 0;
            }
        }
    }
}
