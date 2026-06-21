using UnityEngine;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    /// <summary>区域地图地形 View，占位承载当前 Region 的地形底板/装饰对象。</summary>
    public class TerrainView : MonoBehaviour
    {
        [SerializeField] private TextMesh label;

        public string RegionId { get; private set; }

        public void Refresh(RegionState region)
        {
            RegionId = region.Id;
            if (label != null)
                label.text = region.Id;
        }
    }
}
