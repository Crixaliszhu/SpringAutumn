using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Presentation.Bootstrap;
using SpringAutumn.Presentation.Camera;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;
using SpringAutumn.Presentation.UI;

namespace SpringAutumn.EditorTools
{
    public static class ScenePrefabBuilder
    {
        private const string SceneDir = "Assets/_Game/Scenes";
        private const string PrefabDir = "Assets/_Game/Prefabs";
        private const string MapPrefabDir = PrefabDir + "/Map";
        private const string RegionPrefabPath = MapPrefabDir + "/Region.prefab";
        private const string CityPrefabPath = MapPrefabDir + "/City.prefab";
        private const string VillagePrefabPath = MapPrefabDir + "/Village.prefab";
        private const string ArmyPrefabPath = MapPrefabDir + "/Army.prefab";
        private const string FontDir = "Assets/_Game/Resources/Fonts";
        private const string LocalCjkFontPath = FontDir + "/SpringAutumnLocalCJK.ttf";
        private const string CjkFontAssetPath = FontDir + "/SpringAutumn CJK SDF.asset";
        private const string BootstrapScenePath = SceneDir + "/BootstrapScene.scene";
        private const string BuildRequestPath = "Temp/SpringAutumnBuildStage1.request";
        private static TMP_FontAsset _cjkFontAsset;

        [InitializeOnLoadMethod]
        private static void RunRequestedBuild()
        {
            if (!File.Exists(BuildRequestPath))
                return;

            try
            {
                File.Delete(BuildRequestPath);
                BuildStage1And2();
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        [MenuItem("SpringAutumn/Build Scenes/Stage 1-10 Bootstrap HUD FinalCheck")]
        public static void BuildStage1And2()
        {
            EnsureFolders();
            RebuildGeneratedFontAsset();
            int regionLayer = EnsureUserLayer("Region");
            int cityLayer = EnsureUserLayer("City");
            int villageLayer = EnsureUserLayer("Village");
            int terrainLayer = EnsureUserLayer("Terrain");
            int armyLayer = EnsureUserLayer("Army");
            RegionView regionPrefab = CreateRegionPrefab(regionLayer);
            CityView cityPrefab = CreateCityPrefab(cityLayer);
            VillageView villagePrefab = CreateVillagePrefab(villageLayer);
            ArmyView armyPrefab = CreateArmyPrefab(armyLayer);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BootstrapScene";

            var launcherObject = new GameObject("GameLauncher");
            var launcher = launcherObject.AddComponent<GameLauncher>();
            SetSerializedValue(launcher, "configRelativePath", "_Game/Config");
            SetSerializedValue(launcher, "secondsPerMonth", GameApplication.DefaultSecondsPerMonth);
            SetSerializedValue(launcher, "autoNewGameOnStart", false);

            var bindingObject = new GameObject("SceneBindingBootstrap");
            var binding = bindingObject.AddComponent<SceneBindingBootstrap>();
            SetSerializedValue(binding, "launcher", launcher);
            SetSerializedValue(binding, "startNewGameOnAwake", false);

            CreateEventSystem();

            Camera worldCamera = CreateGameCameras(out CameraManager cameraManager);
            MapLayerController mapLayerController = CreateWorldMap(regionPrefab, cityPrefab, villagePrefab, armyPrefab, terrainLayer, out RegionMapView regionMapView);
            SetSerializedValue(mapLayerController, "cameraManager", cameraManager);
            SelectionManager selectionManager = CreateInputSystem(worldCamera, cameraManager, regionLayer, cityLayer, villageLayer, armyLayer, terrainLayer);

            Canvas canvas = CreateCanvas(worldCamera);
            GameObject hudRoot = CreatePanel("HUD", canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 88f));
            var hud = hudRoot.AddComponent<HudView>();

            TMP_Text dateText = CreateText("DateText", hudRoot.transform, "第1年1月", 22, TextAlignmentOptions.Left);
            SetRect(dateText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(220f, 32f));

            TMP_Text resourceText = CreateText("ResourceText", hudRoot.transform, "粮 0  钱 0  人口 0  兵 0  Region 0", 20, TextAlignmentOptions.Left);
            SetRect(resourceText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(240f, -14f), new Vector2(-540f, 30f));

            Button pauseButton = CreateButton("PauseButton", hudRoot.transform, "暂停", new Vector2(1f, 1f), new Vector2(-390f, -14f), new Vector2(72f, 32f));
            Button speed1Button = CreateButton("Speed1Button", hudRoot.transform, "1x", new Vector2(1f, 1f), new Vector2(-306f, -14f), new Vector2(54f, 32f));
            Button speed2Button = CreateButton("Speed2Button", hudRoot.transform, "2x", new Vector2(1f, 1f), new Vector2(-242f, -14f), new Vector2(54f, 32f));
            Button speed3Button = CreateButton("Speed3Button", hudRoot.transform, "3x", new Vector2(1f, 1f), new Vector2(-178f, -14f), new Vector2(54f, 32f));
            Button menuButton = CreateButton("MenuButton", hudRoot.transform, "菜单", new Vector2(1f, 1f), new Vector2(-96f, -14f), new Vector2(72f, 32f));
            SetButtonColor(speed1Button, new Color(0.55f, 0.42f, 0.16f, 0.95f));

            GameObject menuPanel = CreatePanel("MenuPanel", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -250f), new Vector2(260f, 220f));
            TMP_Text menuTitle = CreateText("MenuPanelText", menuPanel.transform, "游戏菜单", 20, TextAlignmentOptions.Center);
            SetRect(menuTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(0f, 30f));
            Button saveButton = CreateButton("SaveButton", menuPanel.transform, "保存", new Vector2(0f, 1f), new Vector2(26f, -58f), new Vector2(96f, 34f));
            Button loadButton = CreateButton("LoadButton", menuPanel.transform, "读档", new Vector2(0f, 1f), new Vector2(138f, -58f), new Vector2(96f, 34f));
            Button closeMenuButton = CreateButton("CloseMenuButton", menuPanel.transform, "关闭", new Vector2(0f, 1f), new Vector2(82f, -104f), new Vector2(96f, 34f));
            TMP_Text menuStatusText = CreateText("MenuStatusText", menuPanel.transform, "槽位 1", 15, TextAlignmentOptions.Center);
            SetRect(menuStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 28f));
            menuPanel.SetActive(false);

            GameObject pausePanel = CreatePanel("PausePanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 200f));
            TMP_Text pauseTitle = CreateText("PauseTitle", pausePanel.transform, "游戏已暂停", 26, TextAlignmentOptions.Center);
            SetCenteredRect(pauseTitle.rectTransform, new Vector2(0f, 38f), new Vector2(260f, 48f));
            Button resumeButton = CreateButton("ResumeButton", pausePanel.transform, "继续游戏", new Vector2(0.5f, 0.5f), new Vector2(86f, -56f), new Vector2(160f, 44f));
            SetCenteredRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0f, -48f), new Vector2(160f, 44f));
            pausePanel.SetActive(false);

            SetSerializedValue(hud, "dateText", dateText);
            SetSerializedValue(hud, "resourceText", resourceText);
            SetSerializedValue(hud, "pauseButton", pauseButton);
            SetSerializedValue(hud, "speed1Button", speed1Button);
            SetSerializedValue(hud, "speed2Button", speed2Button);
            SetSerializedValue(hud, "speed3Button", speed3Button);
            SetSerializedValue(hud, "menuButton", menuButton);
            SetSerializedValue(hud, "menuPanel", menuPanel);
            SetSerializedValue(hud, "pausePanel", pausePanel);
            SetSerializedValue(hud, "resumeButton", resumeButton);
            SetSerializedValue(hud, "launcher", launcher);
            SetSerializedValue(hud, "sceneBinding", binding);
            SetSerializedValue(hud, "saveButton", saveButton);
            SetSerializedValue(hud, "loadButton", loadButton);
            SetSerializedValue(hud, "closeMenuButton", closeMenuButton);
            SetSerializedValue(hud, "menuStatusText", menuStatusText);
            SetSerializedValue(hud, "saveSlot", GameApplication.AutoSaveSlot);

            GameObject messagePanel = CreatePanel("MessagePanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(320f, -190f));
            var messageSystem = messagePanel.AddComponent<MessageSystem>();
            TMP_Text messageText = CreateText("MessageText", messagePanel.transform, "", 18, TextAlignmentOptions.TopLeft);
            SetRect(messageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-20f, -20f));
            SetSerializedValue(messageSystem, "messageText", messageText);

            GameObject regionBriefRoot = CreatePanel("RegionBriefPanel", canvas.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, -24f), new Vector2(300f, 300f));
            var regionBriefPanel = regionBriefRoot.AddComponent<RegionBriefPanel>();

            Text regionBriefTitle = CreateLegacyText("TitleText", regionBriefRoot.transform, "区域简报", 22, TextAnchor.MiddleLeft);
            SetRect(regionBriefTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(268f, 34f));

            Text regionBriefBody = CreateLegacyText("BodyText", regionBriefRoot.transform, "", 18, TextAnchor.UpperLeft);
            SetRect(regionBriefBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -60f), new Vector2(268f, 160f));

            Button enterRegionButton = CreateButton("EnterRegionButton", regionBriefRoot.transform, "进入区域", new Vector2(0f, 0f), new Vector2(112f, 62f), new Vector2(120f, 34f));
            SetRect(enterRegionButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(102f, 34f));

            SetSerializedValue(regionBriefPanel, "titleText", regionBriefTitle);
            SetSerializedValue(regionBriefPanel, "bodyText", regionBriefBody);
            SetSerializedValue(regionBriefPanel, "enterRegionButton", enterRegionButton);
            SetSerializedValue(regionBriefPanel, "mapLayerController", mapLayerController);
            regionBriefRoot.SetActive(false);

            CreateRegionMapOverlay(canvas.transform, regionMapView);
            SettlementPanel settlementPanel = CreateSettlementPanel(canvas.transform);

            TMP_Text statusText = CreateText("StatusText", canvas.transform, "Initializing...", 18, TextAlignmentOptions.Right);
            SetRect(statusText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(520f, 32f));

            SetSerializedValue(binding, "hudView", hud);
            SetSerializedValue(binding, "messageSystem", messageSystem);
            SetSerializedValue(binding, "regionBriefPanel", regionBriefPanel);
            SetSerializedValue(binding, "settlementPanel", settlementPanel);
            SetSerializedValue(binding, "mapLayerController", mapLayerController);
            SetSerializedValue(binding, "selectionManager", selectionManager);
            SetSerializedValue(binding, "statusText", statusText);

            CreateMainMenu(canvas.transform, launcher, binding);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpringAutumn] Stage 1-10 scene generated: " + BootstrapScenePath);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(SceneDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MapPrefabDir);
            Directory.CreateDirectory(FontDir);
        }

        private static void CreateMainMenu(Transform canvasTransform, GameLauncher launcher, SceneBindingBootstrap binding)
        {
            GameObject root = CreatePanel("MainMenu", canvasTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var background = root.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.02f, 0.03f, 0.02f, 0.96f);
            var menu = root.AddComponent<MainMenuView>();

            TMP_Text title = CreateText("Title", root.transform, "春秋问鼎", 42, TextAlignmentOptions.Center);
            SetCenteredRect(title.rectTransform, new Vector2(0f, 150f), new Vector2(360f, 60f));

            TMP_Text subtitle = CreateText("Subtitle", root.transform, "从流民村开始，问鼎天下", 18, TextAlignmentOptions.Center);
            SetCenteredRect(subtitle.rectTransform, new Vector2(0f, 102f), new Vector2(420f, 36f));

            Button startButton = CreateButton("StartButton", root.transform, "开始游戏", new Vector2(0.5f, 0.5f), new Vector2(90f, 46f), new Vector2(180f, 42f));
            SetCenteredRect(startButton.GetComponent<RectTransform>(), new Vector2(0f, 42f), new Vector2(180f, 42f));
            Button loadButton = CreateButton("LoadButton", root.transform, "读取槽位 1", new Vector2(0.5f, 0.5f), new Vector2(90f, -6f), new Vector2(180f, 42f));
            SetCenteredRect(loadButton.GetComponent<RectTransform>(), new Vector2(0f, -8f), new Vector2(180f, 42f));
            Button settingsButton = CreateButton("SettingsButton", root.transform, "设置", new Vector2(0.5f, 0.5f), new Vector2(90f, -58f), new Vector2(180f, 42f));
            SetCenteredRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, -58f), new Vector2(180f, 42f));
            Button exitButton = CreateButton("ExitButton", root.transform, "退出", new Vector2(0.5f, 0.5f), new Vector2(90f, -110f), new Vector2(180f, 42f));
            SetCenteredRect(exitButton.GetComponent<RectTransform>(), new Vector2(0f, -108f), new Vector2(180f, 42f));

            Text statusText = CreateLegacyText("StatusText", root.transform, "", 16, TextAnchor.MiddleCenter);
            SetCenteredRect(statusText.rectTransform, new Vector2(0f, -164f), new Vector2(420f, 36f));

            GameObject settingsPanel = CreatePanel("SettingsPanel", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(360f, 52f));
            Text settingsText = CreateLegacyText("SettingsText", settingsPanel.transform, "设置项后续接入；当前版本使用默认配置", 15, TextAnchor.MiddleCenter);
            SetRect(settingsText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            settingsPanel.SetActive(false);

            SetSerializedValue(menu, "launcher", launcher);
            SetSerializedValue(menu, "sceneBinding", binding);
            SetSerializedValue(menu, "startButton", startButton);
            SetSerializedValue(menu, "loadButton", loadButton);
            SetSerializedValue(menu, "settingsButton", settingsButton);
            SetSerializedValue(menu, "exitButton", exitButton);
            SetSerializedValue(menu, "settingsPanel", settingsPanel);
            SetSerializedValue(menu, "statusText", statusText);
            SetSerializedValue(menu, "saveSlot", GameApplication.AutoSaveSlot);
        }

        private static Canvas CreateCanvas(Camera worldCamera)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Camera CreateGameCameras(out CameraManager cameraManager)
        {
            Camera worldCamera = CreateSceneCamera("WorldCamera", true, new Vector3(0f, 0f, -10f), 5f);
            Camera regionCamera = CreateSceneCamera("RegionCamera", false, new Vector3(0f, 0f, -10f), 4.2f);

            var worldController = worldCamera.gameObject.AddComponent<WorldCameraController>();
            SetSerializedValue(worldController, "targetCamera", worldCamera);
            SetSerializedValue(worldController, "xBounds", new Vector2(-3f, 3f));
            SetSerializedValue(worldController, "zBounds", new Vector2(-2f, 2f));
            SetSerializedValue(worldController, "heightBounds", new Vector2(3.4f, 6.2f));
            SetSerializedValue(worldController, "panSpeed", 0.006f);
            SetSerializedValue(worldController, "zoomSpeed", 3.5f);

            var regionController = regionCamera.gameObject.AddComponent<RegionCameraController>();
            SetSerializedValue(regionController, "targetCamera", regionCamera);
            SetSerializedValue(regionController, "xBounds", new Vector2(-2f, 4f));
            SetSerializedValue(regionController, "zBounds", new Vector2(-2.5f, 2.5f));
            SetSerializedValue(regionController, "heightBounds", new Vector2(2.8f, 5.6f));
            SetSerializedValue(regionController, "panSpeed", 0.005f);
            SetSerializedValue(regionController, "zoomSpeed", 3f);

            var cameraRoot = new GameObject("CameraSystem");
            cameraManager = cameraRoot.AddComponent<CameraManager>();
            var cameraInput = cameraRoot.AddComponent<CameraInputAdapter>();
            SetSerializedValue(cameraManager, "worldCameraController", worldController);
            SetSerializedValue(cameraManager, "regionCameraController", regionController);
            SetSerializedValue(cameraInput, "cameraManager", cameraManager);
            SetSerializedValue(cameraInput, "wheelZoomScale", 1f);
            SetSerializedValue(cameraInput, "handleDrag", false);
            SetSerializedValue(cameraInput, "handleZoom", true);

            regionCamera.gameObject.SetActive(false);
            return worldCamera;
        }

        private static Camera CreateSceneCamera(string name, bool isMainCamera, Vector3 position, float orthographicSize)
        {
            var cameraObject = new GameObject(name);
            if (isMainCamera)
                cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.08f);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        private static MapLayerController CreateWorldMap(RegionView regionPrefab, CityView cityPrefab, VillageView villagePrefab, ArmyView armyPrefab, int terrainLayer, out RegionMapView regionMapView)
        {
            var worldRoot = new GameObject("WorldMapRoot");
            worldRoot.transform.position = new Vector3(1.6f, -0.25f, 0f);
            var worldMapView = worldRoot.AddComponent<WorldMapView>();

            var regionRoot = new GameObject("RegionRoot");
            regionRoot.transform.SetParent(worldRoot.transform, false);

            var borderObject = new GameObject("NationBorderView");
            borderObject.transform.SetParent(worldRoot.transform, false);
            var borderView = borderObject.AddComponent<NationBorderView>();

            SetSerializedValue(worldMapView, "regionRoot", regionRoot.transform);
            SetSerializedValue(worldMapView, "regionViewPrefab", regionPrefab);
            SetSerializedValue(worldMapView, "nationBorderView", borderView);

            var regionMapRoot = new GameObject("RegionMapRoot");
            regionMapView = regionMapRoot.AddComponent<RegionMapView>();

            var terrainObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terrainObject.name = "TerrainView";
            terrainObject.layer = terrainLayer;
            terrainObject.transform.SetParent(regionMapRoot.transform, false);
            terrainObject.transform.localPosition = new Vector3(1.6f, -0.2f, 0.08f);
            terrainObject.transform.localScale = new Vector3(6.6f, 4.2f, 0.05f);
            var terrainRenderer = terrainObject.GetComponent<Renderer>();
            if (terrainRenderer != null)
                terrainRenderer.sharedMaterial.color = new Color(0.12f, 0.15f, 0.12f);
            var terrainView = terrainObject.AddComponent<TerrainView>();

            var terrainLabelObject = new GameObject("Label");
            terrainLabelObject.transform.SetParent(terrainObject.transform, false);
            terrainLabelObject.transform.localPosition = new Vector3(0f, 0.5f, -0.18f);
            terrainLabelObject.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            var terrainLabel = terrainLabelObject.AddComponent<TextMesh>();
            terrainLabel.text = "REGION";
            terrainLabel.anchor = TextAnchor.MiddleCenter;
            terrainLabel.alignment = TextAlignment.Center;
            terrainLabel.characterSize = 0.09f;
            terrainLabel.fontSize = 32;
            terrainLabel.color = new Color(0.95f, 0.92f, 0.84f);
            SetSerializedValue(terrainView, "label", terrainLabel);

            var settlementRoot = new GameObject("SettlementRoot");
            settlementRoot.transform.SetParent(regionMapRoot.transform, false);
            settlementRoot.transform.localPosition = new Vector3(1.6f, -0.25f, -0.06f);

            var armyRoot = new GameObject("ArmyRoot");
            armyRoot.transform.SetParent(regionMapRoot.transform, false);
            armyRoot.transform.localPosition = new Vector3(1.6f, -0.25f, -0.12f);

            SetSerializedValue(regionMapView, "settlementRoot", settlementRoot.transform);
            SetSerializedValue(regionMapView, "armyRoot", armyRoot.transform);
            SetSerializedValue(regionMapView, "terrainView", terrainView);
            SetSerializedValue(regionMapView, "cityViewPrefab", cityPrefab);
            SetSerializedValue(regionMapView, "villageViewPrefab", villagePrefab);
            SetSerializedValue(regionMapView, "armyViewPrefab", armyPrefab);
            regionMapRoot.SetActive(false);

            var controllerObject = new GameObject("MapLayerController");
            var mapLayerController = controllerObject.AddComponent<MapLayerController>();
            SetSerializedValue(mapLayerController, "worldMapView", worldMapView);
            SetSerializedValue(mapLayerController, "regionMapView", regionMapView);
            return mapLayerController;
        }

        private static void CreateRegionMapOverlay(Transform canvasTransform, RegionMapView regionMapView)
        {
            GameObject panel = CreatePanel("RegionMapPanel", canvasTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, -108f), new Vector2(360f, 150f));

            Text title = CreateLegacyText("TitleText", panel.transform, "区域地图", 20, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(220f, 30f));

            Text placeholder = CreateLegacyText("PlaceholderText", panel.transform, "区域地图内容将在阶段 5 接入", 17, TextAnchor.UpperLeft);
            SetRect(placeholder.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -50f), new Vector2(240f, 82f));

            Button returnButton = CreateButton("ReturnWorldButton", panel.transform, "返回天下", new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(96f, 34f));

            SetSerializedValue(regionMapView, "uiRoot", panel);
            SetSerializedValue(regionMapView, "titleText", title);
            SetSerializedValue(regionMapView, "placeholderText", placeholder);
            SetSerializedValue(regionMapView, "returnButton", returnButton);
            panel.SetActive(false);
        }

        private static SettlementPanel CreateSettlementPanel(Transform canvasTransform)
        {
            GameObject panel = CreatePanel("SettlementPanel", canvasTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, -24f), new Vector2(310f, 300f));
            var settlementPanel = panel.AddComponent<SettlementPanel>();
            var commandDispatcher = panel.AddComponent<UICommandDispatcher>();

            Text title = CreateLegacyText("TitleText", panel.transform, "据点", 22, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(278f, 34f));

            Text body = CreateLegacyText("BodyText", panel.transform, "", 16, TextAnchor.UpperLeft);
            SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -58f), new Vector2(278f, 120f));

            Text status = CreateLegacyText("StatusText", panel.transform, "", 15, TextAnchor.UpperLeft);
            status.color = new Color(0.9f, 0.82f, 0.5f);
            SetRect(status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 60f), new Vector2(278f, 34f));

            Button buildButton = CreateButton("BuildButton", panel.transform, "建设", new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(124f, 38f));
            Button recruitButton = CreateButton("RecruitButton", panel.transform, "征兵", new Vector2(0f, 0f), new Vector2(154f, 18f), new Vector2(124f, 38f));
            Button attackButton = CreateButton("AttackButton", panel.transform, "进攻", new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(124f, 38f));
            Button diplomacyButton = CreateButton("DiplomacyButton", panel.transform, "外交", new Vector2(0f, 0f), new Vector2(154f, 18f), new Vector2(124f, 38f));
            attackButton.gameObject.SetActive(false);
            diplomacyButton.gameObject.SetActive(false);

            SetSerializedValue(settlementPanel, "titleText", title);
            SetSerializedValue(settlementPanel, "bodyText", body);
            SetSerializedValue(settlementPanel, "statusText", status);
            SetSerializedValue(settlementPanel, "buildButton", buildButton);
            SetSerializedValue(settlementPanel, "recruitButton", recruitButton);
            SetSerializedValue(settlementPanel, "attackButton", attackButton);
            SetSerializedValue(settlementPanel, "diplomacyButton", diplomacyButton);
            SetSerializedValue(settlementPanel, "commandDispatcher", commandDispatcher);
            panel.SetActive(false);
            return settlementPanel;
        }

        private static SelectionManager CreateInputSystem(Camera raycastCamera, CameraManager cameraManager, int regionLayer, int cityLayer, int villageLayer, int armyLayer, int terrainLayer)
        {
            var inputRoot = new GameObject("InputSystemRoot");
            var selectionManager = inputRoot.AddComponent<SelectionManager>();
            var mouseInput = inputRoot.AddComponent<MouseInputAdapter>();
            var touchInput = inputRoot.AddComponent<TouchInputAdapter>();
            var inputManager = inputRoot.AddComponent<InputManager>();

            SetSerializedValue(inputManager, "raycastCamera", raycastCamera);
            SetSerializedValue(inputManager, "selectionManager", selectionManager);
            SetSerializedValue(inputManager, "cameraManager", cameraManager);
            SetSerializedValue(inputManager, "mouseInput", mouseInput);
            SetSerializedValue(inputManager, "touchInput", touchInput);
            SetSerializedValue(inputManager, "armyLayer", 1 << armyLayer);
            SetSerializedValue(inputManager, "cityLayer", 1 << cityLayer);
            SetSerializedValue(inputManager, "villageLayer", 1 << villageLayer);
            SetSerializedValue(inputManager, "terrainLayer", (1 << regionLayer) | (1 << terrainLayer));
            return selectionManager;
        }

        private static RegionView CreateRegionPrefab(int regionLayer)
        {
            AssetDatabase.DeleteAsset(RegionPrefabPath);

            GameObject regionObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            regionObject.name = "Region";
            regionObject.layer = regionLayer;
            regionObject.transform.localScale = new Vector3(1.0f, 0.58f, 0.08f);
            var renderer = regionObject.GetComponent<Renderer>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(regionObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.12f);
            labelObject.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
            labelObject.layer = regionLayer;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "REGION";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.1f;
            label.fontSize = 48;
            label.color = Color.white;

            var regionView = regionObject.AddComponent<RegionView>();
            SetSerializedValue(regionView, "targetRenderer", renderer);
            SetSerializedValue(regionView, "label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(regionObject, RegionPrefabPath);
            Object.DestroyImmediate(regionObject);
            return prefab.GetComponent<RegionView>();
        }

        private static CityView CreateCityPrefab(int cityLayer)
        {
            AssetDatabase.DeleteAsset(CityPrefabPath);
            GameObject cityObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cityObject.name = "City";
            cityObject.layer = cityLayer;
            cityObject.transform.localScale = new Vector3(0.72f, 0.72f, 0.12f);
            var renderer = cityObject.GetComponent<Renderer>();

            TextMesh label = CreateWorldLabel(cityObject.transform, "CITY", new Vector3(0f, -0.78f, -0.18f), 0.52f);
            var cityView = cityObject.AddComponent<CityView>();
            SetSerializedValue(cityView, "targetRenderer", renderer);
            SetSerializedValue(cityView, "label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cityObject, CityPrefabPath);
            Object.DestroyImmediate(cityObject);
            return prefab.GetComponent<CityView>();
        }

        private static VillageView CreateVillagePrefab(int villageLayer)
        {
            AssetDatabase.DeleteAsset(VillagePrefabPath);
            GameObject villageObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villageObject.name = "Village";
            villageObject.layer = villageLayer;
            villageObject.transform.localScale = new Vector3(0.52f, 0.52f, 0.1f);
            var renderer = villageObject.GetComponent<Renderer>();

            TextMesh label = CreateWorldLabel(villageObject.transform, "VILLAGE", new Vector3(0f, -0.7f, -0.18f), 0.5f);
            var villageView = villageObject.AddComponent<VillageView>();
            SetSerializedValue(villageView, "targetRenderer", renderer);
            SetSerializedValue(villageView, "label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(villageObject, VillagePrefabPath);
            Object.DestroyImmediate(villageObject);
            return prefab.GetComponent<VillageView>();
        }

        private static ArmyView CreateArmyPrefab(int armyLayer)
        {
            AssetDatabase.DeleteAsset(ArmyPrefabPath);
            GameObject armyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            armyObject.name = "Army";
            armyObject.layer = armyLayer;
            armyObject.transform.localScale = new Vector3(0.38f, 0.38f, 0.1f);
            var renderer = armyObject.GetComponent<Renderer>();

            TextMesh label = CreateWorldLabel(armyObject.transform, "ARMY", new Vector3(0f, -0.72f, -0.18f), 0.46f);
            var armyView = armyObject.AddComponent<ArmyView>();
            SetSerializedValue(armyView, "targetRenderer", renderer);
            SetSerializedValue(armyView, "label", label);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(armyObject, ArmyPrefabPath);
            Object.DestroyImmediate(armyObject);
            return prefab.GetComponent<ArmyView>();
        }

        private static TextMesh CreateWorldLabel(Transform parent, string text, Vector3 localPosition, float localScale)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localScale = new Vector3(localScale, localScale, localScale);

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 56;
            label.color = Color.white;
            return label;
        }

        private static int EnsureUserLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.stringValue == layerName)
                    return i;
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue))
                    continue;

                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }

            Debug.LogWarning("[SpringAutumn] No empty user layer slot for " + layerName + ". Falling back to Default layer.");
            return 0;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = obj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            return obj;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset cjkFont = GetOrCreateCjkFontAsset();
            if (cjkFont != null)
                tmp.font = cjkFont;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = new Color(0.95f, 0.92f, 0.84f);
            tmp.extraPadding = true;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontStyle = FontStyles.Normal;
            tmp.fontWeight = FontWeight.Regular;
            return tmp;
        }

        private static Text CreateLegacyText(string name, Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var uiText = obj.AddComponent<Text>();
            Font font = GetOrCreateLocalFontAsset();
            if (font != null)
                uiText.font = font;
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = new Color(0.95f, 0.92f, 0.84f);
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            return uiText;
        }

        private static TMP_FontAsset GetOrCreateCjkFontAsset()
        {
            if (_cjkFontAsset != null)
                return _cjkFontAsset;

            _cjkFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkFontAssetPath);
            if (_cjkFontAsset != null)
                return _cjkFontAsset;

            Font sourceFont = GetOrCreateLocalFontAsset();
            if (sourceFont == null)
            {
                Debug.LogError("[SpringAutumn] Missing CJK font. Put a Chinese .ttf file at " + LocalCjkFontPath + " and rebuild scenes.");
                return null;
            }

            _cjkFontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                72,
                8,
                GlyphRenderMode.SDFAA_HINTED,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (_cjkFontAsset == null)
            {
                Debug.LogError("[SpringAutumn] Failed to create TMP font asset from " + AssetDatabase.GetAssetPath(sourceFont));
                return null;
            }

            _cjkFontAsset.name = "SpringAutumn CJK SDF";
            _cjkFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            ResetFontMaterial(_cjkFontAsset);
            AssetDatabase.CreateAsset(_cjkFontAsset, CjkFontAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SpringAutumn] Created TMP CJK font asset: " + CjkFontAssetPath);
            return _cjkFontAsset;
        }

        private static void ResetFontMaterial(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null || fontAsset.material == null)
                return;

            Material material = fontAsset.material;
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
        }

        private static Font GetOrCreateLocalFontAsset()
        {
            Font existing = AssetDatabase.LoadAssetAtPath<Font>(LocalCjkFontPath);
            if (existing != null)
                return existing;

            string systemFont = FindLocalDevelopmentFontFile();
            if (string.IsNullOrEmpty(systemFont))
                return null;

            File.Copy(systemFont, LocalCjkFontPath, true);
            AssetDatabase.ImportAsset(LocalCjkFontPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(LocalCjkFontPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            Debug.Log("[SpringAutumn] Copied local development CJK font: " + systemFont);
            return AssetDatabase.LoadAssetAtPath<Font>(LocalCjkFontPath);
        }

        private static void RebuildGeneratedFontAsset()
        {
            _cjkFontAsset = null;
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CjkFontAssetPath) != null)
                AssetDatabase.DeleteAsset(CjkFontAssetPath);
        }

        private static string FindLocalDevelopmentFontFile()
        {
            string windowsFontDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "Fonts");
            string[] candidates =
            {
                "simhei.ttf",
                "msyh.ttc",
                "simsun.ttc",
                "Deng.ttf"
            };

            foreach (string candidate in candidates)
            {
                string path = Path.Combine(windowsFontDir, candidate);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject obj = CreatePanel(name, parent, anchor, anchor, new Vector2(1f, 1f), anchoredPosition, size);
            var button = obj.AddComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();

            Text text = CreateLegacyText("Label", obj.transform, label, 18, TextAnchor.MiddleCenter);
            text.color = Color.white;
            return button;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button == null)
                return;

            if (button.targetGraphic != null)
                button.targetGraphic.color = color;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = new Color(color.r + 0.08f, color.g + 0.08f, color.b + 0.08f, color.a);
            button.colors = colors;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetSerializedValue(UnityEngine.Object target, string propertyName, object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new MissingReferenceException($"{target.name}.{propertyName}");

            switch (value)
            {
                case UnityEngine.Object objectReference:
                    property.objectReferenceValue = objectReference;
                    break;
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
                case bool boolValue:
                    property.boolValue = boolValue;
                    break;
                case int intValue:
                    property.intValue = intValue;
                    break;
                case Vector2 vector2Value:
                    property.vector2Value = vector2Value;
                    break;
                default:
                    throw new System.NotSupportedException($"Unsupported serialized value: {value}");
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
