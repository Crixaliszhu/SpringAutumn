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
        private readonly HashSet<string> _visibleSettlements = new HashSet<string>();
        private readonly HashSet<string> _visibleArmies = new HashSet<string>();
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
            _visibleSettlements.Clear();
            _visibleArmies.Clear();

            if (region.HasCity)
                RefreshSettlement(world.Settlements.Get(region.CityId), Vector3.zero);

            for (int i = 0; i < region.VillageIds.Count; i++)
            {
                string villageId = region.VillageIds[i];
                if (world.Settlements.TryGet(villageId, out var village))
                    RefreshSettlement(village, CalculateVillagePosition(i, region.VillageIds.Count, region.HasCity));
            }

            foreach (var army in world.Armies.GetAll())
            {
                if (army.CurrentRegionId == _regionId && army.Status != ArmyStatus.Disbanded)
                    RefreshArmy(army);
            }

            HideInactive(_settlementViews, _visibleSettlements);
            HideInactive(_armyViews, _visibleArmies);
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

        private void RefreshSettlement(SettlementState settlement, Vector3 localPosition)
        {
            SettlementView view = GetOrCreateSettlement(settlement.Id);
            view.gameObject.SetActive(true);
            view.transform.localPosition = localPosition;
            view.Refresh(settlement);
            _visibleSettlements.Add(settlement.Id);
        }

        private void RefreshArmy(ArmyState army)
        {
            ArmyView view = GetOrCreateArmy(army.Id);
            view.gameObject.SetActive(true);
            view.Refresh(army);
            _visibleArmies.Add(army.Id);
        }

        private static Vector3 CalculateVillagePosition(int index, int count, bool hasCity)
        {
            if (!hasCity && count == 1)
                return Vector3.zero;

            float radius = hasCity ? 1.55f : 1.05f;
            float startAngle = hasCity ? 210f : 90f;
            float span = hasCity ? 120f : 240f;
            float angle = count <= 1 ? 270f : startAngle + span * index / (count - 1);
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, -0.05f);
        }

        private static void HideInactive<T>(Dictionary<string, T> views, HashSet<string> visibleIds) where T : MonoBehaviour
        {
            foreach (var pair in views)
            {
                if (!visibleIds.Contains(pair.Key) && pair.Value != null)
                    pair.Value.gameObject.SetActive(false);
            }
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
