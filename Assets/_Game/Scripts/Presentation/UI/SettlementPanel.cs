using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.UI
{
    public class SettlementPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private UICommandDispatcher commandDispatcher;

        private GameApplication _application;
        private string _settlementId;

        public void Bind(GameApplication application)
        {
            _application = application;
            commandDispatcher?.Bind(application);
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            buildButton?.onClick.AddListener(BuildDefault);
            recruitButton?.onClick.AddListener(RecruitDefault);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            if (evt.Type != SelectionType.City && evt.Type != SelectionType.Village)
                return;
            Show(evt.Id);
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
                bodyText.text = $"人口：{settlement.Population}\n粮食：{settlement.Grain}\n铜钱：{settlement.Money}\n守军：{settlement.Garrison}";
            gameObject.SetActive(true);
        }

        private void BuildDefault()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;
            var settlement = _application.World.Settlements.Get(_settlementId);
            string building = settlement.IsCity ? "MARKET" : "FARM";
            commandDispatcher?.Enqueue(new BuildCommand(settlement.OwnerId, settlement.Id, building, _application.Config));
        }

        private void RecruitDefault()
        {
            if (_application?.World == null || string.IsNullOrEmpty(_settlementId))
                return;
            var settlement = _application.World.Settlements.Get(_settlementId);
            commandDispatcher?.Enqueue(new RecruitCommand(settlement.OwnerId, settlement.Id, 10, _application.Config));
        }
    }
}
