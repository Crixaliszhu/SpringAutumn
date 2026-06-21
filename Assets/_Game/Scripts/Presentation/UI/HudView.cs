using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;

namespace SpringAutumn.Presentation.UI
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speed1Button;
        [SerializeField] private Button speed2Button;
        [SerializeField] private Button speed3Button;
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Color normalButtonColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color selectedButtonColor = new Color(0.55f, 0.42f, 0.16f, 0.95f);

        private GameApplication _application;
        private float _currentSpeed = 1f;

        public void Bind(GameApplication application)
        {
            _application = application;
            pauseButton?.onClick.AddListener(ShowPausePanel);
            resumeButton?.onClick.AddListener(ResumeFromPanel);
            speed1Button?.onClick.AddListener(() => SetSpeed(1f));
            speed2Button?.onClick.AddListener(() => SetSpeed(2f));
            speed3Button?.onClick.AddListener(() => SetSpeed(3f));
            menuButton?.onClick.AddListener(ToggleMenu);
            if (pausePanel != null)
                pausePanel.SetActive(false);
            UpdateSpeedButtons();
            Refresh();
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

            if (dateText != null)
                dateText.text = $"第{world.Time.Year}年{world.Time.Month}月";

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

            if (resourceText != null)
                resourceText.text = $"粮 {grain}  钱 {money}  人口 {population}  兵 {soldiers}  Region {regions}";
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

        private void ToggleMenu()
        {
            if (menuPanel != null)
                menuPanel.SetActive(!menuPanel.activeSelf);
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
