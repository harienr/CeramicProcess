using UnityEngine;

namespace PorcelainFromClay
{
    /// <summary>
    /// Entry point. Drop this on an empty GameObject in a scene; everything else is generated at runtime.
    /// The scene file in Assets/Scenes contains exactly this component.
    /// </summary>
    public sealed class WorkshopBootstrap : MonoBehaviour
    {
        public static WorkshopBootstrap Instance;
        public static bool IsCaptureRun;

        public WorkshopRefs Refs { get; private set; }
        public PlayerRig Player { get; private set; }
        public WorkshopUI UI { get; private set; }
        public TaoxiSequence Sequence { get; private set; }
        public WorkshopAudio Audio { get; private set; }
        public MaterialLibrary Materials { get; private set; }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;

            Materials = new MaterialLibrary();
            Materials.Build();

            UI = WorkshopUI.Create(transform, Materials);

            var builder = new WorkshopBuilder(Materials, transform);
            Refs = builder.Build();

            Player = PlayerRig.Create(transform, Refs.PlayerSpawn.position, Refs.SpawnYaw, Refs.SpawnPitch, Materials, UI);
            Refs.MainCamera = Player.MainCamera;
            Refs.PostGrade = Player.MainCamera.GetComponent<PostGrade>();

            Audio = gameObject.AddComponent<WorkshopAudio>();
            Audio.Init();

            Sequence = gameObject.AddComponent<TaoxiSequence>();
            Sequence.Init(Refs, Player, UI, Audio);

            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-capture") IsCaptureRun = true;
            }

            if (IsCaptureRun)
            {
                Debug.Log("独立淘洗预览脚本保存在 Preview 目录，整合版使用统一验证入口。");
            }

            foreach (var arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-selftest")
                {
                    Debug.Log("请使用瓷从泥生 / 整合工坊 / 验证流程。");
                }
            }
        }
    }
}
