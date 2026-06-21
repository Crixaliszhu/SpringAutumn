using System.Collections.Generic;

namespace SpringAutumn.Runtime
{
    /// <summary>战略区域运行时状态。AI 以 Region 为战争目标单位，"城决定区域归属"。</summary>
    public class RegionState : Entity
    {
        public string OwnerId;
        public bool IsFrontier;

        /// <summary>核心城据点 Id；玩家/中立无城区域为空。</summary>
        public string CityId;

        public List<string> VillageIds = new List<string>();
        public List<string> NeighborRegionIds = new List<string>();

        public RegionState() { }

        public RegionState(string id) : base(id) { }

        /// <summary>是否拥有核心城。</summary>
        public bool HasCity => !string.IsNullOrEmpty(CityId);

        public void ChangeOwner(WorldRuntime world, string newOwnerId, int captureLoyalty)
        {
            OwnerId = newOwnerId;

            if (HasCity && world.Settlements.TryGet(CityId, out var city))
            {
                city.OwnerId = newOwnerId;
                city.Loyalty = captureLoyalty;
            }

            foreach (var villageId in VillageIds)
            {
                if (world.Settlements.TryGet(villageId, out var village))
                {
                    village.OwnerId = newOwnerId;
                    village.Loyalty = captureLoyalty;
                }
            }
        }
    }
}
