using UnityEngine;
namespace CiCongNiSheng.Journey
{
    public sealed class JourneyStationLabel : MonoBehaviour
    {
        void LateUpdate()
        {
            var world = JourneyWorkshop.Instance;
            if (world == null || world.Camera == null) return;
            Vector3 facing = transform.position - world.Camera.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(facing);
        }
    }
}