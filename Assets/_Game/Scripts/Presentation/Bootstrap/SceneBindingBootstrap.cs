using TMPro;
using UnityEngine;
using SpringAutumn.Presentation.UI;

namespace SpringAutumn.Presentation.Bootstrap
{
    /// <summary>Connects scene objects to the runtime application after the launcher has initialized.</summary>
    public class SceneBindingBootstrap : MonoBehaviour
    {
        [SerializeField] private GameLauncher launcher;
        [SerializeField] private HudView hudView;
        [SerializeField] private MessageSystem messageSystem;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private bool startNewGameOnAwake = true;

        private bool _bound;

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

            TmpFontResolver.ApplyToScene();
            hudView?.Bind(launcher.Application);
            messageSystem?.Bind(launcher.Application);
            _bound = true;

            var world = launcher.Application.World;
            SetStatus($"World ready: {world.Nations.Count} nations / {world.Regions.Count} regions / {world.Settlements.Count} settlements");
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
            Debug.Log("[SceneBootstrap] " + text);
        }
    }
}
