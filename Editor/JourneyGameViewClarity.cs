using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CiCongNiSheng.EditorTools
{
    [InitializeOnLoad]
    public static class JourneyGameViewClarity
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static JourneyGameViewClarity()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && !Application.isBatchMode &&
                    EditorSceneManager.GetActiveScene().path == JourneyEditorTools.ScenePath)
                    EditorApplication.delayCall += Configure;
            };
        }

        [MenuItem("瓷从泥生/整合工坊/5 · 清晰 Game 视图（1x）", false, 4)]
        public static void Configure()
        {
            if (Application.isBatchMode) return;
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type == null) return;
            var view = EditorWindow.GetWindow(type);
            try
            {
                SetIfPresent(type, view, "lowResolutionForAspectRatios", false);
                // Free Aspect renders at the available viewport's native pixel count.
                SetIfPresent(type, view, "selectedSizeIndex", 0);
                var zoom = type.GetField("m_ZoomArea", Flags)?.GetValue(view);
                bool scaleSet = zoom != null && SetIfPresent(zoom.GetType(), zoom, "scale", Vector2.one);
                view.Repaint();
                Debug.Log("[工坊画面] 已关闭低分辨率预览，Game 视图按窗口像素渲染。" +
                    (scaleSet ? " Scale 已设为 1x。" : " 如画面被放大，请将 Game 顶部 Scale 调为 1x。"));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[工坊画面] 请在 Game 顶部手动设置 Scale 1x，并关闭 Low Resolution Aspect Ratios。" + ex.GetType().Name);
            }
        }

        static bool SetIfPresent(Type type, object target, string name, object value)
        {
            var property = type.GetProperty(name, Flags);
            if (property == null || !property.CanWrite) return false;
            property.SetValue(target, value, null);
            return true;
        }
    }
}