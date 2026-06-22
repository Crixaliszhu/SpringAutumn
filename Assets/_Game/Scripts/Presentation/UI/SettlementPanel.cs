using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.UI
{
    public class SettlementPanel : MonoBehaviour
    {
        private const string PlayerNationId = "PLAYER";
        private const string PlayerRegionId = "PLAYER_R01";
        private const string PlayerSourceSettlementId = "V_PLAYER_001";
        private const int AttackArmySoldiers = 10;

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private UICommandDispatcher commandDispatcher;

        private GameApplication _application;
        private string _settlementId;

        public void Bind(GameApplication application)
        {
            ResolveReferences();
            _application = application;
            commandDispatcher?.Bind(application);
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            _application.Events.Subscribe<MonthChanged>(OnMonthChanged);
            _application.Events.Subscribe<MapLayerChanged>(OnMapLayerChanged);
            buildButton?.onClick.AddListener(BuildDefault);
            recruitButton?.onClick.AddListener(RecruitDefault);
            attackButton?.onClick.AddListener(AttackSelected);
            diplomacyButton?.onClick.AddListener(OpenDiplomacy);
            gameObject.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (commandDispatcher == null)
                commandDispatcher = GetComponent<UICommandDispatcher>();

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text == null)
                    continue;

                switch (text.name)
                {
                    case "TitleText":
                        if (titleText == null)
                            titleText = text;
                        break;
                    case "BodyText":
                        if (bodyText == null)
                            bodyText = text;
                        break;
                    case "StatusText":
                        if (statusText == null)
                            statusText = text;
                        break;
                }
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                switch (button.name)
                {
                    case "BuildButton":
                        if (buildButton == null)
                            buildButton = button;
                        break;
                    case "RecruitButton":
                        if (recruitButton == null)
                            recruitButton = button;
                        break;
                    case "AttackButton":
                        if (attackButton == null)
                            attackButton = button;
                        break;
                    case "DiplomacyButton":
                        if (diplomacyButton == null)
                            diplomacyButton = button;
                        break;
                }
            }
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
            _application?.Events.Unsubscribe<MonthChanged>(OnMonthChanged);
            _application?.Events.Unsubscribe<MapLayerChanged>(OnMapLayerChanged);
            buildButton?.onClick.RemoveListener(BuildDefault);
            recruitButton?.onClick.RemoveListener(RecruitDefault);
            attackButton?.onClick.RemoveListener(AttackSelected);
            diplomacyButton?.onClick.RemoveListener(OpenDiplomacy);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.Type != SelectionType.City && evt.Type != SelectionType.Village)
            {
                Hide();
                return;
            }
            Show(evt.Id);
        }

        private void OnMapLayerChanged(MapLayerChanged evt)
        {
            if (evt.Layer != MapLayer.Region)
                Hide();
        }

        private void OnMonthChanged(MonthChanged evt)
        {
            if (!string.IsNullOrEmpty(_settlementId) && gameObject.activeSelf)
                Show(_settlementId);
        }

        private void Show(string settlementId)
        {
            var world = _application?.World;
            if (world == null || !world.Settlements.TryGet(settlementId, out var settlement))
                return;

            _settlementId = settlementId;
            if (titleText != null)
                titleText.text = settlement.Id;
            if (bodyText != null)
            {
                string ownerLine = settlement.OwnerId == PlayerNationId ? "可操作" : "仅可查看";
                bodyText.text = $"所属：{settlement.OwnerId}（{ownerLine}）\n人口：{settlement.Population}\n粮食：{settlement.Grain}\n铜钱：{settlement.Money}\n守军：{settlement.Garrison}\n建设队列：{settlement.ConstructionQueue.Count}\n征兵队列：{settlement.RecruitQueue.Count}";
            }
            RefreshActions(settlement);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _settlementId = null;
            gameObject.SetActive(false);
        }

        private void RefreshActions(SettlementState settlement)
        {
            bool playerOwned = settlement.OwnerId == PlayerNationId;
            bool diplomacyAvailable = !playerOwned && settlement.IsCity;

            SetButtonVisible(buildButton, playerOwned);
            SetButtonVisible(recruitButton, playerOwned);
            SetButtonVisible(attackButton, !playerOwned);
            SetButtonVisible(diplomacyButton, diplomacyAvailable);

            if (playerOwned)
                SetStatus("选择建设或征兵");
            else if (diplomacyAvailable)
                SetStatus("可进攻；郡城/国都可外交");
            else
                SetStatus("可进攻");
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        private void BuildDefault()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;
            var settlement = _application.World.Settlements.Get(_settlementId);
            if (!CanOperate(settlement.OwnerId))
                return;

            string building = settlement.IsCity ? "MARKET" : "FARM";
            bool accepted = commandDispatcher != null && commandDispatcher.Enqueue(new BuildCommand(settlement.OwnerId, settlement.Id, building, _application.Config));
            SetStatus(accepted ? $"已提交建设：{building}，下月执行" : "建设未通过：资源或规则不足");
        }

        private void RecruitDefault()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;
            var settlement = _application.World.Settlements.Get(_settlementId);
            if (!CanOperate(settlement.OwnerId))
                return;

            bool accepted = commandDispatcher != null && commandDispatcher.Enqueue(new RecruitCommand(settlement.OwnerId, settlement.Id, 10, _application.Config));
            SetStatus(accepted ? "已提交征兵：10，下月执行" : "征兵未通过：资源或规则不足");
        }

        private void AttackSelected()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;

            var world = _application.World;
            if (!world.Settlements.TryGet(_settlementId, out var target))
                return;
            if (target.OwnerId == PlayerNationId)
            {
                SetStatus("不能攻击自己的据点");
                return;
            }
            if (!world.Regions.TryGet(PlayerRegionId, out var playerRegion)
                || !playerRegion.NeighborRegionIds.Contains(target.RegionId))
            {
                SetStatus("只能攻击玩家区域相邻区域内的据点");
                return;
            }

            var command = new MoveArmyCommand(PlayerNationId, PlayerSourceSettlementId, target.RegionId, target.Id, AttackArmySoldiers, _application.Config);
            if (commandDispatcher == null || !commandDispatcher.Enqueue(command))
            {
                SetStatus("进攻未通过：请先在流民村征兵并保留守军");
                return;
            }

            SetStatus($"已派出 {AttackArmySoldiers} 人进攻 {target.Id}，下月出发");
        }

        private void OpenDiplomacy()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;

            var settlement = _application.World.Settlements.Get(_settlementId);
            if (!settlement.IsCity)
            {
                SetStatus("外交仅对郡城/国都开放");
                return;
            }

            SetStatus($"外交功能后续接入：{settlement.OwnerId}");
        }

        private bool CanOperate(string ownerId)
        {
            if (ownerId == PlayerNationId)
                return true;

            SetStatus("只能操作玩家据点");
            return false;
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }
    }
}
