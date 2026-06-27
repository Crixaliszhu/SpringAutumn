using System.Collections.Generic;
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
        private const int DefaultAttackArmySoldiers = 10;

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button transferButton;
        [SerializeField] private Button diplomacyButton;
        [SerializeField] private GameObject attackConfirmPanel;
        [SerializeField] private InputField attackCountInput;
        [SerializeField] private Button confirmAttackButton;
        [SerializeField] private Button cancelAttackButton;
        [SerializeField] private UICommandDispatcher commandDispatcher;

        [Header("面板布局（右上角，覆盖场景值）")]
        [SerializeField] private bool overrideLayout = true;
        [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(-12f, -92f);
        [SerializeField] private float panelScale = 0.9f;

        private GameApplication _application;
        private string _settlementId;
        private PanelMode _panelMode = PanelMode.None;
        private string _selectedAttackSourceSettlementId;
        private string _transferSourceSettlementId;
        private string _transferTargetSettlementId;
        private GameObject _choicePanel;
        private bool _buttonListenersRegistered;
        private readonly List<Button> _choiceButtons = new List<Button>();

        private enum PanelMode
        {
            None,
            AttackSourceSelection,
            AttackConfirm,
            TransferTargetSelection,
            TransferConfirm
        }

        public void Bind(GameApplication application)
        {
            Unsubscribe();
            ResolveReferences();
            _application = application;
            if (overrideLayout)
                UiPanelLayout.AnchorTopRight(GetComponent<RectTransform>(), panelAnchoredPosition, panelScale);
            commandDispatcher?.Bind(application);
            EnsureDynamicControls();
            Subscribe();
            RegisterButtonListeners();
            HideOperationPanels();
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
                    case "TransferButton":
                        if (transferButton == null)
                            transferButton = button;
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

        private void EnsureDynamicControls()
        {
            if (transferButton == null && diplomacyButton != null)
            {
                transferButton = Instantiate(diplomacyButton, diplomacyButton.transform.parent);
                transferButton.name = "TransferButton";
                transferButton.onClick.RemoveAllListeners();
                SetButtonText(transferButton, "调兵");
                transferButton.gameObject.SetActive(false);

                RectTransform transferRect = transferButton.GetComponent<RectTransform>();
                RectTransform diplomacyRect = diplomacyButton.GetComponent<RectTransform>();
                if (transferRect != null && diplomacyRect != null)
                {
                    transferRect.anchorMin = diplomacyRect.anchorMin;
                    transferRect.anchorMax = diplomacyRect.anchorMax;
                    transferRect.pivot = diplomacyRect.pivot;
                    transferRect.anchoredPosition = diplomacyRect.anchoredPosition;
                    transferRect.sizeDelta = diplomacyRect.sizeDelta;
                }
            }

            if (_choicePanel == null)
            {
                _choicePanel = new GameObject("MilitaryChoicePanel");
                _choicePanel.transform.SetParent(transform, false);

                var rect = _choicePanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(16f, -118f);
                rect.sizeDelta = new Vector2(278f, 136f);

                var image = _choicePanel.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.35f);

                var layout = _choicePanel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(6, 6, 6, 6);
                layout.spacing = 6f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                _choicePanel.SetActive(false);
            }
        }

        private static void SetButtonText(Button button, string text)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label != null)
                label.text = text;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveButtonListeners();
        }

        private void Subscribe()
        {
            _application?.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            _application?.Events.Subscribe<MonthChanged>(OnMonthChanged);
            _application?.Events.Subscribe<MapLayerChanged>(OnMapLayerChanged);
        }

        private void Unsubscribe()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
            _application?.Events.Unsubscribe<MonthChanged>(OnMonthChanged);
            _application?.Events.Unsubscribe<MapLayerChanged>(OnMapLayerChanged);
        }

        private void RegisterButtonListeners()
        {
            if (_buttonListenersRegistered)
                return;

            buildButton?.onClick.AddListener(BuildDefault);
            recruitButton?.onClick.AddListener(RecruitDefault);
            attackButton?.onClick.AddListener(AttackSelected);
            transferButton?.onClick.AddListener(TransferSelected);
            diplomacyButton?.onClick.AddListener(OpenDiplomacy);
            confirmAttackButton?.onClick.AddListener(ConfirmCurrentAction);
            cancelAttackButton?.onClick.AddListener(CancelAttack);
            _buttonListenersRegistered = true;
        }

        private void RemoveButtonListeners()
        {
            if (!_buttonListenersRegistered)
                return;

            buildButton?.onClick.RemoveListener(BuildDefault);
            recruitButton?.onClick.RemoveListener(RecruitDefault);
            attackButton?.onClick.RemoveListener(AttackSelected);
            transferButton?.onClick.RemoveListener(TransferSelected);
            diplomacyButton?.onClick.RemoveListener(OpenDiplomacy);
            confirmAttackButton?.onClick.RemoveListener(ConfirmCurrentAction);
            cancelAttackButton?.onClick.RemoveListener(CancelAttack);
            _buttonListenersRegistered = false;
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
                bodyText.text = SettlementPanelTextFormatter.FormatBody(settlement, _application.Config, PlayerNationId);
            RefreshActions(settlement, keepAttackConfirm);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _settlementId = null;
            HideOperationPanels();
            gameObject.SetActive(false);
        }

        private void RefreshActions(SettlementState settlement, bool preserveAttackConfirm = false)
        {
            bool playerOwned = settlement.OwnerId == PlayerNationId;
            bool diplomacyAvailable = !playerOwned && settlement.IsCity;

            SetButtonVisible(buildButton, playerOwned);
            SetButtonVisible(recruitButton, playerOwned);
            SetButtonVisible(attackButton, !playerOwned);
            SetButtonVisible(transferButton, playerOwned);
            SetButtonVisible(diplomacyButton, diplomacyAvailable);
            LayoutActionButtons(playerOwned);

            bool canPreserve = preserveAttackConfirm
                && ((playerOwned && IsTransferMode()) || (!playerOwned && IsAttackMode()));
            if (!canPreserve)
                HideOperationPanels();

            if (canPreserve && !playerOwned && _panelMode == PanelMode.AttackConfirm)
                SetStatus($"输入派兵数量并确认进攻，最多可派 {GetMaxAttackSoldiers()}");
            else if (canPreserve && !playerOwned && _panelMode == PanelMode.AttackSourceSelection)
                SetStatus("选择出兵据点");
            else if (canPreserve && playerOwned && _panelMode == PanelMode.TransferConfirm)
                SetStatus($"输入调兵数量并确认，最多可调 {GetMaxTransferSoldiers()}");
            else if (canPreserve && playerOwned && _panelMode == PanelMode.TransferTargetSelection)
                SetStatus("选择调入据点");
            else if (playerOwned)
                SetStatus("选择建设、征兵或调兵");
            else if (diplomacyAvailable)
                SetStatus("可进攻；郡城/国都可外交");
            else
                SetStatus("可进攻");
        }

        private void LayoutActionButtons(bool playerOwned)
        {
            if (playerOwned)
            {
                SetButtonRect(buildButton, new Vector2(16f, 18f), new Vector2(124f, 38f));
                SetButtonRect(recruitButton, new Vector2(154f, 18f), new Vector2(124f, 38f));
                SetButtonRect(transferButton, new Vector2(16f, 64f), new Vector2(124f, 38f));
                return;
            }

            SetButtonRect(attackButton, new Vector2(16f, 18f), new Vector2(124f, 38f));
            SetButtonRect(diplomacyButton, new Vector2(154f, 18f), new Vector2(124f, 38f));
        }

        private static void SetButtonRect(Button button, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
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
            string buildingName = SettlementPanelTextFormatter.GetBuildingName(_application.Config, building);
            SetStatus(accepted ? $"已提交建设：{buildingName}，下月执行" : "建设未通过：资源或规则不足");
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

            WorldRuntime world = _application.World;
            if (!world.Settlements.TryGet(_settlementId, out var target))
                return;
            if (target.OwnerId == PlayerNationId)
            {
                SetStatus("不能攻击自己的据点");
                return;
            }

            List<SettlementState> sources = GetAttackSources(target);
            if (sources.Count == 0)
            {
                SetStatus("没有可出兵的玩家据点");
                return;
            }

            _panelMode = PanelMode.AttackSourceSelection;
            _selectedAttackSourceSettlementId = null;
            HideAttackConfirmOnly();
            ShowChoicePanel(sources, SelectAttackSource);
            SetStatus("选择出兵据点");
        }

        private void TransferSelected()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;

            WorldRuntime world = _application.World;
            if (!world.Settlements.TryGet(_settlementId, out var source))
                return;
            if (source.OwnerId != PlayerNationId)
            {
                SetStatus("只能从玩家据点调兵");
                return;
            }

            List<SettlementState> targets = GetTransferTargets(source);
            if (targets.Count == 0)
            {
                SetStatus("没有可调入的玩家据点");
                return;
            }

            _panelMode = PanelMode.TransferTargetSelection;
            _transferSourceSettlementId = source.Id;
            _transferTargetSettlementId = null;
            HideAttackConfirmOnly();
            ShowChoicePanel(targets, SelectTransferTarget);
            SetStatus("选择调入据点");
        }

        private void SelectAttackSource(SettlementState source)
        {
            if (source == null)
                return;

            _selectedAttackSourceSettlementId = source.Id;
            _panelMode = PanelMode.AttackConfirm;
            HideChoicePanel();
            ShowSoldierConfirm("进攻", "派", GetMaxAttackSoldiers());
        }

        private void SelectTransferTarget(SettlementState target)
        {
            if (target == null)
                return;

            _transferTargetSettlementId = target.Id;
            _panelMode = PanelMode.TransferConfirm;
            HideChoicePanel();
            ShowSoldierConfirm("调兵", "调", GetMaxTransferSoldiers());
        }

        private void ConfirmCurrentAction()
        {
            if (_panelMode == PanelMode.TransferConfirm)
                ConfirmTransfer();
            else
                ConfirmAttack();
        }

        private void ConfirmAttack()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;

            WorldRuntime world = _application.World;
            if (!world.Settlements.TryGet(_settlementId, out var target))
                return;
            if (!world.Settlements.TryGet(_selectedAttackSourceSettlementId, out var source))
            {
                SetStatus("请先选择出兵据点");
                return;
            }

            int soldiers = ReadSoldiers();
            if (soldiers <= 0)
            {
                SetStatus("请输入大于 0 的派兵数量");
                return;
            }

            string failure = GetAttackRuleFailure(source, target, soldiers);
            if (!string.IsNullOrEmpty(failure))
            {
                SetStatus(failure);
                return;
            }

            var command = new MoveArmyCommand(PlayerNationId, source.Id, target.RegionId, target.Id, soldiers, _application.Config);
            if (commandDispatcher == null || !commandDispatcher.Enqueue(command))
            {
                SetStatus("进攻未通过：命令系统拒绝，请检查目标和出兵源");
                return;
            }

            HideOperationPanels();
            SetStatus($"已从 {GetSettlementName(source.Id)} 派出 {soldiers} 人进攻 {GetSettlementName(target.Id)}，下月出发");
        }

        private void ConfirmTransfer()
        {
            if (_application?.World == null)
                return;

            WorldRuntime world = _application.World;
            if (!world.Settlements.TryGet(_transferSourceSettlementId, out var source)
                || !world.Settlements.TryGet(_transferTargetSettlementId, out var target))
            {
                SetStatus("请先选择调兵据点");
                return;
            }

            int soldiers = ReadSoldiers();
            if (soldiers <= 0)
            {
                SetStatus("请输入大于 0 的调兵数量");
                return;
            }

            string failure = GetTransferRuleFailure(source, target, soldiers);
            if (!string.IsNullOrEmpty(failure))
            {
                SetStatus(failure);
                return;
            }

            var command = new TransferArmyCommand(PlayerNationId, source.Id, target.Id, soldiers, _application.Config);
            if (commandDispatcher == null || !commandDispatcher.Enqueue(command))
            {
                SetStatus("调兵未通过：命令系统拒绝，请检查目标和出兵源");
                return;
            }

            HideOperationPanels();
            SetStatus($"已从 {GetSettlementName(source.Id)} 调出 {soldiers} 人前往 {GetSettlementName(target.Id)}");
        }

        private void ShowSoldierConfirm(string actionName, string maxVerb, int maxSoldiers)
        {
            if (attackCountInput != null)
            {
                int defaultSoldiers = maxSoldiers > 0 && maxSoldiers < DefaultAttackArmySoldiers ? maxSoldiers : DefaultAttackArmySoldiers;
                attackCountInput.text = defaultSoldiers.ToString();
            }
            if (attackConfirmPanel != null)
                attackConfirmPanel.SetActive(true);
            SetStatus(maxSoldiers > 0 ? $"输入{actionName}数量并确认，最多可{maxVerb} {maxSoldiers}" : "当前没有可派兵力");
        }

        private void HideAttackConfirmOnly()
        {
            if (attackConfirmPanel != null)
                attackConfirmPanel.SetActive(false);
        }

        private void HideOperationPanels()
        {
            _panelMode = PanelMode.None;
            _selectedAttackSourceSettlementId = null;
            _transferSourceSettlementId = null;
            _transferTargetSettlementId = null;
            HideAttackConfirmOnly();
            HideChoicePanel();
        }

        private bool IsAttackConfirmVisible()
        {
            return _panelMode != PanelMode.None
                && ((attackConfirmPanel != null && attackConfirmPanel.activeSelf)
                    || (_choicePanel != null && _choicePanel.activeSelf));
        }

        private void CancelAttack()
        {
            HideOperationPanels();
            SetStatus("已取消军事操作");
        }

        private int ReadSoldiers()
        {
            if (attackCountInput == null || string.IsNullOrWhiteSpace(attackCountInput.text))
                return DefaultAttackArmySoldiers;

            return int.TryParse(attackCountInput.text, out int soldiers) ? soldiers : 0;
        }

        private string GetAttackRuleFailure(SettlementState source, SettlementState target, int soldiers)
        {
            var world = _application?.World;
            var config = _application?.Config;
            if (world == null || config == null)
                return "进攻未通过：游戏配置未就绪";
            if (source.OwnerId != PlayerNationId)
                return $"进攻未通过：出兵据点 {source.Id} 不属于玩家";
            if (target.OwnerId == PlayerNationId)
                return "进攻未通过：不能攻击自己的据点";
            if (!MilitaryCommandRules.CanReach(world, source, target))
                return $"进攻未通过：{source.RegionId} 与 {target.RegionId} 不相邻";

            if (!MilitaryCommandRules.HasArmyCapacity(world, config, PlayerNationId))
                return $"进攻未通过：军队数量已达上限 {config.AI.maxArmyCount}";

            int minGarrison = MilitaryCommandRules.GetMinGarrison(config.Battle, source);
            int maxDraw = (int)(source.Garrison * config.Battle.maxConscriptRate);
            int maxSoldiers = GetMaxAttackSoldiers(source);
            if (soldiers > maxSoldiers)
                return $"进攻未通过：{source.Id} 守军 {source.Garrison}，最多可派 {maxSoldiers}（需留守 {minGarrison}，抽调上限 {maxDraw}）";

            return null;
        }

        private int GetMaxAttackSoldiers()
        {
            var world = _application?.World;
            if (world == null || !world.Settlements.TryGet(_selectedAttackSourceSettlementId, out var source))
                return 0;

            return GetMaxAttackSoldiers(source);
        }

        private int GetMaxAttackSoldiers(SettlementState source)
        {
            return MilitaryCommandRules.GetMaxDeployableSoldiers(_application?.Config, source);
        }

        private int GetMaxTransferSoldiers()
        {
            var world = _application?.World;
            if (world == null || !world.Settlements.TryGet(_transferSourceSettlementId, out var source))
                return 0;

            return MilitaryCommandRules.GetMaxDeployableSoldiers(_application?.Config, source);
        }

        private string GetTransferRuleFailure(SettlementState source, SettlementState target, int soldiers)
        {
            var world = _application?.World;
            var config = _application?.Config;
            if (world == null || config == null)
                return "调兵未通过：游戏配置未就绪";
            if (source.Id == target.Id)
                return "调兵未通过：来源和目标不能相同";
            if (source.OwnerId != PlayerNationId || target.OwnerId != PlayerNationId)
                return "调兵未通过：只能在玩家据点之间调兵";
            if (!MilitaryCommandRules.CanReach(world, source, target))
                return $"调兵未通过：{source.RegionId} 与 {target.RegionId} 不相邻";
            if (!MilitaryCommandRules.HasArmyCapacity(world, config, PlayerNationId))
                return $"调兵未通过：军队数量已达上限 {config.AI.maxArmyCount}";

            int minGarrison = MilitaryCommandRules.GetMinGarrison(config.Battle, source);
            int maxDraw = (int)(source.Garrison * config.Battle.maxConscriptRate);
            int maxSoldiers = MilitaryCommandRules.GetMaxDeployableSoldiers(config, source);
            if (soldiers > maxSoldiers)
                return $"调兵未通过：{source.Id} 守军 {source.Garrison}，最多可调 {maxSoldiers}（需留守 {minGarrison}，抽调上限 {maxDraw}）";

            return null;
        }

        private List<SettlementState> GetAttackSources(SettlementState target)
        {
            var results = new List<SettlementState>();
            var world = _application?.World;
            var config = _application?.Config;
            if (world == null || config == null || target == null)
                return results;
            if (!MilitaryCommandRules.HasArmyCapacity(world, config, PlayerNationId))
                return results;

            foreach (var source in world.Settlements.GetAll())
            {
                if (source.OwnerId != PlayerNationId)
                    continue;
                if (!MilitaryCommandRules.CanReach(world, source, target))
                    continue;
                if (MilitaryCommandRules.GetMaxDeployableSoldiers(config, source) <= 0)
                    continue;

                results.Add(source);
            }

            return results;
        }

        private List<SettlementState> GetTransferTargets(SettlementState source)
        {
            var results = new List<SettlementState>();
            var world = _application?.World;
            var config = _application?.Config;
            if (world == null || config == null || source == null)
                return results;
            if (MilitaryCommandRules.GetMaxDeployableSoldiers(config, source) <= 0)
                return results;
            if (!MilitaryCommandRules.HasArmyCapacity(world, config, PlayerNationId))
                return results;

            foreach (var target in world.Settlements.GetAll())
            {
                if (target.Id == source.Id || target.OwnerId != PlayerNationId)
                    continue;
                if (!MilitaryCommandRules.CanReach(world, source, target))
                    continue;

                results.Add(target);
            }

            return results;
        }

        private void ShowChoicePanel(List<SettlementState> options, System.Action<SettlementState> onSelected)
        {
            EnsureDynamicControls();
            HideChoicePanel();

            if (_choicePanel == null)
                return;

            foreach (var option in options)
            {
                SettlementState selected = option;
                Button button = CreateChoiceButton($"{GetSettlementName(option.Id)}  守军 {option.Garrison}");
                button.onClick.AddListener(() => onSelected(selected));
                _choiceButtons.Add(button);
            }

            _choicePanel.SetActive(true);
        }

        private Button CreateChoiceButton(string label)
        {
            var buttonObject = new GameObject("ChoiceButton");
            buttonObject.transform.SetParent(_choicePanel.transform, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.24f, 0.95f);

            var button = buttonObject.AddComponent<Button>();
            var layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 34f;
            layout.minHeight = 34f;

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            Text text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = ResolveFont();
            text.fontSize = 15;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }

        private Font ResolveFont()
        {
            if (bodyText != null && bodyText.font != null)
                return bodyText.font;
            if (statusText != null && statusText.font != null)
                return statusText.font;
            if (titleText != null && titleText.font != null)
                return titleText.font;
            Font cjkFont = UiTextFontResolver.GetResolvedFont();
            return cjkFont != null ? cjkFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void HideChoicePanel()
        {
            for (int i = 0; i < _choiceButtons.Count; i++)
            {
                if (_choiceButtons[i] != null)
                    Destroy(_choiceButtons[i].gameObject);
            }

            _choiceButtons.Clear();
            if (_choicePanel != null)
                _choicePanel.SetActive(false);
        }

        private bool IsAttackMode()
        {
            return _panelMode == PanelMode.AttackSourceSelection || _panelMode == PanelMode.AttackConfirm;
        }

        private bool IsTransferMode()
        {
            return _panelMode == PanelMode.TransferTargetSelection || _panelMode == PanelMode.TransferConfirm;
        }

        private string GetSettlementName(string settlementId)
        {
            if (_application?.Config != null
                && _application.Config.Settlements.TryGetValue(settlementId, out var config))
            {
                return config.name;
            }

            return settlementId;
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
