using UnityEngine;
using UnityEngine.UI;
using SpringAutumn.Presentation.Bootstrap;

namespace SpringAutumn.Presentation.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameLauncher launcher;
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject settingsPanel;

        private void Awake()
        {
            startButton?.onClick.AddListener(OnStart);
            loadButton?.onClick.AddListener(() => launcher?.LoadGame(GameApplicationSlot));
            settingsButton?.onClick.AddListener(ToggleSettings);
            exitButton?.onClick.AddListener(OnExit);
        }

        private const int GameApplicationSlot = 1;

        private void OnStart()
        {
            launcher?.NewGame();
            gameObject.SetActive(false);
        }

        private void ToggleSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void OnExit()
        {
            launcher?.ExitGame();
            Application.Quit();
        }
    }
}
