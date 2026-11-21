using UnityEngine;

namespace Porcelain
{
    /// <summary>
    /// 场景入口：把整个“过筛”工坊搭出来。
    ///
    /// - 编辑器里打开场景就会生成一次预览（对象标 DontSave，不会写进场景文件），
    ///   所以不按 Play 也能在 Scene / Game 视图里看到工坊全貌；
    /// - 按 Play 时清掉预览、重建一份“正式”的（参与物理、交互、界面）；
    /// - 退出 Play 后自动补回预览，不会留下一片空场景。
    ///
    /// 之所以运行时搭建：所有网格与贴图都由代码生成，不占工程资源；
    /// 想调布局/材质，改 WorkshopBuilder 与 Palette 即可，不必在场景里手工摆。
    /// </summary>
    [ExecuteAlways]
    public class WorkshopRuntime : MonoBehaviour
    {
        [Tooltip("进 Play 时自动搭建")]
        public bool buildOnAwake = true;

        [Tooltip("在编辑器里也直接搭出来（DontSave，不会污染场景文件）")]
        public bool previewInEditMode = true;

        public WorkshopRefs Refs { get; private set; }

#if UNITY_EDITOR
        void OnEnable()
        {
            if (Application.isPlaying || !previewInEditMode) return;
            // 场景刚加载时立刻建对象容易和资源导入打架，推迟一帧
            UnityEditor.EditorApplication.delayCall += BuildPreview;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            UnityEditor.EditorApplication.delayCall -= BuildPreview;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode &&
                previewInEditMode && !Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += BuildPreview;
            }
        }

        void BuildPreview()
        {
            if (this == null || gameObject == null) return;
            if (Application.isPlaying || !previewInEditMode) return;
            if (GameObject.Find("Workshop") != null) return;

            Refs = WorkshopBuilder.Build(true);
            Debug.Log("[瓷从泥生] 已在编辑器内生成场景预览（DontSave，不会写进场景文件）。" +
                      "按 Play 进入第一人称操作 —— 对准筛台按住 E 上料，再按住 E 摇筛，R 刮渣。");
        }
#endif

        void Awake()
        {
            if (!Application.isPlaying || !buildOnAwake) return;

            // 清掉编辑器预览与占位灯，避免对象重复、灯光重复
            var preview = GameObject.Find("Workshop");
            if (preview != null) DestroyImmediate(preview);
            var placeholder = GameObject.Find("BootstrapLight");
            if (placeholder != null) DestroyImmediate(placeholder);

            Refs = WorkshopBuilder.Build(false);
        }
    }
}
