using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiCongNiSheng.EditorTools
{
    /// <summary>
    /// 场景入口。场景本身只放一个装配器，所有几何与材质在进入 Play 时生成，
    /// 因此这里提供一键打开并运行，避免"空场景"造成误解。
    /// </summary>
    public static class WorkshopEditorTools
    {
        const string ScenePath = "Assets/Scenes/SieveWash.unity";

        [MenuItem("瓷从泥生/1 · 打开筛洗场景并运行", false, 1)]
        static void OpenAndPlay()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("[瓷从泥生] 打不开场景：" + ScenePath);
                return;
            }
            EditorApplication.isPlaying = true;
        }

        [MenuItem("瓷从泥生/2 · 打开筛洗场景（不运行）", false, 2)]
        static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("瓷从泥生/3 · 将筛洗场景加入 Build Settings", false, 3)]
        static void AddToBuildSettings()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool exists = false;
            foreach (var s in list)
            {
                if (s.path == ScenePath) { exists = true; break; }
            }
            if (!exists)
            {
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
                Debug.Log("[瓷从泥生] 已把 " + ScenePath + " 加入 Build Settings。");
            }
            else
            {
                Debug.Log("[瓷从泥生] 场景已在 Build Settings 中。");
            }
        }

        [MenuItem("瓷从泥生/4 · 检查资源与设置", false, 4)]
        static void CheckSetup()
        {
            string[] shaders =
            {
                "Assets/Resources/Shaders/SieveCloth.shader",
                "Assets/Resources/Shaders/WaterSurface.shader",
                "Assets/Resources/Shaders/UnlitDecal.shader",
                "Assets/Resources/Shaders/StyleGrade.shader"
            };
            foreach (var path in shaders)
            {
                var sh = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Debug.Log(sh != null
                    ? "[瓷从泥生] 着色器正常：" + sh.name
                    : "[瓷从泥生] 缺少着色器：" + path);
            }

            Debug.Log("[瓷从泥生] 颜色空间：" + (PlayerSettings.colorSpace == ColorSpace.Linear ? "Linear" : "Gamma")
                      + "（规范按 Linear 调校）");
            Debug.Log("[瓷从泥生] 场景是否在 Build Settings：" +
                      (System.Array.Exists(EditorBuildSettings.scenes, s => s.path == ScenePath) ? "是" : "否"));
        }
    }
}
