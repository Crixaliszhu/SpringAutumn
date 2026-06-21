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

        private GameApplication _application;

        public void Bind(GameApplication application)
        {
            _application = application;
            pauseButton?.onClick.AddListener(TogglePause);
            speed1Button?.onClick.AddListener(() => SetSpeed(1f));
            speed2Button?.onClick.AddListener(() => SetSpeed(2f));
            speed3Button?.onClick.AddListener(() => SetSpeed(3f));
            menuButton?.onClick.AddListener(ToggleMenu);
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

        private void TogglePause()
        {
            if (_application == null)
                return;
            if (_application.IsPaused) _application.Resume();
            else _application.Pause();
        }

        private void SetSpeed(float speed)
        {
            if (_application != null && speed > 0f)
                _application.SecondsPerMonth = GameApplication.DefaultSecondsPerMonth / speed;
        }

        private void ToggleMenu()
        {
            if (menuPanel != null)
                menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }
}
