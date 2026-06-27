using System.IO;
using UnityEngine;
using SpringAutumn.Bootstrap;
using SpringAutumn.Config;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Save;

namespace SpringAutumn.Presentation.Bootstrap
{
    /// <summary>Unity 场景入口。负责加载配置、创建 GameApplication，并在 Update 中驱动月度 Tick。</summary>
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private string configResourceDir = "Config";
        [SerializeField] private float secondsPerMonth = GameApplication.DefaultSecondsPerMonth;
        [SerializeField] private bool autoNewGameOnStart;

        public GameApplication Application { get; private set; }
        public ConfigDatabase Config { get; private set; }
        public string LastError { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (autoNewGameOnStart)
                NewGame();
        }

        private void Update()
        {
            Application?.Update(Time.deltaTime);
        }

        public bool Initialize()
        {
            try
            {
                Config = new ConfigLoader().Load(JsonConfigSource.FromResources(configResourceDir));

                string saveDir = Path.Combine(UnityEngine.Application.persistentDataPath, "saves");
                var saveManager = new SaveManager(Config, new FileSaveStorage(saveDir));
                Application = new GameApplication(Config, saveManager)
                {
                    SecondsPerMonth = secondsPerMonth
                };

                LastError = null;
                return true;
            }
            catch (System.Exception ex)
            {
                LastError = ex.Message;
                Debug.LogError("[Bootstrap] " + ex);
                return false;
            }
        }

        public void NewGame()
        {
            EnsureInitialized();
            Application.NewGame();
        }

        public bool LoadGame(int slot)
        {
            EnsureInitialized();
            return Application.LoadGame(slot) != null;
        }

        public void PauseGame()
        {
            Application?.Pause();
        }

        public void ResumeGame()
        {
            Application?.Resume();
        }

        public bool SaveGame(int slot)
        {
            EnsureInitialized();
            return Application.Save(slot);
        }

        public bool ExitGame()
        {
            return Application != null && Application.ExitGame();
        }

        private void EnsureInitialized()
        {
            if (Application == null && !Initialize())
                throw new System.InvalidOperationException(LastError);
        }
    }
}
