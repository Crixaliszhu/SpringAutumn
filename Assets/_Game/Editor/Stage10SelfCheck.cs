using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpringAutumn.Bootstrap;
using SpringAutumn.Commands;
using SpringAutumn.Config;
using SpringAutumn.Core.Events;
using SpringAutumn.Presentation.Bootstrap;
using SpringAutumn.Presentation.Camera;
using SpringAutumn.Presentation.Config;
using SpringAutumn.Presentation.Input;
using SpringAutumn.Presentation.Map;
using SpringAutumn.Presentation.UI;
using SpringAutumn.Save;
using SpringAutumn.Runtime;

namespace SpringAutumn.EditorTools
{
    public static class Stage10SelfCheck
    {
        private const string BootstrapScenePath = "Assets/_Game/Scenes/BootstrapScene.scene";
        private const string RegionPrefabPath = "Assets/_Game/Prefabs/Map/Region.prefab";
        private const string CityPrefabPath = "Assets/_Game/Prefabs/Map/City.prefab";
        private const string VillagePrefabPath = "Assets/_Game/Prefabs/Map/Village.prefab";
        private const string ArmyPrefabPath = "Assets/_Game/Prefabs/Map/Army.prefab";

        [MenuItem("SpringAutumn/Validation/Stage 10 WeChat MiniGame Self Check")]
        public static void Run()
        {
            var report = new Report();

            Scene scene = EnsureBootstrapSceneLoaded(report);
            CheckBuildSettings(report);
            CheckSceneAssembly(report, scene);
            CheckCoreLoop(report);

            string text = report.ToString();
            if (report.Failures > 0)
                Debug.LogError(text);
            else if (report.Warnings > 0)
                Debug.LogWarning(text);
            else
                Debug.Log(text);
        }

        private static Scene EnsureBootstrapSceneLoaded(Report report)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == BootstrapScenePath)
                return active;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                report.Fail("BootstrapScene", "用户取消保存当前场景，未切换到 BootstrapScene");
                return active;
            }

            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            report.Pass("BootstrapScene", "已打开 BootstrapScene");
            return scene;
        }

        private static void CheckBuildSettings(Report report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool hasScene = scenes.Length > 0 && scenes[0].path == BootstrapScenePath && scenes[0].enabled;
            report.Check(hasScene, "Build Settings", "BootstrapScene 是第 1 个启用场景", "BootstrapScene 未设置为第 1 个启用场景");

            report.Check(AssetDatabase.LoadAssetAtPath<GameObject>(RegionPrefabPath) != null, "Prefab", "Region.prefab 存在", "Region.prefab 缺失");
            report.Check(AssetDatabase.LoadAssetAtPath<GameObject>(CityPrefabPath) != null, "Prefab", "City.prefab 存在", "City.prefab 缺失");
            report.Check(AssetDatabase.LoadAssetAtPath<GameObject>(VillagePrefabPath) != null, "Prefab", "Village.prefab 存在", "Village.prefab 缺失");
            report.Check(AssetDatabase.LoadAssetAtPath<GameObject>(ArmyPrefabPath) != null, "Prefab", "Army.prefab 存在", "Army.prefab 缺失");
        }

        private static void CheckSceneAssembly(Report report, Scene scene)
        {
            if (!scene.IsValid())
            {
                report.Fail("Scene", "当前场景无效，跳过场景装配检查");
                return;
            }

            var scalers = FindSceneComponents<CanvasScaler>(scene);
            CanvasScaler scaler = scalers.Count > 0 ? scalers[0] : null;
            report.Check(scaler != null, "Canvas", "CanvasScaler 已装配", "CanvasScaler 缺失");
            if (scaler != null)
            {
                report.Check(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                    "Canvas", "CanvasScaler 使用 ScaleWithScreenSize", "CanvasScaler 未使用 ScaleWithScreenSize");
                report.Check(scaler.referenceResolution == new Vector2(1280f, 720f),
                    "Canvas", "参考分辨率为 1280x720", "参考分辨率不是 1280x720");
                report.Check(Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f),
                    "Canvas", "宽高匹配权重为 0.5", "宽高匹配权重不是 0.5");
            }

            report.Check(FindSceneComponents<EventSystem>(scene).Count > 0, "Input", "EventSystem 已装配", "EventSystem 缺失");
            report.Check(FindSceneComponents<StandaloneInputModule>(scene).Count > 0, "Input", "StandaloneInputModule 已装配", "StandaloneInputModule 缺失");
            report.Check(FindSceneComponents<InputManager>(scene).Count > 0, "Input", "InputManager 已装配", "InputManager 缺失");
            report.Check(FindSceneComponents<TouchInputAdapter>(scene).Count > 0, "Input", "TouchInputAdapter 已装配", "TouchInputAdapter 缺失");
            report.Check(FindSceneComponents<CameraInputAdapter>(scene).Count > 0, "Input", "CameraInputAdapter 已装配", "CameraInputAdapter 缺失");

            report.Check(FindSceneComponents<WorldCameraController>(scene).Count == 1, "Camera", "WorldCamera 已装配", "WorldCamera 数量异常");
            report.Check(FindSceneComponents<RegionCameraController>(scene).Count == 1, "Camera", "RegionCamera 已装配", "RegionCamera 数量异常");
            report.Check(FindSceneComponents<MainMenuView>(scene).Count > 0, "UI", "主菜单已装配", "主菜单缺失");
            report.Check(FindSceneComponents<HudView>(scene).Count > 0, "UI", "HUD 已装配", "HUD 缺失");
            report.Check(FindSceneComponents<RegionBriefPanel>(scene).Count > 0, "UI", "区域简报已装配", "区域简报缺失");
            report.Check(FindSceneComponents<SettlementPanel>(scene).Count > 0, "UI", "据点面板已装配", "据点面板缺失");

            int objectCount = CountSceneObjects(scene);
            int rendererCount = FindSceneComponents<Renderer>(scene).Count;
            int colliderCount = FindSceneComponents<Collider>(scene).Count;
            report.Pass("Performance", $"场景对象 {objectCount} / Renderer {rendererCount} / Collider {colliderCount}");
            if (objectCount > 900)
                report.Warn("Performance", "场景对象数超过 900，微信小游戏导出前建议继续压测");
            if (rendererCount > 180)
                report.Warn("Performance", "Renderer 数超过 180，移动端建议关注 draw calls");
            if (colliderCount > 160)
                report.Warn("Performance", "Collider 数超过 160，移动端建议关注点击射线成本");
        }

        private static void CheckCoreLoop(Report report)
        {
            string saveDir = Path.Combine(Path.GetTempPath(), "SpringAutumnStage10SelfCheck_" + Guid.NewGuid().ToString("N"));

            try
            {
                string configDir = Path.Combine(Application.dataPath, "_Game/Config");
                ConfigDatabase config = new ConfigLoader().Load(JsonConfigSource.FromDirectory(configDir));
                var saveManager = new SaveManager(config, new FileSaveStorage(saveDir));
                var app = new GameApplication(config, saveManager);

                app.NewGame();
                WorldRuntime world = app.World;
                report.Check(world.Nations.Count == 7 && world.Regions.Count == 24 && world.Settlements.Count == 89,
                    "Core Loop", "新游戏世界规模正确", "新游戏世界规模异常");

                var build = new BuildCommand("PLAYER", "V_PLAYER_001", "FARM", config);
                report.Check(build.Validate(world), "Core Loop", "建设命令可提交", "建设命令无法提交");
                app.Engine.EnqueueCommand(build);
                app.Engine.NextMonth();
                app.Engine.NextMonth();
                report.Check(HasBuilding(world, "V_PLAYER_001", "FARM"), "Core Loop", "建设完成生效", "建设未完成");

                var recruit = new RecruitCommand("PLAYER", "V_PLAYER_001", 10, config);
                report.Check(recruit.Validate(world), "Core Loop", "征兵命令可提交", "征兵命令无法提交");
                app.Engine.EnqueueCommand(recruit);
                app.Engine.NextMonth();
                app.Engine.NextMonth();
                report.Check(world.Settlements.Get("V_PLAYER_001").Garrison >= 20,
                    "Core Loop", "征兵完成生效", "征兵未完成");

                bool captured = false;
                app.Events.Subscribe<RegionCaptured>(evt =>
                {
                    if (evt.RegionId == "NEU_R01" && evt.NewOwnerId == "PLAYER")
                        captured = true;
                });

                SettlementState source = world.Settlements.Get("V_PLAYER_001");
                source.Grain += 100000;
                source.Money += 1000;
                source.Garrison = 70;

                var move = new MoveArmyCommand("PLAYER", "V_PLAYER_001", "NEU_R01", "V_NEU_001", 31, config);
                report.Check(move.Validate(world), "Core Loop", "出兵命令可提交", "出兵命令无法提交");
                app.Engine.EnqueueCommand(move);
                app.Engine.NextMonth();

                report.Check(captured && world.Regions.Get("NEU_R01").OwnerId == "PLAYER",
                    "Core Loop", "移动军队并攻占中立区域成功", "移动/战斗/占领流程未完成");

                report.Check(app.Save(3), "Save", "临时槽位保存成功", "临时槽位保存失败");
                report.Check(app.LoadGame(3) != null, "Save", "临时槽位读取成功", "临时槽位读取失败");
            }
            catch (Exception ex)
            {
                report.Fail("Core Loop", ex.Message);
            }
            finally
            {
                TryDeleteDirectory(saveDir);
            }
        }

        private static bool HasBuilding(WorldRuntime world, string settlementId, string buildingId)
        {
            if (!world.Settlements.TryGet(settlementId, out var settlement))
                return false;

            foreach (var building in settlement.Buildings)
            {
                if (building.BuildingId == buildingId)
                    return true;
            }

            return false;
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            var results = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results;
        }

        private static int CountSceneObjects(Scene scene)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                CountRecursive(root.transform, ref count);
            return count;
        }

        private static void CountRecursive(Transform transform, ref int count)
        {
            count++;
            for (int i = 0; i < transform.childCount; i++)
                CountRecursive(transform.GetChild(i), ref count);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
                // Temporary self-check saves are harmless if cleanup is blocked by the OS.
            }
        }

        private class Report
        {
            private readonly StringBuilder _builder = new StringBuilder();

            public int Failures { get; private set; }
            public int Warnings { get; private set; }

            public void Check(bool condition, string section, string pass, string fail)
            {
                if (condition)
                    Pass(section, pass);
                else
                    Fail(section, fail);
            }

            public void Pass(string section, string message)
            {
                _builder.AppendLine($"[PASS] {section}: {message}");
            }

            public void Warn(string section, string message)
            {
                Warnings++;
                _builder.AppendLine($"[WARN] {section}: {message}");
            }

            public void Fail(string section, string message)
            {
                Failures++;
                _builder.AppendLine($"[FAIL] {section}: {message}");
            }

            public override string ToString()
            {
                return $"[SpringAutumn] Stage 10 Self Check: {Failures} failures, {Warnings} warnings\n" + _builder;
            }
        }
    }
}
