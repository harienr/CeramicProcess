using UnityEngine;
using UnityEngine.Rendering;

namespace Cishan
{
    /// <summary>Interior light and grade: soft south daylight, warm grey bounce, restrained ACES-ish look.</summary>
    public class CishanLook : MonoBehaviour
    {
        public Camera cam;
        public ReflectionProbe probe;
        public float ambientIntensity = 0.95f;
        public Color skyAmbient = new Color(0.72f, 0.70f, 0.64f);
        public Color equatorAmbient = new Color(0.55f, 0.52f, 0.46f);
        public Color groundAmbient = new Color(0.30f, 0.29f, 0.26f);

        void Awake()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 9f;
            RenderSettings.fogEndDistance = 36f;
            RenderSettings.fogColor = Palette.Hex("D8CFBD");

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyAmbient;
            RenderSettings.ambientEquatorColor = equatorAmbient;
            RenderSettings.ambientGroundColor = groundAmbient;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = 0.55f;

            QualitySettings.shadowDistance = 30f;
            QualitySettings.pixelLightCount = 6;

            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Palette.Hex("E8DFCE");
                cam.nearClipPlane = 0.045f;
                cam.farClipPlane = 70f;
                if (cam.GetComponent<CishanGrade>() == null) cam.gameObject.AddComponent<CishanGrade>();
            }
        }

        void Start()
        {
            if (probe != null) probe.RenderProbe();
        }
    }
}
