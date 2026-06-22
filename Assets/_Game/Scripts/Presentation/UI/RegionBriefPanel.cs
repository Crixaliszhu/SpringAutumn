using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;

namespace SpringAutumn.Presentation.UI
{
    public class RegionBriefPanel : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button enterRegionButton;
        [SerializeField] private MapLayerController mapLayerController;

        private GameApplication _application;
        private string _regionId;
        private string _briefText;

        public void Bind(GameApplication application)
        {
            _application = application;
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            _application.Events.Subscribe<MapLayerChanged>(OnMapLayerChanged);
            enterRegionButton?.onClick.AddListener(EnterRegion);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
            _application?.Events.Unsubscribe<MapLayerChanged>(OnMapLayerChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.Type != SelectionType.Region)
            {
                Hide();
                return;
            }
            Show(evt.Id);
        }

        private void OnMapLayerChanged(MapLayerChanged evt)
        {
            if (evt.Layer != MapLayer.World)
                Hide();
        }

        public void Show(string regionId)
        {
            var world = _application?.World;
            if (world == null || !world.Regions.TryGet(regionId, out var region))
                return;

            _regionId = regionId;
            int villages = region.VillageIds.Count;
            int population = 0;
            int garrison = 0;

            if (region.HasCity && world.Settlements.TryGet(region.CityId, out var city))
            {
                population += city.Population;
                garrison += city.Garrison;
            }

            foreach (var villageId in region.VillageIds)
            {
                if (world.Settlements.TryGet(villageId, out var village))
                {
                    population += village.Population;
                    garrison += village.Garrison;
                }
            }

            if (titleText != null)
                titleText.text = region.Id;
            if (bodyText != null)
            {
                string coreCityId = region.HasCity ? region.CityId : "无";
                _briefText = $"所属：{region.OwnerId}\n核心城：{coreCityId}\n村庄：{villages}\n人口：{population}\n守军：{garrison}";
                bodyText.text = _briefText;
            }

            gameObject.SetActive(true);
        }

        private void EnterRegion()
        {
            if (string.IsNullOrEmpty(_regionId))
                return;

            mapLayerController?.EnterRegion(_regionId);
            Hide();
        }

        public void Hide()
        {
            _regionId = null;
            _briefText = null;
            gameObject.SetActive(false);
        }

    }
}
