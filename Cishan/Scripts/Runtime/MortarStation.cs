using System.Collections;
using UnityEngine;

namespace Cishan
{
    /// <summary>
    /// One mortar: fist stones are crushed into powder, the powder mound grows,
    /// and one scoop empties it into the bucket beside it.
    /// </summary>
    public class MortarStation : MonoBehaviour
    {
        public string id = "A";
        public TripHammer hammer;
        public Transform pile;
        public Renderer pileRenderer;
        public Transform stones;
        public Transform scoop;
        public Transform scoopPowder;
        public Transform pourAnchor;
        public BucketFill bucket;
        public PowderPuff dust;
        public int requiredStrikes = 6;
        public Color coarseColor = new Color(0.72f, 0.70f, 0.65f);
        public Color fineColor = new Color(0.85f, 0.83f, 0.79f);

        int strikes;
        bool animating;
        bool scooped;
        Vector3 pileRest;
        Vector3 stonesRest;
        Vector3 scoopRestPos;
        Quaternion scoopRestRot;
        MaterialPropertyBlock block;
        bool materialLoaded = true;
        public bool MaterialLoaded => materialLoaded;
        public bool LoadedAsPowder { get; private set; }

        public int Strikes { get { return strikes; } }
        public float Fill01 { get { return Mathf.Clamp01(strikes / (float)Mathf.Max(1, requiredStrikes)); } }
        public bool IsDone { get { return strikes >= requiredStrikes; } }
        public bool Scooped { get { return scooped; } }
        public bool Busy { get { return animating; } }
        public bool IsRunning { get { return hammer != null && !hammer.AtRest; } }

        void Awake()
        {
            if (pile != null)
            {
                pileRest = pile.localScale;
                pile.gameObject.SetActive(false);
            }
            if (stones != null) stonesRest = stones.localScale;
            if (scoop != null)
            {
                scoopRestPos = scoop.localPosition;
                scoopRestRot = scoop.localRotation;
            }
            if (scoopPowder != null) scoopPowder.gameObject.SetActive(false);
            block = new MaterialPropertyBlock();
        }

        void Start()
        {
            Refresh();
        }

        public void OnStrike()
        {
            if (scooped || !materialLoaded) return;
            strikes = Mathf.Min(strikes + 1, requiredStrikes);
            Refresh();
        }

        void Refresh()
        {
            if (!materialLoaded || scooped)
            {
                if (pile != null) pile.gameObject.SetActive(false);
                if (stones != null) stones.gameObject.SetActive(false);
                return;
            }
            float f = Fill01;
            if (pile != null)
            {
                pile.gameObject.SetActive(LoadedAsPowder || f > 0.02f);
                pile.localScale = Vector3.Lerp(pileRest * (LoadedAsPowder ? 0.7f : 0.35f), pileRest, Mathf.SmoothStep(0f, 1f, f));
            }
            if (stones != null)
            {
                stones.gameObject.SetActive(!LoadedAsPowder && f < 1f);
                float s = Mathf.Lerp(1f, 0.35f, f);
                stones.localScale = stonesRest * s;
            }
            if (pileRenderer != null)
            {
                pileRenderer.GetPropertyBlock(block);
                block.SetColor("_Color", Color.Lerp(coarseColor, fineColor, f));
                block.SetFloat("_DustAmount", Mathf.Lerp(0.15f, 0.45f, f));
                pileRenderer.SetPropertyBlock(block);
            }
        }

        public IEnumerator ScoopRoutine()
        {
            if (scoop == null || scooped || !materialLoaded || !IsDone) yield break;
            animating = true;
            if (dust != null) dust.Puff(10);
            if (CishanGame.I != null && CishanGame.I.audio != null) CishanGame.I.audio.PlayScoop();

            Vector3 dipHigh = new Vector3(0.02f, 0.34f, 0.02f);
            Vector3 dipLow = new Vector3(0.02f, 0.16f, 0.02f);
            Vector3 lifted = new Vector3(0.08f, 0.64f, 0.08f);
            Vector3 over = lifted;
            if (pourAnchor != null)
            {
                over = transform.InverseTransformPoint(pourAnchor.position) + new Vector3(0f, 0.30f, 0f);
            }

            Quaternion settle = Quaternion.Euler(0f, scoopRestRot.eulerAngles.y, 0f);
            Quaternion tilt = settle * Quaternion.Euler(0f, 0f, 72f);

            yield return MoveScoop(scoopRestPos, dipHigh, 0.35f, scoopRestRot, settle);
            yield return MoveScoop(dipHigh, dipLow, 0.22f, settle, settle);
            if (scoopPowder != null) scoopPowder.gameObject.SetActive(true);
            yield return MoveScoop(dipLow, lifted, 0.32f, settle, settle);
            yield return MoveScoop(lifted, over, 0.42f, settle, settle);
            yield return MoveScoop(over, over, 0.24f, settle, tilt);
            if (scoopPowder != null) scoopPowder.gameObject.SetActive(false);
            if (pile != null) pile.gameObject.SetActive(false);
            if (dust != null) dust.Puff(8);
            if (CishanGame.I != null && CishanGame.I.audio != null) CishanGame.I.audio.PlayPour();
            if (bucket != null) bucket.AddFill(0.5f);

            scooped = true;
            strikes = 0;
            if (stones != null) stones.gameObject.SetActive(false);
            yield return MoveScoop(over, scoopRestPos, 0.40f, tilt, scoopRestRot);
            animating = false;
        }

        IEnumerator MoveScoop(Vector3 from, Vector3 to, float duration, Quaternion r0, Quaternion r1)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                scoop.localPosition = Vector3.Lerp(from, to, k);
                scoop.localRotation = Quaternion.Slerp(r0, r1, k);
                yield return null;
            }
            scoop.localPosition = to;
            scoop.localRotation = r1;
        }

        public void ResetMortar()
        {
            scooped = false;
            strikes = 0;
            Refresh();
        }

        public void SetMaterialLoaded(bool loaded, int strikeGoal = 6, bool isPowder = false)
        {
            materialLoaded = loaded;
            LoadedAsPowder = loaded && isPowder;
            requiredStrikes = Mathf.Max(1, strikeGoal);
            scooped = false;
            strikes = 0;
            Refresh();
        }
    }
}
