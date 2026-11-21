using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using CiCongNiSheng.Journey;

namespace CiCongNiSheng.EditorTools
{
    [InitializeOnLoad]
    public static class JourneyEditorTools
    {
        public const string ScenePath = "Assets/Scenes/PorcelainJourney.unity";
        const string RequestPath = "Assets/Docs/JourneyValidation/request.txt";
        const string ReportPath = "Assets/Docs/JourneyValidation/editor-report.txt";
        static double nextCheck;
        static JourneyPlaythrough validation;
        static int logErrors;
        static readonly List<string> errors = new List<string>();

        static JourneyEditorTools()
        {
            EditorApplication.update += Update;
            Application.logMessageReceived += Log;
            CompilationPipeline.assemblyCompilationFinished += (path, messages) =>
            {
                foreach (var message in messages)
                    if (message.type == CompilerMessageType.Error) errors.Add(message.message);
            };
        }

        static void Log(string condition, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            logErrors++;
            errors.Add(condition + "\n" + stack);
        }

        static void Update()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isCompiling &&
                SessionState.GetBool("PorcelainJourney.ValidateOnPlay", false))
            {
                SessionState.SetBool("PorcelainJourney.ValidateOnPlay", false);
                validation = new JourneyPlaythrough(Write, () => errors.ToArray());
            }
            if (validation != null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                try { if (validation.Tick()) validation = null; }
                catch (Exception ex) { Write("FAIL: " + ex); validation = null; if (Application.isBatchMode) EditorApplication.Exit(1); }
            }
            if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.75;
            if (!File.Exists(RequestPath)) return;
            string request = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            try
            {
                if (request == "setup") Install();
                else if (request == "play") { Open(); EditorApplication.isPlaying = true; }
                else if (request == "stop") EditorApplication.isPlaying = false;
                else if (request == "probe") Probe();
                else if (request == "test") BeginValidation();
                else if (request == "preview") { var w = JourneyWorkshop.Instance; if (w != null && w.IsReady) { w.Respawn(); w.HUD.ShowBook(true); } }
                else if (request.StartsWith("capture:")) Capture(request.Substring(8));
            }
            catch (Exception ex) { Write("FAIL: " + ex); }
        }

        [MenuItem("瓷从泥生/整合工坊/1 · 打开四工序场景", false, 0)]
        public static void Open()
        {
            if (EditorApplication.isPlaying) return;
            if (!File.Exists(ScenePath)) { Install(); return; }
            if (EditorSceneManager.GetActiveScene().path == ScenePath) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("瓷从泥生/整合工坊/2 · 运行第一人称体验", false, 1)]
        public static void Play()
        {
            Open();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("瓷从泥生/整合工坊/3 · 生成或修复整合场景", false, 2)]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先退出播放模式。");
            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.CopyAsset("Assets/Cishan/Scenes/06_ChongShi.unity", ScenePath))
                    throw new InvalidOperationException("无法复制原始舂石场景。");
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (UnityEngine.Object.FindObjectOfType<JourneyWorkshop>() == null)
                new GameObject("瓷从泥生 · 四工序整合入口").AddComponent<JourneyWorkshop>();
            // The runtime builder activates only on Play; source scenes stay independently usable.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) buildScenes.Add(existing);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Write("SETUP PASS\nScene: " + ScenePath + "\nUnity: " + Application.unityVersion);
        }

        [MenuItem("瓷从泥生/整合工坊/4 · 验证流程", false, 3)]
        public static void BeginValidation()
        {
            Directory.CreateDirectory("Assets/Docs/JourneyValidation");
            errors.Clear();
            logErrors = 0;
            ValidateFlow();
            SessionState.SetBool("PorcelainJourney.ValidateOnPlay", true);
            if (!EditorApplication.isPlaying) { Open(); EditorApplication.isPlaying = true; }
            EditorApplication.isPaused = false;
            Write(JourneyValidationCases.RunFlow() + "\nWaiting for Play Mode...");
        }

        [InitializeOnEnterPlayMode]
        static void EnteredPlay(EnterPlayModeOptions options)
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("PorcelainJourney.ValidateOnPlay", false)) return;
                SessionState.SetBool("PorcelainJourney.ValidateOnPlay", false);
                validation = new JourneyPlaythrough(Write, () => errors.ToArray());
            };
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        static void ValidateFlow()
        {
            JourneyValidationCases.RunFlow();
        }

        public static void Probe()
        {
            var workshop = UnityEngine.Object.FindObjectOfType<JourneyWorkshop>();
            string result = "PROBE\nPlaying: " + EditorApplication.isPlaying + "\nErrors: " + logErrors + "\n";
            if (workshop != null && workshop.IsReady)
            {
                result += "Ready: true\nStep: " + workshop.Flow.Step + "\nCameras: " + Camera.allCamerasCount + "\n";
                result += "Player: " + workshop.Player.transform.position + "\n";
                foreach (var target in workshop.Targets)
                    result += target.action + ": " + target.transform.position + " active=" + target.gameObject.activeSelf + "\n";
            }
            result += string.Join("\n", errors);
            Write(result);
        }

        static void Capture(string label)
        {
            if (!EditorApplication.isPlaying) return;
            var w = JourneyWorkshop.Instance;
            if (w == null || !w.IsReady) return;
            string safe = label.Replace("/", "").Replace("\\", "").Replace("..", "");
            ScreenCapture.CaptureScreenshot("Assets/Docs/JourneyValidation/" + safe + ".png");
        }

        static void Write(string report)
        {
            Directory.CreateDirectory("Assets/Docs/JourneyValidation");
            File.WriteAllText(ReportPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + report);
            if (report.StartsWith("PLAYTHROUGH ")) File.WriteAllText("Assets/Docs/JourneyValidation/playthrough-report.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + report);
        }
    }
}
