using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Bootstrap;

namespace SpringAutumn.Presentation.UI
{
    /// <summary>
    /// 游戏结束弹窗：监听 GameEnded 事件，展示胜负结果并提供"重新开始"。
    /// 完全程序化构建（含独立 Canvas），无需在场景中手动连线。
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.14f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.55f, 0.42f, 0.16f, 0.98f);

        private GameApplication _application;
        private GameLauncher _launcher;
        private SceneBindingBootstrap _sceneBinding;

        private GameObject _root;
        private Text _titleText;
        private Text _subtitleText;
        private bool _built;

        public void Bind(GameApplication application, GameLauncher launcher, SceneBindingBootstrap sceneBinding)
        {
            _application?.Events.Unsubscribe<GameEnded>(OnGameEnded);
            _application = application;
            _launcher = launcher;
            _sceneBinding = sceneBinding;

            BuildUi();
            Hide();

            _application.Events.Subscribe<GameEnded>(OnGameEnded);
        }

        private void OnDestroy()
        {
            _application?.Events.Unsubscribe<GameEnded>(OnGameEnded);
        }

        private void OnGameEnded(GameEnded e)
        {
            Debug.Log($"[GameOverView] GameEnded received, playerWon={e.PlayerWon}");
            Show(e.PlayerWon);
        }

        private void Show(bool playerWon)
        {
            if (!_built)
                BuildUi();

            if (_titleText != null)
                _titleText.text = playerWon ? "一统天下" : "败亡";
            if (_subtitleText != null)
                _subtitleText.text = playerWon
                    ? "诸侯尽归，问鼎成功！"
                    : "基业尽失，霸图崩塌。";

            if (_root != null)
                _root.SetActive(true);

            _application?.Pause();
        }

        private void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Restart()
        {
            Hide();
            if (_launcher != null)
            {
                _launcher.NewGame();
                _sceneBinding?.RefreshScene();
            }
            else
            {
                _application?.NewGame();
            }
        }

        private void BuildUi()
        {
            if (_built)
                return;

            // 独立全屏 Canvas，置顶显示。
            var canvasObject = new GameObject("GameOverCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 576f);
            canvasObject.AddComponent<GraphicRaycaster>();

            _root = canvasObject;

            // 半透明遮罩，铺满全屏并拦截点击。
            var dim = CreateChild(canvasObject.transform, "Dim");
            StretchFull(dim);
            var dimImage = dim.AddComponent<Image>();
            dimImage.color = DimColor;

            // 中央面板。
            var panel = CreateChild(dim.transform, "Panel");
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(520f, 320f);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = PanelColor;

            _titleText = CreateText(panel.transform, "Title", new Vector2(0f, 90f), new Vector2(460f, 90f), 56, FontStyle.Bold);
            _subtitleText = CreateText(panel.transform, "Subtitle", new Vector2(0f, 10f), new Vector2(460f, 60f), 26, FontStyle.Normal);

            CreateRestartButton(panel.transform);

            _built = true;
        }

        private void CreateRestartButton(Transform parent)
        {
            var buttonObject = CreateChild(parent, "RestartButton");
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 64f);
            rect.anchoredPosition = new Vector2(0f, -90f);

            var image = buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Restart);

            CreateText(buttonObject.transform, "Label", Vector2.zero, new Vector2(240f, 64f), 28, FontStyle.Bold);
            buttonObject.transform.Find("Label").GetComponent<Text>().text = "重新开始";
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle style)
        {
            var go = CreateChild(parent, name);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<Text>();
            text.font = ResolveFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Font ResolveFont()
        {
            string[] candidates = { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "DengXian", "Arial" };
            foreach (var name in candidates)
            {
                Font font = Font.CreateDynamicFontFromOSFont(name, 24);
                if (font != null)
                    return font;
            }

            // 内置字体回退（不同 Unity 版本名称不同）。
            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtin == null)
                builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return builtin;
        }
    }
}
