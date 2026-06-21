using System.Collections.Generic;
using UnityEngine;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    /// <summary>天下地图势力边界占位 View。V1 可由场景中的 LineRenderer/网格对象实现具体边界。</summary>
    public class NationBorderView : MonoBehaviour
    {
        [SerializeField] private Color borderColor = Color.white;

        private readonly Dictionary<string, string> _regionOwners = new Dictionary<string, string>();

        public void Refresh(WorldRuntime world)
        {
            _regionOwners.Clear();
            if (world == null)
                return;

            foreach (var region in world.Regions.GetAll())
                _regionOwners[region.Id] = region.OwnerId;

            foreach (var renderer in GetComponentsInChildren<LineRenderer>())
                renderer.startColor = renderer.endColor = borderColor;
        }

        public string GetOwner(string regionId)
        {
            return _regionOwners.TryGetValue(regionId, out var owner) ? owner : null;
        }
    }
}
