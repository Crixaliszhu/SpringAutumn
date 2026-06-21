using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace SpringAutumn.Presentation.Input
{
    public class TouchInputAdapter : MonoBehaviour
    {
        public bool TryRead(out PointerInputSample sample)
        {
            sample = default;
            if (UnityInput.touchCount != 1)
                return false;

            Touch touch = UnityInput.GetTouch(0);
            PointerPhase phase = PointerPhase.None;
            if (touch.phase == TouchPhase.Began) phase = PointerPhase.Began;
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) phase = PointerPhase.Moved;
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) phase = PointerPhase.Ended;

            if (phase == PointerPhase.None)
                return false;

            sample = new PointerInputSample
            {
                Phase = phase,
                Position = touch.position,
                Delta = touch.deltaPosition,
                Time = Time.unscaledTime
            };
            return true;
        }
    }
}
