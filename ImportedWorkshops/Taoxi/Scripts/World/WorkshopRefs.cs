using System.Collections.Generic;
using UnityEngine;

namespace PorcelainFromClay
{
    public sealed class WorkshopRefs
    {
        public Transform Root;
        public Transform PlayerSpawn;
        public float SpawnYaw;
        public float SpawnPitch;

        public TaoxiVat UpperVat;
        public TaoxiVat LowerVat;
        public Transform ChannelWater;
        public Transform GateBoard;
        public Transform TailingsBucket;
        public Transform TailingsPile;
        public Transform ClayTray;
        public List<Transform> TrayLumps = new List<Transform>();
        public Transform PourAnchor;
        public Transform StirAnchor;
        public Transform GateAnchor;
        public Transform ClayAnchor;
        public Transform ScrapeAnchor;
        public Transform PowderAnchor;
        public Transform BucketAnchor;

        public Interactable PowderTarget;
        public Interactable WaterTarget;
        public Interactable StirTarget;
        public Interactable GateTarget;
        public Interactable ClayTarget;
        public Interactable ScrapeTarget;

        public Light WindowSun;
        public Light WorkFill;
        public Camera MainCamera;
        public PostGrade PostGrade;
    }
}
