using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Presentation.Bootstrap;
using SpringAutumn.Runtime;

namespace SpringAutumn.Presentation.UI
{
    public class HudView : MonoBehaviour
    {
        private const int DefaultSlot = 1;

        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private Text legacyDateText;
        [SerializeField] private Text legacyResourceText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1Button;
        [SerializeField] private Button speed2Button;
        [SerializeField] private Button speed3Button;
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private GameLauncher launcher;
        [SerializeField] private SceneBindingBootstrap sceneBinding;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button closeMenuButton;
        [SerializeField] private TMP_Text menuStatusText;
        [SerializeField] private Text legacyMenuStatusText;
        [SerializeField] private int saveSlot = DefaultSlot;
        [SerializeField] private Color normalButtonColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color selectedButtonColor = new Color(0.55f, 0.42f, 0.16f, 0.95f);

        private GameApplication _application;
        private float _currentSpeed = 1f;
        private bool _listenersRegistered;

        public void Bind(GameApplication application)
        {
            _application = application;
            RegisterButtonListeners();
            ConfigureTextLayout();
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (menuPanel != null)
                menuPanel.SetActive(false);
            UpdateSpeedButtons();
            Refresh();
        }

        private void ConfigureTextLayout()
        {
            if (dateText != null)
            {
                dateText.enableWordWrapping = false;
                legacyDateText = LegacyTextMirror.FromTmp(dateText);
            }
            if (legacyDateText != null)
                legacyDateText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (resourceText != null)
            {
                resourceText.enableWordWrapping = false;
                legacyResourceText = LegacyTextMirror.FromTmp(resourceText);
            }
            if (legacyResourceText != null)
                legacyResourceText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (menuStatusText != null)
                legacyMenuStatusText = LegacyTextMirror.FromTmp(menuStatusText);
        }

        private void RegisterButtonListeners()
        {
            if (_listenersRegistered)
                return;

            pauseButton?.onClick.AddListener(ShowPausePanel);
            resumeButton?.onClick.AddListener(ResumeFromPanel);
            speed1Button?.onClick.AddListener(SetSpeed1);
            speed2Button?.onClick.AddListener(SetSpeed2);
            speed3Button?.onClick.AddListener(SetSpeed3);
            menuButton?.onClick.AddListener(ToggleMenu);
            saveButton?.onClick.AddListener(SaveGame);
            loadButton?.onClick.AddListener(LoadGame);
            closeMenuButton?.onClick.AddListener(CloseMenu);
            _listenersRegistered = true;
        }

        private void OnDestroy()
        {
            if (!_listenersRegistered)
                return;

            pauseButton?.onClick.RemoveListener(ShowPausePanel);
            resumeButton?.onClick.RemoveListener(ResumeFromPanel);
            speed1Button?.onClick.RemoveListener(SetSpeed1);
            speed2Button?.onClick.RemoveListener(SetSpeed2);
            speed3Button?.onClick.RemoveListener(SetSpeed3);
            menuButton?.onClick.RemoveListener(ToggleMenu);
            saveButton?.onClick.RemoveListener(SaveGame);
            loadButton?.onClick.RemoveListener(LoadGame);
            closeMenuButton?.onClick.RemoveListener(CloseMenu);
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            var world = _application?.World;
            if (world == null)
                return;

            string date = FormatDate(world.Time);
            if (dateText != null)
            {
                dateText.text = date;
                LegacyTextMirror.SetText(legacyDateText, date);
            }
            else
            {
                LegacyTextMirror.SetText(legacyDateText, date);
            }

            int grain = 0;
            int money = 0;
            int population = 0;
            int soldiers = 0;
            int regions = 0;
            foreach (var settlement in world.Settlements.GetAll())
            {
                if (settlement.OwnerId != "PLAYER")
                    continue;
                grain += settlement.Grain;
                money += settlement.Money;
                population += settlement.Population;
                soldiers += settlement.Garrison;
            }
            foreach (var region in world.Regions.GetAll())
            {
                if (region.OwnerId == "PLAYER")
                    regions++;
            }

            string resources = FormatResources(grain, money, population, soldiers, regions);
            if (resourceText != null)
            {
                resourceText.text = resources;
                LegacyTextMirror.SetText(legacyResourceText, resources);
            }
            else
            {
                LegacyTextMirror.SetText(legacyResourceText, resources);
            }
        }

        public static string FormatDate(GameTimeState time)
        {
            return time == null ? "第1年1月" : $"第{time.Year}年{time.Month}月";
        }

        public static string FormatResources(int grain, int money, int population, int soldiers, int regions)
        {
            return $"粮:{grain} 钱:{money} 人:{population} 兵:{soldiers} 郡:{regions}";
        }

        private void ShowPausePanel()
        {
            if (_application == null)
                return;

            _application.Pause();
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        private void ResumeFromPanel()
        {
            if (_application == null)
                return;

            _application.Resume();
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void SetSpeed(float speed)
        {
            if (_application != null && speed > 0f)
            {
                _application.SecondsPerMonth = GameApplication.DefaultSecondsPerMonth / speed;
                _currentSpeed = speed;
                UpdateSpeedButtons();
            }
        }

        private void SetSpeed1() => SetSpeed(1f);
        private void SetSpeed2() => SetSpeed(2f);
        private void SetSpeed3() => SetSpeed(3f);

        private void ToggleMenu()
        {
            if (menuPanel != null)
                menuPanel.SetActive(!menuPanel.activeSelf);
        }

        private void CloseMenu()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        private void SaveGame()
        {
            bool ok = launcher != null ? launcher.SaveGame(saveSlot) : _application != null && _application.Save(saveSlot);
            SetMenuStatus(ok ? $"已保存到槽位 {saveSlot}" : "保存失败");
        }

        private void LoadGame()
        {
            bool ok = launcher != null && launcher.LoadGame(saveSlot);
            if (!ok)
            {
                SetMenuStatus(launcher?.Application?.SaveManager?.LastError ?? "读取失败");
                return;
            }

            sceneBinding?.RefreshScene(saveSlot);
            Refresh();
            SetMenuStatus($"已读取槽位 {saveSlot}");
        }

        private void SetMenuStatus(string text)
        {
            if (menuStatusText != null)
            {
                menuStatusText.text = text;
                LegacyTextMirror.SetText(legacyMenuStatusText, text);
            }
            else
            {
                LegacyTextMirror.SetText(legacyMenuStatusText, text);
            }
        }

        private void UpdateSpeedButtons()
        {
            SetButtonSelected(speed1Button, Mathf.Approximately(_currentSpeed, 1f));
            SetButtonSelected(speed2Button, Mathf.Approximately(_currentSpeed, 2f));
            SetButtonSelected(speed3Button, Mathf.Approximately(_currentSpeed, 3f));
        }

        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null)
                return;

            Color color = selected ? selectedButtonColor : normalButtonColor;
            if (button.targetGraphic != null)
                button.targetGraphic.color = color;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = selected ? selectedButtonColor : new Color(0.14f, 0.14f, 0.14f, 0.9f);
            button.colors = colors;
        }
    }
}
