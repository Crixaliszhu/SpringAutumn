using UnityEngine;

namespace SpringAutumn.Presentation.Camera
{
    public class RegionCameraController : MonoBehaviour, ICameraController
    {
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Vector2 xBounds = new Vector2(-25f, 25f);
        [SerializeField] private Vector2 zBounds = new Vector2(-25f, 25f);
        [SerializeField] private Vector2 heightBounds = new Vector2(8f, 35f);
        [SerializeField] private float panSpeed = 0.035f;
        [SerializeField] private float zoomSpeed = 3.5f;
        [SerializeField] private float smoothTime = 0.10f;

        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private CameraState _state = new CameraState();

        public UnityEngine.Camera Camera => targetCamera;
        public CameraState State => _state;
        public bool IsActive => targetCamera != null && targetCamera.gameObject.activeSelf;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<UnityEngine.Camera>();
            if (targetCamera != null)
            {
                targetCamera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                _targetPosition = Clamp(targetCamera.transform.position);
                SaveState();
            }
        }

        private void LateUpdate()
        {
            if (targetCamera != null && IsActive)
                targetCamera.transform.position = Vector3.SmoothDamp(targetCamera.transform.position, _targetPosition, ref _velocity, smoothTime);
        }

        public void Activate()
        {
            if (targetCamera != null)
                targetCamera.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            if (targetCamera != null)
                targetCamera.gameObject.SetActive(false);
        }

        public void Pan(Vector2 screenDelta)
        {
            _targetPosition += new Vector3(-screenDelta.x * panSpeed, 0f, -screenDelta.y * panSpeed);
            _targetPosition = Clamp(_targetPosition);
        }

        public void Zoom(float delta)
        {
            _targetPosition.y = Mathf.Clamp(_targetPosition.y - delta * zoomSpeed, heightBounds.x, heightBounds.y);
            _targetPosition = Clamp(_targetPosition);
        }

        public void Focus(Vector3 worldPosition)
        {
            _targetPosition = Clamp(new Vector3(worldPosition.x, _targetPosition.y, worldPosition.z));
        }

        public void SaveState()
        {
            _state.Capture(targetCamera);
        }

        public void RestoreState()
        {
            _state.Apply(targetCamera);
            if (targetCamera != null)
                _targetPosition = Clamp(targetCamera.transform.position);
        }

        private Vector3 Clamp(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, xBounds.x, xBounds.y);
            p.y = Mathf.Clamp(p.y, heightBounds.x, heightBounds.y);
            p.z = Mathf.Clamp(p.z, zBounds.x, zBounds.y);
            return p;
        }
    }
}
