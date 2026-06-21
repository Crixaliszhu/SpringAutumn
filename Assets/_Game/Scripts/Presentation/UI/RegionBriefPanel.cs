using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;

namespace SpringAutumn.Presentation.UI
{
    public class RegionBriefPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button enterRegionButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private MapLayerController mapLayerController;

        private GameApplication _application;
        private string _regionId;

        public void Bind(GameApplication application)
        {
            _application = application;
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            enterRegionButton?.onClick.AddListener(() => mapLayerController?.EnterRegion(_regionId));
            diplomacyButton?.onClick.AddListener(() => gameObject.SetActive(false));
            attackButton?.onClick.AddListener(() => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.Type != SelectionType.Region)
                return;
            Show(evt.Id);
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
                bodyText.text = $"所属：{region.OwnerId}\n城邑：{(region.HasCity ? 1 : 0)}\n村庄：{villages}\n人口：{population}\n守军：{garrison}";

            gameObject.SetActive(true);
        }
    }
}
