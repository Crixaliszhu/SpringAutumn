using SpringAutumn.Runtime;

namespace SpringAutumn.AI
{
    public static class AIEvaluator
    {
        public static int CalculatePower(WorldRuntime world, string nationId)
        {
            int population = 0;
            int soldiers = 0;
            int regions = 0;
            int grain = 0;
            int money = 0;

            foreach (var region in world.Regions.GetAll())
            {
                if (region.OwnerId == nationId)
                    regions++;
            }

            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != nationId)
                    continue;
                population += settlement.Population;
                soldiers += settlement.Garrison;
                grain += settlement.Grain;
                money += settlement.Money;
            }

            foreach (var army in world.Armies.GetAll())
            {
                if (army.NationId == nationId && army.Status != ArmyStatus.Disbanded)
                    soldiers += army.Soldiers;
            }

            return population + soldiers * 5 + regions * 500 + grain / 1000 + money / 10;
        }

        public static int CalculatePlayerThreat(WorldRuntime world)
        {
            int cityCount = 0;
            int soldiers = 0;
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != "PLAYER")
                    continue;
                if (settlement.IsCity)
                    cityCount++;
                soldiers += settlement.Garrison;
            }
            return cityCount * 30 + soldiers / 10;
        }

        public static int CalculateRegionValue(WorldRuntime world, RegionState region)
        {
            int value = region.HasCity ? 50 : 20;
            if (region.IsFrontier)
                value += 20;

            if (region.HasCity && world.Settlements.TryGet(region.CityId, out var city))
            {
                value += city.Population / 100 + city.Garrison / 10;
            }

            foreach (var villageId in region.VillageIds)
            {
                if (world.Settlements.TryGet(villageId, out var village))
                    value += village.Population / 100 + village.Land / 1000;
            }
            return value;
        }
    }
}
