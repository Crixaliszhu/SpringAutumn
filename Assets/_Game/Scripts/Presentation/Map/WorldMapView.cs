using System.Collections.Generic;
using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Core.Events;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    public class WorldMapView : MonoBehaviour
    {
        [SerializeField] private Transform regionRoot;
        [SerializeField] private RegionView regionViewPrefab;
        [SerializeField] private NationBorderView nationBorderView;

        private readonly Dictionary<string, RegionView> _regionViews = new Dictionary<string, RegionView>();
        private GameApplication _application;
        private MapLayerController _controller;

        public void Bind(GameApplication application, MapLayerController controller)
        {
            _application = application;
            _controller = controller;
            _application.Events.Subscribe<RegionCaptured>(OnRegionCaptured);
            Refresh();
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<RegionCaptured>(OnRegionCaptured);
        }

        public void Refresh()
        {
            WorldRuntime world = _application?.World;
            if (world == null)
                return;

            foreach (var region in world.Regions.GetAll())
            {
                RegionView view = GetOrCreate(region.Id);
                view.Bind(region.Id, _controller);
                view.Refresh(region);
            }

            nationBorderView?.Refresh(world);
        }

        private RegionView GetOrCreate(string regionId)
        {
            if (_regionViews.TryGetValue(regionId, out var existing))
                return existing;

            RegionView view = regionViewPrefab != null
                ? Instantiate(regionViewPrefab, regionRoot != null ? regionRoot : transform)
                : new GameObject(regionId).AddComponent<RegionView>();
            view.name = "Region_" + regionId;
            _regionViews.Add(regionId, view);
            return view;
        }

        private void OnRegionCaptured(RegionCaptured evt)
        {
            if (_application?.World == null)
                return;
            if (_regionViews.TryGetValue(evt.RegionId, out var view))
                view.Refresh(_application.World.Regions.Get(evt.RegionId));
        }
    }
}
