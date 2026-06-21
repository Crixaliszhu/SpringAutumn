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
        [SerializeField] private int layoutColumns = 6;
        [SerializeField] private Vector2 regionSpacing = new Vector2(1.15f, 0.82f);

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

            int index = 0;
            foreach (var region in world.Regions.GetAll())
            {
                RegionView view = GetOrCreate(region.Id);
                view.Bind(region.Id, _controller);
                view.transform.localPosition = CalculateRegionPosition(index);
                view.Refresh(region);
                index++;
            }

            nationBorderView?.Refresh(world);
        }

        private Vector3 CalculateRegionPosition(int index)
        {
            int columns = Mathf.Max(1, layoutColumns);
            int row = index / columns;
            int col = index % columns;
            float x = (col - (columns - 1) * 0.5f) * regionSpacing.x;
            float y = (1.5f - row) * regionSpacing.y;
            return new Vector3(x, y, 0f);
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
