using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.UI
{
    public class MessageSystem : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private int maxMessages = 20;

        private readonly Queue<string> _messages = new Queue<string>();
        private GameApplication _application;

        public void Bind(GameApplication application)
        {
            _application = application;
            _application.Events.Subscribe<MonthChanged>(OnMonthChanged);
            _application.Events.Subscribe<WarDeclared>(OnWarDeclared);
            _application.Events.Subscribe<RegionCaptured>(OnRegionCaptured);
            _application.Events.Subscribe<BattleFinished>(OnBattleFinished);
            _application.Events.Subscribe<BuildingFinished>(OnBuildingFinished);
            _application.Events.Subscribe<RecruitFinished>(OnRecruitFinished);
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnDestroy()
        {
            if (_application == null)
                return;
            _application.Events.Unsubscribe<MonthChanged>(OnMonthChanged);
            _application.Events.Unsubscribe<WarDeclared>(OnWarDeclared);
            _application.Events.Unsubscribe<RegionCaptured>(OnRegionCaptured);
            _application.Events.Unsubscribe<BattleFinished>(OnBattleFinished);
            _application.Events.Unsubscribe<BuildingFinished>(OnBuildingFinished);
            _application.Events.Unsubscribe<RecruitFinished>(OnRecruitFinished);
            _application.Events.Unsubscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void Add(string text)
        {
            _messages.Enqueue(text);
            while (_messages.Count > maxMessages)
                _messages.Dequeue();

            if (messageText != null)
                messageText.text = string.Join("\n", _messages.ToArray());
        }

        private void OnMonthChanged(MonthChanged e) => Add($"{e.Year}年{e.Month}月");
        private void OnWarDeclared(WarDeclared e) => Add($"{e.AttackerNationId} 向 {e.DefenderNationId} 宣战");
        private void OnRegionCaptured(RegionCaptured e) => Add($"{e.RegionId} 易主：{e.OldOwnerId} → {e.NewOwnerId}");
        private void OnBattleFinished(BattleFinished e) => Add($"{e.SettlementId} 战斗结束，攻方胜利：{e.AttackerWon}");
        private void OnBuildingFinished(BuildingFinished e) => Add($"{e.SettlementId} 建筑完成：{e.BuildingId}");
        private void OnRecruitFinished(RecruitFinished e) => Add($"{e.SettlementId} 征兵完成：{e.Count}");
        private void OnSelectionChanged(SelectionChanged e) => Add($"选择：{e.Type} {e.Id}");
    }
}
