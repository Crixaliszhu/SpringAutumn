using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Config;
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
        private const int DefaultAttackArmySoldiers = 10;

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private GameObject attackConfirmPanel;
        [SerializeField] private InputField attackCountInput;
        [SerializeField] private Button confirmAttackButton;
        [SerializeField] private Button cancelAttackButton;
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
            confirmAttackButton?.onClick.AddListener(ConfirmAttack);
            cancelAttackButton?.onClick.AddListener(CancelAttack);
            HideAttackConfirm();
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
                    case "ConfirmAttackButton":
                        if (confirmAttackButton == null)
                            confirmAttackButton = button;
                        break;
                    case "CancelAttackButton":
                        if (cancelAttackButton == null)
                            cancelAttackButton = button;
                        break;
                }
            }

            if (attackConfirmPanel == null)
            {
                Transform confirmTransform = transform.Find("AttackConfirmPanel");
                if (confirmTransform == null)
                {
                    Transform[] transforms = transform.root.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < transforms.Length; i++)
                    {
                        if (transforms[i] != null && transforms[i].name == "AttackConfirmPanel")
                        {
                            confirmTransform = transforms[i];
                            break;
                        }
                    }
                }

                if (confirmTransform != null)
                    attackConfirmPanel = confirmTransform.gameObject;
            }

            if (attackCountInput == null)
            {
                InputField[] inputFields = GetComponentsInChildren<InputField>(true);
                for (int i = 0; i < inputFields.Length; i++)
                {
                    if (inputFields[i] != null && inputFields[i].name == "AttackCountInput")
                    {
                        attackCountInput = inputFields[i];
                        break;
                    }
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
            confirmAttackButton?.onClick.RemoveListener(ConfirmAttack);
            cancelAttackButton?.onClick.RemoveListener(CancelAttack);
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
                Show(_settlementId, true);
        }

        private void Show(string settlementId, bool preserveAttackConfirm = false)
        {
            var world = _application?.World;
            if (world == null || !world.Settlements.TryGet(settlementId, out var settlement))
                return;

            bool keepAttackConfirm = preserveAttackConfirm && _settlementId == settlementId && IsAttackConfirmVisible();
            _settlementId = settlementId;
            if (titleText != null)
                titleText.text = settlement.Id;
            if (bodyText != null)
            {
                string ownerLine = settlement.OwnerId == PlayerNationId ? "可操作" : "仅可查看";
                bodyText.text = $"所属：{settlement.OwnerId}（{ownerLine}）\n人口：{settlement.Population}\n粮食：{settlement.Grain}\n铜钱：{settlement.Money}\n守军：{settlement.Garrison}\n建设队列：{settlement.ConstructionQueue.Count}\n征兵队列：{settlement.RecruitQueue.Count}";
            }
            RefreshActions(settlement, keepAttackConfirm);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _settlementId = null;
            HideAttackConfirm();
            gameObject.SetActive(false);
        }

        private void RefreshActions(SettlementState settlement, bool preserveAttackConfirm = false)
        {
            bool playerOwned = settlement.OwnerId == PlayerNationId;
            bool diplomacyAvailable = !playerOwned && settlement.IsCity;

            SetButtonVisible(buildButton, playerOwned);
            SetButtonVisible(recruitButton, playerOwned);
            SetButtonVisible(attackButton, !playerOwned);
            SetButtonVisible(diplomacyButton, diplomacyAvailable);
            if (!preserveAttackConfirm || playerOwned)
                HideAttackConfirm();

            if (preserveAttackConfirm && !playerOwned)
                SetStatus($"输入派兵数量并确认进攻，最多可派 {GetMaxAttackSoldiers()}");
            else if (playerOwned)
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
            if (!CanStartAttack())
                return;

            ShowAttackConfirm();
        }

        private void ConfirmAttack()
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

            int soldiers = ReadAttackSoldiers();
            if (soldiers <= 0)
            {
                SetStatus("请输入大于 0 的派兵数量");
                return;
            }

            string attackFailure = GetAttackRuleFailure(target, soldiers);
            if (!string.IsNullOrEmpty(attackFailure))
            {
                SetStatus(attackFailure);
                return;
            }

            var command = new MoveArmyCommand(PlayerNationId, PlayerSourceSettlementId, target.RegionId, target.Id, soldiers, _application.Config);
            if (commandDispatcher == null || !commandDispatcher.Enqueue(command))
            {
                SetStatus("进攻未通过：命令系统拒绝，请检查目标和出兵源");
                return;
            }

            HideAttackConfirm();
            SetStatus($"已派出 {soldiers} 人进攻 {target.Id}，下月出发");
        }

        private bool CanStartAttack()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return false;

            var world = _application.World;
            if (!world.Settlements.TryGet(_settlementId, out var target))
                return false;
            if (target.OwnerId == PlayerNationId)
            {
                SetStatus("不能攻击自己的据点");
                return false;
            }
            if (!world.Regions.TryGet(PlayerRegionId, out var playerRegion)
                || !playerRegion.NeighborRegionIds.Contains(target.RegionId))
            {
                SetStatus("只能攻击玩家区域相邻区域内的据点");
                return false;
            }

            return true;
        }

        private void ShowAttackConfirm()
        {
            int maxSoldiers = GetMaxAttackSoldiers();
            if (attackCountInput != null)
            {
                int defaultSoldiers = maxSoldiers > 0 && maxSoldiers < DefaultAttackArmySoldiers ? maxSoldiers : DefaultAttackArmySoldiers;
                attackCountInput.text = defaultSoldiers.ToString();
            }
            if (attackConfirmPanel != null)
                attackConfirmPanel.SetActive(true);
            SetStatus(maxSoldiers > 0 ? $"输入派兵数量并确认进攻，最多可派 {maxSoldiers}" : "当前没有可派兵力");
        }

        private void HideAttackConfirm()
        {
            if (attackConfirmPanel != null)
                attackConfirmPanel.SetActive(false);
        }

        private bool IsAttackConfirmVisible()
        {
            return attackConfirmPanel != null && attackConfirmPanel.activeSelf;
        }

        private void CancelAttack()
        {
            HideAttackConfirm();
            SetStatus("已取消进攻");
        }

        private int ReadAttackSoldiers()
        {
            if (attackCountInput == null || string.IsNullOrWhiteSpace(attackCountInput.text))
                return DefaultAttackArmySoldiers;

            return int.TryParse(attackCountInput.text, out int soldiers) ? soldiers : 0;
        }

        private string GetAttackRuleFailure(SettlementState target, int soldiers)
        {
            var world = _application?.World;
            var config = _application?.Config;
            if (world == null || config == null)
                return "进攻未通过：游戏配置未就绪";
            if (!world.Settlements.TryGet(PlayerSourceSettlementId, out var source))
                return $"进攻未通过：找不到出兵据点 {PlayerSourceSettlementId}";
            if (source.OwnerId != PlayerNationId)
                return $"进攻未通过：出兵据点 {source.Id} 不属于玩家";
            if (!world.Regions.TryGet(source.RegionId, out var sourceRegion))
                return $"进攻未通过：找不到出兵区域 {source.RegionId}";
            if (!sourceRegion.NeighborRegionIds.Contains(target.RegionId) && source.RegionId != target.RegionId)
                return $"进攻未通过：{source.RegionId} 与 {target.RegionId} 不相邻";

            int activeArmies = CountActivePlayerArmies(world);
            if (activeArmies >= config.AI.maxArmyCount)
                return $"进攻未通过：军队数量已达上限 {config.AI.maxArmyCount}";

            int minGarrison = GetMinGarrison(source);
            int maxDraw = (int)(source.Garrison * config.Battle.maxConscriptRate);
            int maxSoldiers = GetMaxAttackSoldiers(source);
            if (soldiers > maxSoldiers)
                return $"进攻未通过：{source.Id} 守军 {source.Garrison}，最多可派 {maxSoldiers}（需留守 {minGarrison}，抽调上限 {maxDraw}）";

            return null;
        }

        private int GetMaxAttackSoldiers()
        {
            var world = _application?.World;
            if (world == null || !world.Settlements.TryGet(PlayerSourceSettlementId, out var source))
                return 0;

            return GetMaxAttackSoldiers(source);
        }

        private int GetMaxAttackSoldiers(SettlementState source)
        {
            var config = _application?.Config;
            if (source == null || config == null)
                return 0;

            int minGarrison = GetMinGarrison(source);
            int maxDraw = (int)(source.Garrison * config.Battle.maxConscriptRate);
            int maxAfterReserve = source.Garrison - minGarrison;
            int maxSoldiers = maxDraw < maxAfterReserve ? maxDraw : maxAfterReserve;
            return maxSoldiers > 0 ? maxSoldiers : 0;
        }

        private int GetMinGarrison(SettlementState source)
        {
            var battle = _application?.Config?.Battle;
            if (battle == null || source == null)
                return 0;
            if (source.Type == SettlementType.Capital)
                return battle.minGarrisonCapital;
            if (source.IsCity)
                return battle.minGarrisonCity;
            return battle.minGarrisonVillage;
        }

        private static int CountActivePlayerArmies(WorldRuntime world)
        {
            int activeArmies = 0;
            foreach (var army in world.Armies.GetAll())
            {
                if (army.NationId == PlayerNationId && army.Status != ArmyStatus.Disbanded)
                    activeArmies++;
            }

            return activeArmies;
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
