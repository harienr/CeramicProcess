using UnityEngine;

namespace CiCongNiSheng
{
    /// <summary>工序级输入：静置跳转、重来。放在流程层而不是具体物件上。</summary>
    public class WorkshopDirector : MonoBehaviour
    {
        public SieveWashProcess process;
        public PlayerInteractor player;
        public Vector3 spawnPosition = new Vector3(0.1f, 0.06f, -1.15f);

        void Update()
        {
            if (process == null) return;

            if (Input.GetKeyDown(KeyCode.T) && process.Phase == WashPhase.Settle)
            {
                process.BeginSettle();
            }

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl && Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }
        }

        void Restart()
        {
            if (player != null)
            {
                player.DropTool();
                player.transform.position = spawnPosition;
            }
            process.ResetAll();
            Debug.Log("[瓷从泥生] 工序已重置。");
        }
    }
}
