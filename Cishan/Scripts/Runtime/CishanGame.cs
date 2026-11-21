using UnityEngine;

namespace Cishan
{
    /// <summary>Scene root services locator, assigned by the editor builder.</summary>
    public class CishanGame : MonoBehaviour
    {
        public static CishanGame I;

        public CishanHUD hud;
        public CishanAudio audio;
        public ChongShiSequence sequence;
        public FirstPersonController player;
        public PlayerInteractor interactor;
        public SluiceGate gate;
        public WaterWheelDrive wheel;

        void Awake()
        {
            I = this;
            if (hud == null) hud = FindObjectOfType<CishanHUD>();
            if (audio == null) audio = GetComponent<CishanAudio>();
            if (sequence == null) sequence = FindObjectOfType<ChongShiSequence>();
            if (player == null) player = FindObjectOfType<FirstPersonController>();
            if (interactor == null) interactor = FindObjectOfType<PlayerInteractor>();
        }

        public void PlayHammer(Vector3 position)
        {
            if (audio != null) audio.PlayHammer(position);
        }
    }
}
