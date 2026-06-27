using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpringAutumn.Presentation.Bootstrap;

namespace SpringAutumn.Presentation.UI
{
    public class MainMenuView : MonoBehaviour
    {
        private const int DefaultSlot = 1;

        [SerializeField] private GameLauncher launcher;
        [SerializeField] private SceneBindingBootstrap sceneBinding;
        [SerializeField] private Button startButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Text statusText;
        [SerializeField] private int saveSlot = DefaultSlot;

        private void Awake()
        {
            ResolveReferences();
            MirrorTmpTexts();
            startButton?.onClick.AddListener(OnStart);
            loadButton?.onClick.AddListener(OnLoad);
            settingsButton?.onClick.AddListener(ToggleSettings);
            exitButton?.onClick.AddListener(OnExit);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void MirrorTmpTexts()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                LegacyTextMirror.FromTmp(texts[i]);
        }

        private void OnDestroy()
        {
            startButton?.onClick.RemoveListener(OnStart);
            loadButton?.onClick.RemoveListener(OnLoad);
            settingsButton?.onClick.RemoveListener(ToggleSettings);
            exitButton?.onClick.RemoveListener(OnExit);
        }

        private void ResolveReferences()
        {
            if (launcher == null)
                launcher = FindObjectOfType<GameLauncher>();
            if (sceneBinding == null)
                sceneBinding = FindObjectOfType<SceneBindingBootstrap>();
        }

        private void OnStart()
        {
            if (launcher == null)
            {
                SetStatus("启动器缺失");
                return;
            }

            launcher.NewGame();
            sceneBinding?.BindScene();
            gameObject.SetActive(false);
        }

        private void OnLoad()
        {
            if (launcher == null)
            {
                SetStatus("启动器缺失");
                return;
            }

            if (!launcher.LoadGame(saveSlot))
            {
                SetStatus(launcher.Application?.SaveManager?.LastError ?? "读取失败");
                return;
            }

            sceneBinding?.BindScene();
            sceneBinding?.RefreshScene(saveSlot);
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

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }
    }
}
