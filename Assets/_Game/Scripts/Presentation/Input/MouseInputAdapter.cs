using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace SpringAutumn.Presentation.Input
{
    public class MouseInputAdapter : MonoBehaviour
    {
        private Vector2 _lastPosition;

        public bool TryRead(out PointerInputSample sample)
        {
            sample = default;

            if (UnityInput.GetMouseButtonDown(0))
            {
                _lastPosition = UnityInput.mousePosition;
                sample = new PointerInputSample
                {
                    Phase = PointerPhase.Began,
                    Position = _lastPosition,
                    Time = Time.unscaledTime
                };
                return true;
            }

            if (UnityInput.GetMouseButton(0))
            {
                Vector2 now = UnityInput.mousePosition;
                sample = new PointerInputSample
                {
                    Phase = PointerPhase.Moved,
                    Position = now,
                    Delta = now - _lastPosition,
                    Time = Time.unscaledTime
                };
                _lastPosition = now;
                return true;
            }

            if (UnityInput.GetMouseButtonUp(0))
            {
                Vector2 now = UnityInput.mousePosition;
                sample = new PointerInputSample
                {
                    Phase = PointerPhase.Ended,
                    Position = now,
                    Delta = now - _lastPosition,
                    Time = Time.unscaledTime
                };
                return true;
            }

            return false;
        }
    }
}
