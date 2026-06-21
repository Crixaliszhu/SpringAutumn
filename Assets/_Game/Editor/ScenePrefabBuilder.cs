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

        [MenuItem("SpringAutumn/Build Scenes/Stage 1-3 Bootstrap HUD WorldMap")]
        public static void BuildStage1And2()
        {
            EnsureFolders();
            RebuildGeneratedFontAsset();
            int regionLayer = EnsureUserLayer("Region");
            RegionView regionPrefab = CreateRegionPrefab(regionLayer);

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
            SetSerializedValue(binding, "startNewGameOnAwake", true);

            CreateEventSystem();

            Camera worldCamera = CreateGameCamera();
            MapLayerController mapLayerController = CreateWorldMap(regionPrefab);
            SelectionManager selectionManager = CreateInputSystem(worldCamera, regionLayer);

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

            GameObject menuPanel = CreatePanel("MenuPanel", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-300f, -220f), new Vector2(260f, 160f));
            CreateText("MenuPanelText", menuPanel.transform, "菜单占位\n后续接入主菜单/存档", 20, TextAlignmentOptions.Center);
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

            GameObject messagePanel = CreatePanel("MessagePanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(320f, -190f));
            var messageSystem = messagePanel.AddComponent<MessageSystem>();
            TMP_Text messageText = CreateText("MessageText", messagePanel.transform, "", 18, TextAlignmentOptions.TopLeft);
            SetRect(messageText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(-20f, -20f));
            SetSerializedValue(messageSystem, "messageText", messageText);

            TMP_Text statusText = CreateText("StatusText", canvas.transform, "Initializing...", 18, TextAlignmentOptions.Right);
            SetRect(statusText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(520f, 32f));

            SetSerializedValue(binding, "hudView", hud);
            SetSerializedValue(binding, "messageSystem", messageSystem);
            SetSerializedValue(binding, "mapLayerController", mapLayerController);
            SetSerializedValue(binding, "selectionManager", selectionManager);
            SetSerializedValue(binding, "statusText", statusText);
            canvas.worldCamera = worldCamera;

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpringAutumn] Stage 1-3 scene generated: " + BootstrapScenePath);
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(SceneDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MapPrefabDir);
            Directory.CreateDirectory(FontDir);
        }

        private static Canvas CreateCanvas(Camera worldCamera)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.pixelPerfect = true;
            canvas.worldCamera = worldCamera;
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

        private static Camera CreateGameCamera()
        {
            var cameraObject = new GameObject("WorldCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.08f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            return camera;
        }

        private static MapLayerController CreateWorldMap(RegionView regionPrefab)
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
            var regionMapView = regionMapRoot.AddComponent<RegionMapView>();
            regionMapRoot.SetActive(false);

            var controllerObject = new GameObject("MapLayerController");
            var mapLayerController = controllerObject.AddComponent<MapLayerController>();
            SetSerializedValue(mapLayerController, "worldMapView", worldMapView);
            SetSerializedValue(mapLayerController, "regionMapView", regionMapView);
            return mapLayerController;
        }

        private static SelectionManager CreateInputSystem(Camera raycastCamera, int regionLayer)
        {
            var inputRoot = new GameObject("InputSystemRoot");
            var selectionManager = inputRoot.AddComponent<SelectionManager>();
            var mouseInput = inputRoot.AddComponent<MouseInputAdapter>();
            var touchInput = inputRoot.AddComponent<TouchInputAdapter>();
            var inputManager = inputRoot.AddComponent<InputManager>();

            SetSerializedValue(inputManager, "raycastCamera", raycastCamera);
            SetSerializedValue(inputManager, "selectionManager", selectionManager);
            SetSerializedValue(inputManager, "mouseInput", mouseInput);
            SetSerializedValue(inputManager, "touchInput", touchInput);
            SetSerializedValue(inputManager, "terrainLayer", 1 << regionLayer);
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
            tmp.fontStyle = FontStyles.Normal;
            tmp.fontWeight = FontWeight.Regular;
            return tmp;
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

            TMP_Text text = CreateText("Label", obj.transform, label, 18, TextAlignmentOptions.Center);
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
                default:
                    throw new System.NotSupportedException($"Unsupported serialized value: {value}");
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
