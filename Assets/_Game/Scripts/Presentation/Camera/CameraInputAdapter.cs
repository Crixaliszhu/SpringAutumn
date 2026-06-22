using UnityEngine;
using UnityEngine.EventSystems;
using UnityInput = UnityEngine.Input;

namespace SpringAutumn.Presentation.Camera
{
    public class CameraInputAdapter : MonoBehaviour
    {
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private float wheelZoomScale = 1f;
        [SerializeField] private bool blockWhenPointerOverUI = true;
        [SerializeField] private bool handleDrag = false;
        [SerializeField] private bool handleZoom = true;

        private bool _dragging;
        private Vector2 _lastPointer;

        private void Update()
        {
            if (cameraManager == null)
                return;
            if (blockWhenPointerOverUI && IsPointerOverUI())
                return;

            HandleMouse();
            HandleTouch();
        }

        public bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleMouse()
        {
            if (UnityInput.GetMouseButtonDown(0))
            {
                _dragging = true;
                _lastPointer = UnityInput.mousePosition;
            }
            else if (UnityInput.GetMouseButtonUp(0))
            {
                _dragging = false;
            }
            else if (handleDrag && _dragging && UnityInput.GetMouseButton(0))
            {
                Vector2 now = UnityInput.mousePosition;
                cameraManager.Pan(now - _lastPointer);
                _lastPointer = now;
            }

            float wheel = UnityInput.mouseScrollDelta.y;
            if (handleZoom && Mathf.Abs(wheel) > 0.001f)
                cameraManager.Zoom(wheel * wheelZoomScale);
        }

        private void HandleTouch()
        {
            if (handleDrag && UnityInput.touchCount == 1)
            {
                Touch touch = UnityInput.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                    cameraManager.Pan(touch.deltaPosition);
            }
            else if (handleZoom && UnityInput.touchCount == 2)
            {
                Touch a = UnityInput.GetTouch(0);
                Touch b = UnityInput.GetTouch(1);
                Vector2 prevA = a.position - a.deltaPosition;
                Vector2 prevB = b.position - b.deltaPosition;
                float prevDistance = Vector2.Distance(prevA, prevB);
                float distance = Vector2.Distance(a.position, b.position);
                cameraManager.Zoom((distance - prevDistance) * 0.02f);
            }
        }
    }
}
