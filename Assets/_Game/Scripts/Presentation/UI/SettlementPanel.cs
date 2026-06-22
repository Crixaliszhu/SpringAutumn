using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.UI
{
    public class SettlementPanel : MonoBehaviour
    {
        private const string PlayerNationId = "PLAYER";

        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button recruitButton;
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
            buildButton?.onClick.AddListener(BuildDefault);
            recruitButton?.onClick.AddListener(RecruitDefault);
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
                }
            }
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
            _application?.Events.Unsubscribe<MonthChanged>(OnMonthChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.Type != SelectionType.City && evt.Type != SelectionType.Village)
                return;
            Show(evt.Id);
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
            SetStatus(settlement.OwnerId == PlayerNationId ? "选择建设或征兵" : "非玩家据点不可操作");
            gameObject.SetActive(true);
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
