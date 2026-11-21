using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class PlayerRig
    {
        public GameObject Root;
        public CharacterController Controller;
        public FirstPersonController Movement;
        public PlayerInteractor Interactor;
        public Camera MainCamera;
        public Camera ViewCamera;
        public ViewmodelRig Viewmodel;
        public AudioListener Listener;

        public static PlayerRig Create(Transform parent, Vector3 spawn, float yaw, float pitch, MaterialLibrary mats, WorkshopUI ui)
        {
            var rig = new PlayerRig();
            var root = new GameObject("Player");
            root.transform.SetParent(parent, false);
            root.transform.position = spawn;
            root.layer = 2;
            rig.Root = root;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.22f;
            cc.slopeLimit = 46f;
            cc.skinWidth = 0.025f;
            rig.Controller = cc;

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 1.62f, 0f);

            var camGO = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            camGO.transform.SetParent(pivot, false);
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.045f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.052f, 0.047f);
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.cullingMask = ~(1 << ViewmodelRig.Layer);
            cam.depth = 0f;
            rig.MainCamera = cam;

            var grade = camGO.AddComponent<PostGrade>();
            grade.GradeMaterial = mats.PostGrade;
            grade.Exposure = 1.16f;
            grade.Saturation = 0.86f;
            grade.Contrast = 1.02f;
            grade.Warmth = 0.06f;
            grade.Vignette = 0.1f;
            grade.Grain = 0.01f;
            rig.Listener = camGO.GetComponent<AudioListener>();

            var viewGO = new GameObject("ViewmodelCamera", typeof(Camera));
            viewGO.transform.SetParent(camGO.transform, false);
            viewGO.transform.localPosition = Vector3.zero;
            viewGO.transform.localRotation = Quaternion.identity;
            var viewCam = viewGO.GetComponent<Camera>();
            viewCam.clearFlags = CameraClearFlags.Depth;
            viewCam.cullingMask = 1 << ViewmodelRig.Layer;
            viewCam.nearClipPlane = 0.01f;
            viewCam.farClipPlane = 1.6f;
            viewCam.fieldOfView = 54f;
            viewCam.allowHDR = true;
            viewCam.allowMSAA = true;
            viewCam.depth = 1f;
            viewCam.useOcclusionCulling = false;
            rig.ViewCamera = viewCam;

            var vm = viewGO.AddComponent<ViewmodelRig>();
            vm.Init(mats, viewCam);
            rig.Viewmodel = vm;

            var movement = root.AddComponent<FirstPersonController>();
            movement.SetCameraPivot(pivot);
            movement.InputEnabled = false;
            movement.Teleport(spawn, yaw, pitch);
            rig.Movement = movement;

            var interactor = root.AddComponent<PlayerInteractor>();
            interactor.Cam = cam;
            interactor.UI = ui;
            rig.Interactor = interactor;

            return rig;
        }
    }
}

