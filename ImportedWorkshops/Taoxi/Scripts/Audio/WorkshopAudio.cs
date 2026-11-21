using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class WorkshopAudio : MonoBehaviour
    {
        AudioSource room;
        AudioSource oneShot;
        AudioSource stir;
        AudioSource steps;
        AudioClip roomClip;
        AudioClip pourClip;
        AudioClip stirClip;
        AudioClip gateClip;
        AudioClip scoopClip;
        AudioClip clickClip;
        AudioClip dripClip;
        AudioClip[] stepClips;

        FirstPersonController player;
        float stepTimer;
        float dripTimer;
        float stirTarget;
        float pourCooldown;

        public void Init()
        {
            roomClip = Resources.Load<AudioClip>("Audio/room_tone");
            pourClip = Resources.Load<AudioClip>("Audio/water_pour");
            stirClip = Resources.Load<AudioClip>("Audio/stir_loop");
            gateClip = Resources.Load<AudioClip>("Audio/gate");
            scoopClip = Resources.Load<AudioClip>("Audio/clay_scoop");
            clickClip = Resources.Load<AudioClip>("Audio/ui_click");
            dripClip = Resources.Load<AudioClip>("Audio/drip");
            stepClips = new[]
            {
                Resources.Load<AudioClip>("Audio/step_1"),
                Resources.Load<AudioClip>("Audio/step_2"),
                Resources.Load<AudioClip>("Audio/step_3"),
                Resources.Load<AudioClip>("Audio/step_4")
            };

            room = MakeSource("Room", true, 0.22f, 0f);
            room.clip = roomClip;
            if (roomClip != null) room.Play();

            oneShot = MakeSource("OneShot", false, 0.6f, 0f);
            steps = MakeSource("Steps", false, 0.22f, 0f);

            stir = MakeSource("Stir", true, 0f, 0f);
            stir.clip = stirClip;
            if (stirClip != null)
            {
                stir.Play();
                stir.volume = 0f;
            }

            dripTimer = Random.Range(7f, 16f);
            var listener = FindObjectOfType<AudioListener>();
            if (listener != null && listener.GetComponent<AudioReverbFilter>() == null)
            {
                var r = listener.gameObject.AddComponent<AudioReverbFilter>();
                r.reverbPreset = AudioReverbPreset.Room;
                r.dryLevel = -180f;
                r.room = -650f;
                r.roomHF = -300f;
                r.decayTime = 1.15f;
                r.decayHFRatio = 0.55f;
                r.reflectionsLevel = -900f;
                r.reverbLevel = -1200f;
                r.reverbDelay = 0.025f;
            }
        }

        AudioSource MakeSource(string name, bool loop, float volume, float spatialBlend)
        {
            var go = new GameObject("Audio_" + name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.volume = volume;
            s.spatialBlend = spatialBlend;
            s.dopplerLevel = 0f;
            return s;
        }

        public void AttachPlayer(FirstPersonController controller)
        {
            player = controller;
        }

        public void PlayPour()
        {
            if (pourClip == null || pourCooldown > 0f) return;
            oneShot.pitch = Random.Range(0.96f, 1.04f);
            oneShot.PlayOneShot(pourClip, 0.55f);
            pourCooldown = 2.5f;
        }

        public void PlayGate()
        {
            if (gateClip == null) return;
            oneShot.pitch = Random.Range(0.94f, 1.03f);
            oneShot.PlayOneShot(gateClip, 0.5f);
        }

        public void PlayScoop()
        {
            if (scoopClip == null) return;
            oneShot.pitch = Random.Range(0.92f, 1.06f);
            oneShot.PlayOneShot(scoopClip, 0.46f);
        }

        public void PlayClick()
        {
            if (clickClip == null) return;
            oneShot.pitch = Random.Range(0.98f, 1.02f);
            oneShot.PlayOneShot(clickClip, 0.25f);
        }

        public void SetStir(bool active)
        {
            stirTarget = active ? 0.3f : 0f;
        }

        public void SetRoomMood(float volume)
        {
            if (room != null) room.volume = volume;
        }

        void Update()
        {
            if (pourCooldown > 0f) pourCooldown -= Time.deltaTime;

            if (stir != null)
            {
                stir.volume = Mathf.Lerp(stir.volume, stirTarget, Time.deltaTime * 4f);
                if (stirTarget > 0f) stir.pitch = Mathf.Lerp(stir.pitch, 0.92f + Mathf.Sin(Time.time * 0.7f) * 0.035f, Time.deltaTime * 2f);
            }

            if (player != null && player.IsMoving && player.IsGrounded)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f && stepClips != null)
                {
                    stepTimer = Random.Range(0.47f, 0.62f);
                    var clip = stepClips[Random.Range(0, stepClips.Length)];
                    if (clip != null)
                    {
                        steps.pitch = Random.Range(0.9f, 1.06f);
                        steps.PlayOneShot(clip, Random.Range(0.12f, 0.2f));
                    }
                }
            }
            else
            {
                stepTimer = Mathf.Min(stepTimer, 0.05f);
            }

            dripTimer -= Time.deltaTime;
            if (dripTimer <= 0f)
            {
                dripTimer = Random.Range(11f, 24f);
                if (dripClip != null) oneShot.PlayOneShot(dripClip, 0.09f);
            }
        }
    }
}
