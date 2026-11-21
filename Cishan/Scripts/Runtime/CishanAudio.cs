using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// Tiny procedural soundscape: filtered water, a wooden axle creak, stone
    /// thuds, gate scrape, scoop and pour. No music, no UI blips.
    /// </summary>
    public class CishanAudio : MonoBehaviour
    {
        public SluiceGate gate;
        public WaterWheelDrive wheel;
        public float master = 0.8f;

        AudioSource waterSource;
        AudioSource creakSource;
        AudioSource oneShot;

        AudioClip waterClip;
        AudioClip creakClip;
        AudioClip hammerClip;
        AudioClip gateClip;
        AudioClip scoopClip;
        AudioClip pourClip;
        AudioClip stepClip;

        void Awake()
        {
            waterClip = BuildWater();
            creakClip = BuildCreak();
            hammerClip = BuildHammer();
            gateClip = BuildGate();
            scoopClip = BuildScoop();
            pourClip = BuildPour();
            stepClip = BuildStep();

            waterSource = MakeSource("WaterLoop", waterClip, 0.06f);
            creakSource = MakeSource("AxleLoop", creakClip, 0f);
            oneShot = MakeSource("OneShots", null, 1f);
            if (waterSource != null) waterSource.Play();
            if (creakSource != null) creakSource.Play();
        }

        AudioSource MakeSource(string name, AudioClip clip, float volume)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = clip != null;
            src.playOnAwake = false;
            src.volume = volume;
            src.spatialBlend = 0f;
            return src;
        }

        void Update()
        {
            float open = gate != null ? gate.OpenAmount : 0f;
            float speed = wheel != null ? wheel.Speed01 : 0f;

            if (waterSource != null)
            {
                waterSource.volume = Mathf.Lerp(0.055f, 0.40f, open) * master;
                waterSource.pitch = Mathf.Lerp(0.94f, 1.05f, open);
            }
            if (creakSource != null)
            {
                creakSource.volume = speed * 0.22f * master;
                creakSource.pitch = Mathf.Lerp(0.7f, 1.12f, speed);
            }
        }

        public void PlayHammer(Vector3 worldPos)
        {
            if (oneShot == null || hammerClip == null) return;
            float d = 6f;
            if (Camera.main != null) d = Vector3.Distance(Camera.main.transform.position, worldPos);
            float near = Mathf.Clamp01(1f - d / 22f);
            oneShot.pitch = Random.Range(0.95f, 1.04f);
            oneShot.PlayOneShot(hammerClip, Mathf.Lerp(0.16f, 0.62f, near) * master);
        }

        public void PlayGate()
        {
            if (oneShot != null && gateClip != null)
            {
                oneShot.pitch = Random.Range(0.94f, 1.02f);
                oneShot.PlayOneShot(gateClip, 0.45f * master);
            }
        }

        public void PlayScoop()
        {
            if (oneShot != null && scoopClip != null)
            {
                oneShot.pitch = Random.Range(0.96f, 1.05f);
                oneShot.PlayOneShot(scoopClip, 0.32f * master);
            }
        }

        public void PlayPour()
        {
            if (oneShot != null && pourClip != null)
            {
                oneShot.pitch = 1f;
                oneShot.PlayOneShot(pourClip, 0.35f * master);
            }
        }

        public void PlayFootstep(float volume)
        {
            if (oneShot != null && stepClip != null)
            {
                oneShot.pitch = Random.Range(0.9f, 1.1f);
                oneShot.PlayOneShot(stepClip, 0.20f * volume * master);
            }
        }

        // ------------------------------------------------------------------ synth
        static AudioClip Finish(float[] data, int rate, string name)
        {
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip BuildWater()
        {
            const int rate = 44100;
            const float dur = 4f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(5);
            float lp = 0f;
            float lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.055f * (w - lp);
                lp2 += 0.006f * (lp - lp2);
                float mod = 0.62f + 0.20f * Mathf.Sin(2f * Mathf.PI * 0.21f * t)
                           + 0.18f * Mathf.Sin(2f * Mathf.PI * 0.07f * t + 1.1f);
                data[i] = (lp * 0.55f + lp2 * 1.4f) * mod * 0.55f;
            }
            int fade = rate / 40;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[n - 1 - i] *= k;
            }
            return Finish(data, rate, "WaterLoop");
        }

        static AudioClip BuildCreak()
        {
            const int rate = 44100;
            const float dur = 4f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(9);

            float[] starts = { 0.45f, 1.75f, 2.95f };
            for (int e = 0; e < starts.Length; e++)
            {
                int start = (int)(starts[e] * rate);
                int len = (int)(0.55f * rate);
                for (int i = 0; i < len && start + i < n; i++)
                {
                    float t = i / (float)rate;
                    float k = i / (float)len;
                    float env = Mathf.Sin(k * Mathf.PI);
                    float f = Mathf.Lerp(270f, 175f, k) + e * 12f;
                    float s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.18f;
                    s += Mathf.Sin(2f * Mathf.PI * f * 2.01f * t) * 0.06f;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.03f;
                    data[start + i] += (s + noise) * env;
                }
            }

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] += Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.035f
                           * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.13f * t));
            }

            int fade = rate / 30;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[n - 1 - i] *= k;
            }
            return Finish(data, rate, "AxleLoop");
        }

        static AudioClip BuildHammer()
        {
            const int rate = 44100;
            const float dur = 0.55f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(13);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 16f);
                float f = 58f + 26f * Mathf.Exp(-t * 26f);
                float body = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.62f;
                float click = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 70f) * 0.42f;
                float stone = Mathf.Sin(2f * Mathf.PI * 173f * t) * Mathf.Exp(-t * 34f) * 0.20f;
                data[i] = body + click + stone;
            }
            return Finish(data, rate, "Hammer");
        }

        static AudioClip BuildGate()
        {
            const int rate = 44100;
            const float dur = 0.85f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(17);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float k = t / dur;
                float env = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.08f * (white - lp);
                float scrape = lp * env * 0.55f;
                float knock = Mathf.Sin(2f * Mathf.PI * 92f * t) * Mathf.Exp(-t * 22f) * 0.4f;
                data[i] = scrape + knock;
            }
            return Finish(data, rate, "Gate");
        }

        static AudioClip BuildScoop()
        {
            const int rate = 44100;
            const float dur = 0.7f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(19);
            float hp = 0f;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 7f) * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 9f * t)));
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                hp = 0.85f * (hp + w - prev);
                prev = w;
                data[i] = hp * env * 0.22f;
            }
            return Finish(data, rate, "Scoop");
        }

        static AudioClip BuildPour()
        {
            const int rate = 44100;
            const float dur = 1.5f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(23);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float k = t / dur;
                float env = Mathf.Exp(-k * 3.2f) * Mathf.Clamp01(k * 12f);
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.12f * (w - lp);
                float grain = (float)(rng.NextDouble()) > 0.985f ? 0.6f : 0f;
                data[i] = (lp + grain) * env * 0.28f;
            }
            return Finish(data, rate, "Pour");
        }

        static AudioClip BuildStep()
        {
            const int rate = 44100;
            const float dur = 0.18f;
            int n = (int)(rate * dur);
            float[] data = new float[n];
            System.Random rng = new System.Random(29);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float w = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.09f * (w - lp);
                float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 45f) * 0.35f;
                data[i] = thump + lp * Mathf.Exp(-t * 30f) * 0.25f;
            }
            return Finish(data, rate, "Step");
        }
    }
}
