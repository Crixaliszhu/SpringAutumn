using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Camera;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    public struct MapLayerChanged : IGameEvent
    {
        public MapLayer Layer;
        public string RegionId;
    }

    public enum MapLayer
    {
        None,
        World,
        Region
    }

    public class MapLayerController : MonoBehaviour
    {
        [SerializeField] private WorldMapView worldMapView;
        [SerializeField] private RegionMapView regionMapView;
        [SerializeField] private CameraManager cameraManager;

        public MapLayer CurrentLayer { get; private set; } = MapLayer.None;
        public string CurrentRegionId { get; private set; }
        public GameApplication Application { get; private set; }

        public void Bind(GameApplication application)
        {
            Unsubscribe();
            Application = application;
            Subscribe();
            worldMapView?.Bind(application, this);
            regionMapView?.Bind(application, this);
            ShowWorldMap();
        }

        private void OnDestroy()
        {
            if (Application == null)
                return;

            Application.Events.Unsubscribe<MonthChanged>(OnWorldChanged);
            Application.Events.Unsubscribe<BattleFinished>(OnWorldChanged);
            Application.Events.Unsubscribe<RegionCaptured>(OnWorldChanged);
        }

        private void Subscribe()
        {
            Application?.Events.Subscribe<MonthChanged>(OnWorldChanged);
            Application?.Events.Subscribe<BattleFinished>(OnWorldChanged);
            Application?.Events.Subscribe<RegionCaptured>(OnWorldChanged);
        }

        private void Unsubscribe()
        {
            Application?.Events.Unsubscribe<MonthChanged>(OnWorldChanged);
            Application?.Events.Unsubscribe<BattleFinished>(OnWorldChanged);
            Application?.Events.Unsubscribe<RegionCaptured>(OnWorldChanged);
        }

        public void ShowWorldMap()
        {
            CurrentLayer = MapLayer.World;
            CurrentRegionId = null;
            SetActive(worldMapView, true);
            regionMapView?.Hide();
            SetActive(regionMapView, false);
            cameraManager?.SwitchToWorld();
            worldMapView?.Refresh();
            PublishLayerChanged();
        }

        public void EnterRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId) || Application?.World == null)
                return;
            if (!Application.World.Regions.Contains(regionId))
                return;

            CurrentLayer = MapLayer.Region;
            CurrentRegionId = regionId;
            SetActive(worldMapView, false);
            SetActive(regionMapView, true);
            cameraManager?.SwitchToRegion(regionMapView != null ? regionMapView.transform.position : Vector3.zero);
            regionMapView?.ShowRegion(regionId);
            PublishLayerChanged();
        }

        public void Refresh()
        {
            if (CurrentLayer == MapLayer.World)
                worldMapView?.Refresh();
            else if (CurrentLayer == MapLayer.Region)
                regionMapView?.Refresh();
        }

        private static void SetActive(MonoBehaviour view, bool active)
        {
            if (view != null)
                view.gameObject.SetActive(active);
        }

        private void OnWorldChanged<T>(T evt) where T : IGameEvent
        {
            Refresh();
        }

        private void PublishLayerChanged()
        {
            Application?.Events.Publish(new MapLayerChanged
            {
                Layer = CurrentLayer,
                RegionId = CurrentRegionId
            });
        }
    }
}
