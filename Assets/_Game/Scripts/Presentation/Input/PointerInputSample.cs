using UnityEngine;

namespace SpringAutumn.Presentation.Input
{
    public enum PointerPhase
    {
        None,
        Began,
        Moved,
        Ended
    }

    public struct PointerInputSample
    {
        public PointerPhase Phase;
        public Vector2 Position;
        public Vector2 Delta;
        public float Time;
    }
}
