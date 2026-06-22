using UnityEngine;
using UnityEngine.EventSystems;
using SpringAutumn.Presentation.Camera;

namespace SpringAutumn.Presentation.Input
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera raycastCamera;
        [SerializeField] private SelectionManager selectionManager;
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private MouseInputAdapter mouseInput;
        [SerializeField] private TouchInputAdapter touchInput;
        [SerializeField] private LayerMask armyLayer;
        [SerializeField] private LayerMask cityLayer;
        [SerializeField] private LayerMask villageLayer;
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private float clickMoveThreshold = 10f;
        [SerializeField] private float clickTimeThreshold = 0.2f;

        private Vector2 _startPosition;
        private float _startTime;
        private bool _dragged;

        public InputState State { get; private set; } = InputState.Idle;

        private void Update()
        {
            if (IsPointerOverUI())
                return;

            if (TryRead(out var sample))
                Handle(sample);
        }

        public bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool TryRead(out PointerInputSample sample)
        {
            sample = default;
            if (touchInput != null && touchInput.TryRead(out sample))
                return true;
            return mouseInput != null && mouseInput.TryRead(out sample);
        }

        private void Handle(PointerInputSample sample)
        {
            if (sample.Phase == PointerPhase.Began)
            {
                _startPosition = sample.Position;
                _startTime = sample.Time;
                _dragged = false;
                State = InputState.Selecting;
                return;
            }

            if (sample.Phase == PointerPhase.Moved)
            {
                if (Vector2.Distance(_startPosition, sample.Position) > clickMoveThreshold)
                {
                    _dragged = true;
                    State = InputState.DraggingCamera;
                    cameraManager?.Pan(sample.Delta);
                }
                return;
            }

            if (sample.Phase == PointerPhase.Ended)
            {
                bool isClick = !_dragged
                    && Vector2.Distance(_startPosition, sample.Position) <= clickMoveThreshold
                    && sample.Time - _startTime <= clickTimeThreshold;

                if (isClick)
                {
                    State = InputState.OpeningUI;
                    RaycastSelection(sample.Position);
                }

                State = InputState.Idle;
            }
        }

        private void RaycastSelection(Vector2 screenPosition)
        {
            UnityEngine.Camera cameraToUse = cameraManager?.ActiveController?.Camera;
            if (cameraToUse == null)
                cameraToUse = raycastCamera != null ? raycastCamera : UnityEngine.Camera.main;
            if (cameraToUse == null)
                return;

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            if (TrySelect(ray, armyLayer)) return;
            if (TrySelect(ray, cityLayer)) return;
            if (TrySelect(ray, villageLayer)) return;
            TrySelect(ray, terrainLayer);
        }

        private bool TrySelect(Ray ray, LayerMask layer)
        {
            if (layer.value == 0)
                return false;
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, layer))
                return false;

            ISelectable selectable = null;
            var behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                selectable = behaviours[i] as ISelectable;
                if (selectable != null)
                    break;
            }

            if (selectable == null)
                return false;

            selectionManager?.Select(selectable);
            return true;
        }
    }
}
