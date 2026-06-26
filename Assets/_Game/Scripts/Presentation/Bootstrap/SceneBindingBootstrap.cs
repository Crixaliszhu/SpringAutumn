using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;
using SpringAutumn.Presentation.UI;

namespace SpringAutumn.Presentation.Bootstrap
{
    /// <summary>Connects scene objects to the runtime application after the launcher has initialized.</summary>
    public class SceneBindingBootstrap : MonoBehaviour
    {
        [SerializeField] private GameLauncher launcher;
        [SerializeField] private HudView hudView;
        [SerializeField] private MessageSystem messageSystem;
        [SerializeField] private RegionBriefPanel regionBriefPanel;
        [SerializeField] private SettlementPanel settlementPanel;
        [SerializeField] private MapLayerController mapLayerController;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Text legacyStatusText;
        [SerializeField] private bool startNewGameOnAwake = true;

        private bool _bound;
        private GameObject _gameOverViewObject;
        private GameOverView _gameOverView;

        private void Awake()
        {
            TmpFontResolver.ApplyToScene();
        }

        private void Start()
        {
            BindScene();
        }

        public void BindScene()
        {
            if (_bound)
                return;

            if (launcher == null)
                launcher = FindObjectOfType<GameLauncher>();

            if (launcher == null)
            {
                SetStatus("GameLauncher missing");
                return;
            }

            if (launcher.Application == null && !launcher.Initialize())
            {
                SetStatus("Bootstrap failed: " + launcher.LastError);
                return;
            }

            if (startNewGameOnAwake && launcher.Application.World == null)
                launcher.NewGame();

            if (launcher.Application.World == null)
            {
                SetStatus("Waiting for New Game or Load Game");
                return;
            }

            BindSceneObjects();
            _bound = true;

            var world = launcher.Application.World;
            SetStatus($"World ready: {world.Nations.Count} nations / {world.Regions.Count} regions / {world.Settlements.Count} settlements");
        }

        public void RefreshScene(int loadedSlot = 0)
        {
            if (!_bound)
            {
                BindScene();
                if (_bound)
                    RefreshSceneState(loadedSlot);
                return;
            }

            if (launcher == null || launcher.Application?.World == null)
                return;

            RefreshSceneState(loadedSlot);
        }

        private void RefreshSceneState(int loadedSlot)
        {
            BindSceneObjects();
            selectionManager?.Clear();
            mapLayerController?.ShowWorldMap();
            regionBriefPanel?.Hide();
            settlementPanel?.Hide();
            hudView?.Refresh();
            if (loadedSlot > 0)
                messageSystem?.ResetForLoadedGame(loadedSlot);

            var world = launcher.Application.World;
            SetStatus($"World ready: {world.Nations.Count} nations / {world.Regions.Count} regions / {world.Settlements.Count} settlements");
        }

        private void EnsureGameOverView()
        {
            if (_gameOverViewObject != null)
            {
                _gameOverView?.Bind(launcher.Application, launcher, this);
                return;
            }

            _gameOverViewObject = new GameObject("GameOverView");
            _gameOverViewObject.transform.SetParent(transform, false);
            _gameOverView = _gameOverViewObject.AddComponent<GameOverView>();
            _gameOverView.Bind(launcher.Application, launcher, this);
        }

        private void BindSceneObjects()
        {
            TmpFontResolver.ApplyToScene();
            UiTextFontResolver.ApplyToScene();
            ResolveOptionalSceneObjects();
            selectionManager?.Bind(launcher.Application);
            mapLayerController?.Bind(launcher.Application);
            hudView?.Bind(launcher.Application);
            messageSystem?.Bind(launcher.Application);
            regionBriefPanel?.Bind(launcher.Application);
            settlementPanel?.Bind(launcher.Application);
            EnsureGameOverView();
        }

        private void ResolveOptionalSceneObjects()
        {
            if (settlementPanel == null)
                settlementPanel = FindObjectOfType<SettlementPanel>(true);
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
                if (legacyStatusText == null)
                    legacyStatusText = LegacyTextMirror.FromTmp(statusText);
            }
            LegacyTextMirror.SetText(legacyStatusText, text);
            Debug.Log("[SceneBootstrap] " + text);
        }
    }
}
