using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CiCongNiSheng.Journey;

namespace CiCongNiSheng.EditorTools
{
    public static class JourneyRenderCapture
    {
        public static void Layout(JourneyWorkshop world)
        {
            var camera = world.Camera;
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            bool oldOrtho = camera.orthographic;
            float oldSize = camera.orthographicSize;
            var canvas = world.HUD.GetComponentInChildren<Canvas>();
            bool oldCanvas = canvas.enabled;
            bool oldHands = world.Player.hands.gameObject.activeSelf;
            Renderer roof = null;
            foreach (var r in world.GetComponentsInChildren<Renderer>()) if (r.name == "屋面") { roof = r; break; }
            bool roofVisible = roof != null && roof.enabled;
            try
            {
                // Architectural cutaway for inspecting the entire ordered row.
                if (roof != null) roof.enabled = false;
                canvas.enabled = false;
                world.Player.hands.gameObject.SetActive(false);
                camera.orthographic = true;
                camera.orthographicSize = 11f;
                camera.transform.position = new Vector3(16.5f, 8f, -14f);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(16.5f, 1.2f, 2.8f) - camera.transform.position);
                Save(world, "linear-workshop-overview");
            }
            finally
            {
                if (roof != null) roof.enabled = roofVisible;
                canvas.enabled = oldCanvas;
                world.Player.hands.gameObject.SetActive(oldHands);
                camera.orthographic = oldOrtho;
                camera.orthographicSize = oldSize;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
            }
        }
        public static void Save(JourneyWorkshop world, string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera = world.Camera;
            var canvas = world.HUD.GetComponentInChildren<Canvas>();
            if (canvas == null) return;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            float oldAspect = camera.aspect;
            var target = new RenderTexture(2560, 1440, 24) { antiAliasing = 4 };
            var image = new Texture2D(2560, 1440, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.aspect = 16f / 9f;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.3f;
                world.HUD.RefreshDisplayInfo();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 2560, 1440), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Assets/Docs/JourneyValidation");
                File.WriteAllBytes("Assets/Docs/JourneyValidation/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                RenderTexture.active = oldActive;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}