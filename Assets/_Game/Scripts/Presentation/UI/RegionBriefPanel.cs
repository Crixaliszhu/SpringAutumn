using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;

namespace SpringAutumn.Presentation.UI
{
    public class RegionBriefPanel : MonoBehaviour
    {
        private const string PlayerNationId = "PLAYER";
        private const string PlayerRegionId = "PLAYER_R01";
        private const string PlayerSourceSettlementId = "V_PLAYER_001";
        private const int ScoutArmySoldiers = 10;

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button enterRegionButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private Button attackButton;
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
            diplomacyButton?.onClick.AddListener(() => ShowPlaceholder("外交功能后续接入"));
            attackButton?.onClick.AddListener(DispatchScoutArmy);
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

        private void ShowPlaceholder(string message)
        {
            if (bodyText != null)
                bodyText.text = string.IsNullOrEmpty(_briefText) ? message : _briefText + "\n\n" + message;
        }

        private void DispatchScoutArmy()
        {
            var world = _application?.World;
            if (world == null || string.IsNullOrEmpty(_regionId))
                return;

            if (_regionId == PlayerRegionId)
            {
                ShowPlaceholder("请选择相邻目标区域");
                return;
            }

            if (!world.Regions.TryGet(PlayerRegionId, out var playerRegion)
                || !playerRegion.NeighborRegionIds.Contains(_regionId))
            {
                ShowPlaceholder("只能向玩家区域相邻的目标出兵");
                return;
            }

            string targetSettlementId = ResolveAttackTarget(_regionId);
            if (string.IsNullOrEmpty(targetSettlementId))
            {
                ShowPlaceholder("目标区域暂无可攻击据点");
                return;
            }

            var command = new MoveArmyCommand(PlayerNationId, PlayerSourceSettlementId, _regionId, targetSettlementId, ScoutArmySoldiers, _application.Config);
            if (!command.Validate(world))
            {
                ShowPlaceholder("出兵未通过：请先在流民村征兵并保留守军");
                return;
            }

            _application.Engine.EnqueueCommand(command);
            ShowPlaceholder($"已派出 {ScoutArmySoldiers} 人进攻 {targetSettlementId}，下一月开始行军");
        }

        private string ResolveAttackTarget(string regionId)
        {
            var world = _application?.World;
            if (world == null || !world.Regions.TryGet(regionId, out var region))
                return null;

            if (region.HasCity && world.Settlements.TryGet(region.CityId, out var city) && city.OwnerId != PlayerNationId)
                return city.Id;

            foreach (var villageId in region.VillageIds)
            {
                if (world.Settlements.TryGet(villageId, out var village) && village.OwnerId != PlayerNationId)
                    return village.Id;
            }

            return null;
        }
    }
}
