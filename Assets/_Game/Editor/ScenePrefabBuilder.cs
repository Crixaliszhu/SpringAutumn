using System.Collections.Generic;
using System.IO;
using System.Text;
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

            // 平面均匀环境光：场景未放置任何灯光，默认天空盒环境光会让 3D 立方体出现明暗渐变，
            // 在地图上表现为中间的亮带/分界。改为 Flat 白色环境光，使所有物体均匀受光、呈扁平 2D 观感。
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;

            var launcherObject = new GameObject("GameLauncher");
            var launcher = launcherObject.AddComponent<GameLauncher>();
            SetSerializedValue(launcher, "configResourceDir", "Config");
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

            Text dateText = CreateLegacyText("DateText", hudRoot.transform, "第1年1月", 22, TextAnchor.MiddleLeft);
            SetRect(dateText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(220f, 32f));

            Text resourceText = CreateLegacyText("ResourceText", hudRoot.transform, "粮 0  钱 0  人口 0  兵 0  Region 0", 20, TextAnchor.MiddleLeft);
            SetRect(resourceText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(240f, -14f), new Vector2(-640f, 30f));

            // 按钮组整体左移，避免最右侧的"菜单"被微信小游戏右上角胶囊按钮（••• 与圆形）遮挡。
            Button pauseButton = CreateButton("PauseButton", hudRoot.transform, "暂停", new Vector2(1f, 1f), new Vector2(-550f, -14f), new Vector2(72f, 32f));
            Button speed1Button = CreateButton("Speed1Button", hudRoot.transform, "1x", new Vector2(1f, 1f), new Vector2(-466f, -14f), new Vector2(54f, 32f));
            Button speed2Button = CreateButton("Speed2Button", hudRoot.transform, "2x", new Vector2(1f, 1f), new Vector2(-402f, -14f), new Vector2(54f, 32f));
            Button speed3Button = CreateButton("Speed3Button", hudRoot.transform, "3x", new Vector2(1f, 1f), new Vector2(-338f, -14f), new Vector2(54f, 32f));
            Button menuButton = CreateButton("MenuButton", hudRoot.transform, "菜单", new Vector2(1f, 1f), new Vector2(-256f, -14f), new Vector2(72f, 32f));
            SetButtonColor(speed1Button, new Color(0.55f, 0.42f, 0.16f, 0.95f));

            GameObject menuPanel = CreatePanel("MenuPanel", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -250f), new Vector2(260f, 220f));
            Text menuTitle = CreateLegacyText("MenuPanelText", menuPanel.transform, "游戏菜单", 20, TextAnchor.MiddleCenter);
            SetRect(menuTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -14f), new Vector2(0f, 30f));
            Button saveButton = CreateButton("SaveButton", menuPanel.transform, "保存", new Vector2(0f, 1f), new Vector2(26f, -58f), new Vector2(96f, 34f));
            SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -58f), new Vector2(96f, 34f));
            Button loadButton = CreateButton("LoadButton", menuPanel.transform, "读档", new Vector2(0f, 1f), new Vector2(138f, -58f), new Vector2(96f, 34f));
            SetRect(loadButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(138f, -58f), new Vector2(96f, 34f));
            Button closeMenuButton = CreateButton("CloseMenuButton", menuPanel.transform, "关闭", new Vector2(0f, 1f), new Vector2(82f, -104f), new Vector2(96f, 34f));
            SetRect(closeMenuButton.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -104f), new Vector2(96f, 34f));
            Text menuStatusText = CreateLegacyText("MenuStatusText", menuPanel.transform, "槽位 1", 15, TextAnchor.MiddleCenter);
            SetRect(menuStatusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(0f, 28f));
            menuPanel.SetActive(false);

            GameObject pausePanel = CreatePanel("PausePanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 200f));
            Text pauseTitle = CreateLegacyText("PauseTitle", pausePanel.transform, "游戏已暂停", 26, TextAnchor.MiddleCenter);
            SetCenteredRect(pauseTitle.rectTransform, new Vector2(0f, 38f), new Vector2(260f, 48f));
            Button resumeButton = CreateButton("ResumeButton", pausePanel.transform, "继续游戏", new Vector2(0.5f, 0.5f), new Vector2(86f, -56f), new Vector2(160f, 44f));
            SetCenteredRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0f, -48f), new Vector2(160f, 44f));
            pausePanel.SetActive(false);

            SetSerializedValue(hud, "legacyDateText", dateText);
            SetSerializedValue(hud, "legacyResourceText", resourceText);
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
            SetSerializedValue(hud, "legacyMenuStatusText", menuStatusText);
            SetSerializedValue(hud, "saveSlot", GameApplication.AutoSaveSlot);

            GameObject messagePanel = CreatePanel("MessagePanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(320f, -190f));
            var messageSystem = messagePanel.AddComponent<MessageSystem>();
            Text messageText = CreateLegacyText("MessageText", messagePanel.transform, "", 18, TextAnchor.UpperLeft);
            SetRect(messageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-20f, -20f));
            SetSerializedValue(messageSystem, "legacyMessageText", messageText);

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

            Text statusText = CreateLegacyText("StatusText", canvas.transform, "Initializing...", 18, TextAnchor.MiddleRight);
            SetRect(statusText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(520f, 32f));

            SetSerializedValue(binding, "hudView", hud);
            SetSerializedValue(binding, "messageSystem", messageSystem);
            SetSerializedValue(binding, "regionBriefPanel", regionBriefPanel);
            SetSerializedValue(binding, "settlementPanel", settlementPanel);
            SetSerializedValue(binding, "mapLayerController", mapLayerController);
            SetSerializedValue(binding, "selectionManager", selectionManager);
            SetSerializedValue(binding, "legacyStatusText", statusText);

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

            Text title = CreateLegacyText("Title", root.transform, "春秋问鼎", 46, TextAnchor.MiddleCenter);
            SetCenteredRect(title.rectTransform, new Vector2(0f, 170f), new Vector2(420f, 70f));

            Text subtitle = CreateLegacyText("Subtitle", root.transform, "从流民村开始，问鼎天下", 26, TextAnchor.MiddleCenter);
            SetCenteredRect(subtitle.rectTransform, new Vector2(0f, 112f), new Vector2(540f, 46f));

            Button startButton = CreateButton("StartButton", root.transform, "开始游戏", new Vector2(0.5f, 0.5f), new Vector2(90f, 46f), new Vector2(260f, 56f));
            SetCenteredRect(startButton.GetComponent<RectTransform>(), new Vector2(0f, 36f), new Vector2(260f, 56f));
            Button loadButton = CreateButton("LoadButton", root.transform, "读取槽位 1", new Vector2(0.5f, 0.5f), new Vector2(90f, -6f), new Vector2(260f, 56f));
            SetCenteredRect(loadButton.GetComponent<RectTransform>(), new Vector2(0f, -38f), new Vector2(260f, 56f));
            Button settingsButton = CreateButton("SettingsButton", root.transform, "设置", new Vector2(0.5f, 0.5f), new Vector2(90f, -58f), new Vector2(260f, 56f));
            SetCenteredRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0f, -112f), new Vector2(260f, 56f));
            Button exitButton = CreateButton("ExitButton", root.transform, "退出", new Vector2(0.5f, 0.5f), new Vector2(90f, -110f), new Vector2(260f, 56f));
            SetCenteredRect(exitButton.GetComponent<RectTransform>(), new Vector2(0f, -186f), new Vector2(260f, 56f));

            // 增大主菜单按钮文字。
            SetButtonFontSize(startButton, 24);
            SetButtonFontSize(loadButton, 24);
            SetButtonFontSize(settingsButton, 24);
            SetButtonFontSize(exitButton, 24);

            Text statusText = CreateLegacyText("StatusText", root.transform, "", 18, TextAnchor.MiddleCenter);
            SetCenteredRect(statusText.rectTransform, new Vector2(0f, -240f), new Vector2(540f, 40f));

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
            // 背景色与区域地形块同色，使地形边缘与背景融合，避免出现"分界线"。
            camera.backgroundColor = new Color(0.10f, 0.12f, 0.10f);
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
            {
                // 直接隐藏地形底板的渲染：它原是一块比背景亮的 3D 立方体，边缘形成"分界线"、
                // 表面还有光照渐变。隐藏渲染后区域地图背景与世界地图一致（纯相机背景色），
                // 不再有矩形/分界线。保留 Collider 以维持点击交互。
                terrainRenderer.enabled = false;
            }
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
            // 据点面板放到左侧、日志区（MessageSystem 固定在左上角 (12,-92) 尺寸 252x124）下方。
            // 标题/信息/状态/操作按钮均为该面板子节点，整体构成"村庄操作菜单"。
            GameObject panel = CreatePanel("SettlementPanel", canvasTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -230f), new Vector2(310f, 320f));
            var settlementPanel = panel.AddComponent<SettlementPanel>();
            var commandDispatcher = panel.AddComponent<UICommandDispatcher>();

            Text title = CreateLegacyText("TitleText", panel.transform, "据点", 22, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(278f, 34f));

            Text body = CreateLegacyText("BodyText", panel.transform, "", 16, TextAnchor.UpperLeft);
            SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -58f), new Vector2(278f, 150f));

            Text status = CreateLegacyText("StatusText", panel.transform, "", 15, TextAnchor.UpperLeft);
            status.color = new Color(0.9f, 0.82f, 0.5f);
            SetRect(status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -214f), new Vector2(278f, 30f));

            Button buildButton = CreateButton("BuildButton", panel.transform, "建设", new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(124f, 38f));
            Button recruitButton = CreateButton("RecruitButton", panel.transform, "征兵", new Vector2(0f, 0f), new Vector2(154f, 18f), new Vector2(124f, 38f));
            Button attackButton = CreateButton("AttackButton", panel.transform, "进攻", new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(124f, 38f));
            Button diplomacyButton = CreateButton("DiplomacyButton", panel.transform, "外交", new Vector2(0f, 0f), new Vector2(154f, 18f), new Vector2(124f, 38f));
            attackButton.gameObject.SetActive(false);
            diplomacyButton.gameObject.SetActive(false);

            GameObject attackConfirmPanel = CreatePanel("AttackConfirmPanel", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 160f));
            Text confirmTitle = CreateLegacyText("Title", attackConfirmPanel.transform, "确认进攻", 18, TextAnchor.MiddleLeft);
            SetRect(confirmTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(324f, 28f));
            InputField attackCountInput = CreateInputField("AttackCountInput", attackConfirmPanel.transform, "10", new Vector2(18f, 66f), new Vector2(324f, 34f));
            Button confirmAttackButton = CreateButton("ConfirmAttackButton", attackConfirmPanel.transform, "确定", new Vector2(0f, 0f), new Vector2(168f, 52f), new Vector2(140f, 36f));
            Button cancelAttackButton = CreateButton("CancelAttackButton", attackConfirmPanel.transform, "取消", new Vector2(0f, 0f), new Vector2(342f, 52f), new Vector2(140f, 36f));
            attackConfirmPanel.SetActive(false);

            SetSerializedValue(settlementPanel, "titleText", title);
            SetSerializedValue(settlementPanel, "bodyText", body);
            SetSerializedValue(settlementPanel, "statusText", status);
            SetSerializedValue(settlementPanel, "buildButton", buildButton);
            SetSerializedValue(settlementPanel, "recruitButton", recruitButton);
            SetSerializedValue(settlementPanel, "attackButton", attackButton);
            SetSerializedValue(settlementPanel, "diplomacyButton", diplomacyButton);
            SetSerializedValue(settlementPanel, "attackConfirmPanel", attackConfirmPanel);
            SetSerializedValue(settlementPanel, "attackCountInput", attackCountInput);
            SetSerializedValue(settlementPanel, "confirmAttackButton", confirmAttackButton);
            SetSerializedValue(settlementPanel, "cancelAttackButton", cancelAttackButton);
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
            // TMP 仅用默认 ASCII 字体（LiberationSans SDF）。中文不挂在 TMP 上：
            // 运行时这些 TMP 文本会被镜像成 legacy Text，由预烘焙的中文位图字体渲染，
            // 从而规避微信小游戏 WASM 环境下 TMP 动态字形生成崩溃，并减小包体。
            TMP_FontAsset primaryFont = ResolveDefaultTmpFont();
            if (primaryFont != null)
                tmp.font = primaryFont;
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

        private static TMP_FontAsset ResolveDefaultTmpFont()
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
                return defaultFont;
            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
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

        private static string CollectUsedCharacters()
        {
            var set = new HashSet<char>();

            // ASCII 可见字符（数字、字母、标点）。
            for (char c = ' '; c <= '~'; c++)
                set.Add(c);

            // 全部配置 JSON（势力/区域/据点/建筑等名称与描述）。
            string configDir = Path.Combine(Application.dataPath, "_Game", "Resources", "Config");
            if (Directory.Exists(configDir))
            {
                foreach (string file in Directory.GetFiles(configDir, "*.json"))
                    AddChars(set, File.ReadAllText(file));
            }

            // 所有源码中的中文字面量（含运行时 UI 以及 Editor 场景生成脚本里的按钮/面板文案）。
            string scriptsDir = Path.Combine(Application.dataPath, "_Game");
            if (Directory.Exists(scriptsDir))
            {
                foreach (string file in Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories))
                    AddCjkChars(set, File.ReadAllText(file));
            }

            var sb = new StringBuilder(set.Count);
            foreach (char c in set)
                sb.Append(c);
            return sb.ToString();
        }

        private static void AddChars(HashSet<char> set, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            foreach (char c in text)
            {
                if (!char.IsControl(c))
                    set.Add(c);
            }
        }

        private static void AddCjkChars(HashSet<char> set, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            foreach (char c in text)
            {
                // CJK 统一表意文字 + 常用标点，覆盖源码内的中文 UI 文案。
                if ((c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0x3000 && c <= 0x303F) ||
                    (c >= 0xFF00 && c <= 0xFFEF))
                {
                    set.Add(c);
                }
            }
        }

        private static Font GetOrCreateLocalFontAsset()
        {
            Font existing = AssetDatabase.LoadAssetAtPath<Font>(LocalCjkFontPath);
            if (existing != null)
                return existing;
            return RebakeLegacyCjkFont();
        }

        /// <summary>
        /// 生成/刷新供 legacy UGUI Text 使用的中文字体。
        /// 关键：使用「非动态 CustomSet」导入——导入时即把用到的字形烘焙进位图图集，
        /// 运行时为纯纹理采样，无需 FreeType 光栅化，因此在微信小游戏/WebGL 真机可正常显示中文；
        /// 字符集仅包含实际用到的字符，控制包体。
        /// </summary>
        private static Font RebakeLegacyCjkFont()
        {
            if (!File.Exists(LocalCjkFontPath))
            {
                string systemFont = FindLocalDevelopmentFontFile();
                if (string.IsNullOrEmpty(systemFont))
                {
                    Debug.LogError("[SpringAutumn] 缺少中文字体源文件。请放一个中文 .ttf 到 " + LocalCjkFontPath + " 后重试。");
                    return null;
                }
                File.Copy(systemFont, LocalCjkFontPath, true);
                AssetDatabase.ImportAsset(LocalCjkFontPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[SpringAutumn] 已复制本地中文字体源：" + systemFont);
            }

            var importer = AssetImporter.GetAtPath(LocalCjkFontPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                string characters = CollectUsedCharacters();
                importer.fontTextureCase = FontTextureCase.CustomSet;
                importer.customCharacters = characters;
                importer.fontSize = 44;
                importer.includeFontData = false;
                importer.SaveAndReimport();
                Debug.Log($"[SpringAutumn] 已烘焙 legacy 中文字体（非动态 CustomSet）：字符数={characters.Length}");
            }

            return AssetDatabase.LoadAssetAtPath<Font>(LocalCjkFontPath);
        }

        private static void RebuildGeneratedFontAsset()
        {
            _cjkFontAsset = null;
            // 删除历史遗留的 TMP CJK SDF 资产（已不再使用，避免占用包体）。
            if (AssetDatabase.LoadAssetAtPath<Object>(CjkFontAssetPath) != null)
                AssetDatabase.DeleteAsset(CjkFontAssetPath);
            // 按当前字符集重新烘焙 legacy 中文位图字体。
            RebakeLegacyCjkFont();
        }

        private static string FindLocalDevelopmentFontFile()
        {
            string windowsFontDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "Fonts");
            string[] candidates =
            {
                Path.Combine(windowsFontDir, "simhei.ttf"),
                Path.Combine(windowsFontDir, "msyh.ttc"),
                Path.Combine(windowsFontDir, "simsun.ttc"),
                Path.Combine(windowsFontDir, "Deng.ttf"),
                "/System/Library/Fonts/Hiragino Sans GB.ttc",
                "/System/Library/Fonts/STHeiti Medium.ttc",
                "/System/Library/Fonts/STHeiti Light.ttc",
                "/System/Library/Fonts/Supplemental/Songti.ttc",
                "/Library/Fonts/NotoSansCJK-Regular.ttc",
                "/Library/Fonts/SourceHanSansSC-Regular.otf",
                "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
                "/usr/share/fonts/truetype/wqy/wqy-microhei.ttc"
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
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

        private static void SetButtonFontSize(Button button, int fontSize)
        {
            if (button == null)
                return;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
                label.fontSize = fontSize;
        }

        private static InputField CreateInputField(string name, Transform parent, string value, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject root = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), anchoredPosition, size);
            var input = root.AddComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 4;

            Text label = CreateLegacyText("Title", root.transform, "派兵", 15, TextAnchor.MiddleLeft);
            SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(70f, 0f));

            GameObject fieldBackground = CreatePanel("Field", root.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(76f, 0f), new Vector2(size.x - 76f, size.y));
            Image fieldImage = fieldBackground.GetComponent<Image>();
            if (fieldImage != null)
                fieldImage.color = new Color(0.04f, 0.04f, 0.04f, 0.85f);

            Text text = CreateLegacyText("Text", fieldBackground.transform, value, 16, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-12f, 0f));
            Text placeholder = CreateLegacyText("Placeholder", fieldBackground.transform, "数量", 16, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.65f, 0.65f, 0.65f, 0.9f);
            SetRect(placeholder.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-12f, 0f));

            input.targetGraphic = fieldImage;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            return input;
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
