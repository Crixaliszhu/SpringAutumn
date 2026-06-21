using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.Map
{
    public class RegionMapView : MonoBehaviour
    {
        [SerializeField] private Transform settlementRoot;
        [SerializeField] private Transform armyRoot;
        [SerializeField] private TerrainView terrainView;
        [SerializeField] private CityView cityViewPrefab;
        [SerializeField] private VillageView villageViewPrefab;
        [SerializeField] private ArmyView armyViewPrefab;
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text placeholderText;
        [SerializeField] private Button returnButton;

        private readonly Dictionary<string, SettlementView> _settlementViews = new Dictionary<string, SettlementView>();
        private readonly Dictionary<string, ArmyView> _armyViews = new Dictionary<string, ArmyView>();
        private GameApplication _application;
        private MapLayerController _controller;
        private string _regionId;

        public void Bind(GameApplication application, MapLayerController controller)
        {
            _application = application;
            _controller = controller;
            returnButton?.onClick.AddListener(ReturnToWorldMap);
            Hide();
        }

        private void OnDestroy()
        {
            returnButton?.onClick.RemoveListener(ReturnToWorldMap);
        }

        public void ShowRegion(string regionId)
        {
            _regionId = regionId;
            if (uiRoot != null)
                uiRoot.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            WorldRuntime world = _application?.World;
            if (world == null || string.IsNullOrEmpty(_regionId) || !world.Regions.TryGet(_regionId, out var region))
                return;

            if (titleText != null)
                titleText.text = "区域地图：" + region.Id;

            if (placeholderText != null)
                placeholderText.text = $"核心城：{(region.HasCity ? region.CityId : "无")}\n村庄：{region.VillageIds.Count}\n区域地图内容将在阶段 5 接入";

            terrainView?.Refresh(region);

            if (region.HasCity)
                RefreshSettlement(world.Settlements.Get(region.CityId));

            foreach (var villageId in region.VillageIds)
            {
                if (world.Settlements.TryGet(villageId, out var village))
                    RefreshSettlement(village);
            }

            foreach (var army in world.Armies.GetAll())
            {
                if (army.CurrentRegionId == _regionId && army.Status != ArmyStatus.Disbanded)
                    RefreshArmy(army);
            }
        }

        public void ReturnToWorldMap()
        {
            _controller?.ShowWorldMap();
        }

        public void Hide()
        {
            if (uiRoot != null)
                uiRoot.SetActive(false);
        }

        private void RefreshSettlement(SettlementState settlement)
        {
            SettlementView view = GetOrCreateSettlement(settlement.Id);
            view.Refresh(settlement);
        }

        private void RefreshArmy(ArmyState army)
        {
            ArmyView view = GetOrCreateArmy(army.Id);
            view.Refresh(army);
        }

        private SettlementView GetOrCreateSettlement(string id)
        {
            if (_settlementViews.TryGetValue(id, out var existing))
                return existing;

            SettlementView prefab = null;
            if (_application?.World != null && _application.World.Settlements.TryGet(id, out var settlement))
                prefab = settlement.IsCity ? cityViewPrefab : villageViewPrefab;

            SettlementView view = prefab != null
                ? Instantiate(prefab, settlementRoot != null ? settlementRoot : transform)
                : new GameObject(id).AddComponent<SettlementView>();
            view.name = "Settlement_" + id;
            _settlementViews.Add(id, view);
            return view;
        }

        private ArmyView GetOrCreateArmy(string id)
        {
            if (_armyViews.TryGetValue(id, out var existing))
                return existing;

            ArmyView view = armyViewPrefab != null
                ? Instantiate(armyViewPrefab, armyRoot != null ? armyRoot : transform)
                : new GameObject(id).AddComponent<ArmyView>();
            view.name = "Army_" + id;
            _armyViews.Add(id, view);
            return view;
        }
    }
}
