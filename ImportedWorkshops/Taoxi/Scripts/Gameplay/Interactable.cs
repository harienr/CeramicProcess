using UnityEngine;

namespace PorcelainFromClay
{
    public enum WorkshopAction
    {
        None,
        AddPowder,
        AddWater,
        Stir,
        OpenGate,
        CollectClay,
        ScrapeSand,
        Begin
    }

    /// <summary>Invisible, trigger-based aim target. The player never bumps into these.</summary>
    public sealed class Interactable : MonoBehaviour
    {
        public WorkshopAction Action = WorkshopAction.None;
        public string Label = "";
        public Vector3 FocusPoint;
        public Transform FocusTransform;

        public Vector3 Focus
        {
            get { return FocusTransform != null ? FocusTransform.position : FocusPoint; }
        }
    }
}
