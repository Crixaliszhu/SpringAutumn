using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Input;

namespace SpringAutumn.Presentation.UI
{
    public class MessageSystem : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Text legacyMessageText;
        [SerializeField] private int maxMessages = 50;

        [Header("日志面板布局（左上角，覆盖场景值）")]
        [SerializeField] private bool overrideLayout = true;
        [Tooltip("相对画布左上角的偏移（x 向右、y 向下为负）。")]
        [SerializeField] private Vector2 panelAnchoredPosition = new Vector2(12f, -92f);
        [SerializeField] private Vector2 panelSize = new Vector2(252f, 124f);

        private readonly Queue<string> _messages = new Queue<string>();
        private GameApplication _application;
        private ScrollRect _scrollRect;
        private bool _layoutBuilt;

        public void Bind(GameApplication application)
        {
            Unsubscribe();
            _application = application;
            BuildLayout();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void ResetForLoadedGame(int slot)
        {
            _messages.Clear();
            if (messageText != null)
                messageText.text = string.Empty;
            if (legacyMessageText != null)
                legacyMessageText.text = string.Empty;

            var world = _application?.World;
            if (world != null)
                Add($"读档{slot}：第{world.Time.Year}年{world.Time.Month}月");
        }

        private void Subscribe()
        {
            if (_application == null)
                return;

            _application.Events.Subscribe<MonthChanged>(OnMonthChanged);
            _application.Events.Subscribe<WarDeclared>(OnWarDeclared);
            _application.Events.Subscribe<RegionCaptured>(OnRegionCaptured);
            _application.Events.Subscribe<BattleFinished>(OnBattleFinished);
            _application.Events.Subscribe<BuildingFinished>(OnBuildingFinished);
            _application.Events.Subscribe<RecruitFinished>(OnRecruitFinished);
            _application.Events.Subscribe<SelectionChanged>(OnSelectionChanged);
            _application.Events.Subscribe<GameEnded>(OnGameEnded);
        }

        private void Unsubscribe()
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
            _application.Events.Unsubscribe<GameEnded>(OnGameEnded);
        }

        /// <summary>把日志面板收缩到左上角，并把文本包进 ScrollRect 支持滚动。</summary>
        private void BuildLayout()
        {
            if (_layoutBuilt || (messageText == null && legacyMessageText == null))
                return;

            if (legacyMessageText == null)
                legacyMessageText = LegacyTextMirror.FromTmp(messageText);

            var panel = GetComponent<RectTransform>();
            if (panel == null && messageText != null && messageText.transform.parent != null)
                panel = messageText.transform.parent as RectTransform;
            if (panel == null && legacyMessageText != null && legacyMessageText.transform.parent != null)
                panel = legacyMessageText.transform.parent as RectTransform;
            if (panel == null)
                return;

            if (overrideLayout)
            {
                panel.anchorMin = new Vector2(0f, 1f);
                panel.anchorMax = new Vector2(0f, 1f);
                panel.pivot = new Vector2(0f, 1f);
                panel.anchoredPosition = panelAnchoredPosition;
                panel.sizeDelta = panelSize;
            }

            // 视口（裁剪区）。
            var viewportObject = new GameObject("Viewport");
            var viewport = viewportObject.AddComponent<RectTransform>();
            viewport.SetParent(panel, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0f, 1f);
            viewport.offsetMin = new Vector2(6f, 6f);
            viewport.offsetMax = new Vector2(-6f, -6f);
            viewportObject.AddComponent<RectMask2D>();

            // 把文本作为可滚动内容挂到视口下，顶部对齐、按内容撑高。
            var content = legacyMessageText != null ? legacyMessageText.rectTransform : messageText.rectTransform;
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            content.anchoredPosition = Vector2.zero;
            if (messageText != null)
            {
                messageText.alignment = TextAlignmentOptions.TopLeft;
                messageText.enableWordWrapping = true;
            }
            if (legacyMessageText != null)
            {
                legacyMessageText.alignment = TextAnchor.UpperLeft;
                legacyMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
                legacyMessageText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            var fitter = content.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect = panel.gameObject.GetComponent<ScrollRect>();
            if (_scrollRect == null)
                _scrollRect = panel.gameObject.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewport;
            _scrollRect.content = content;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 18f;

            _layoutBuilt = true;
        }

        private void Add(string text)
        {
            _messages.Enqueue(text);
            while (_messages.Count > maxMessages)
                _messages.Dequeue();

            string joined = string.Join("\n", _messages.ToArray());
            if (messageText != null)
                messageText.text = joined;
            LegacyTextMirror.SetText(legacyMessageText, joined);
            if (messageText != null || legacyMessageText != null)
            {
                ScrollToBottom();
            }
        }

        private void ScrollToBottom()
        {
            if (_scrollRect == null)
                return;
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void OnMonthChanged(MonthChanged e) => Add($"{e.Year}年{e.Month}月");
        private void OnWarDeclared(WarDeclared e) => Add($"{e.AttackerNationId} 向 {e.DefenderNationId} 宣战");
        private void OnRegionCaptured(RegionCaptured e) => Add($"{e.RegionId} 易主：{e.OldOwnerId} → {e.NewOwnerId}");
        private void OnBattleFinished(BattleFinished e) => Add($"{e.SettlementId} 战斗结束，攻方胜利：{e.AttackerWon}");
        private void OnBuildingFinished(BuildingFinished e) => Add($"{e.SettlementId} 建筑完成：{e.BuildingId}");
        private void OnRecruitFinished(RecruitFinished e) => Add($"{e.SettlementId} 征兵完成：{e.Count}");
        private void OnSelectionChanged(SelectionChanged e) => Add($"选择：{e.Type} {e.Id}");
        private void OnGameEnded(GameEnded e) => Add(e.PlayerWon ? "★ 游戏结束：一统天下，问鼎成功！" : "☠ 游戏结束：基业尽失，败亡。");
    }
}
