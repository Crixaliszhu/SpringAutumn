using UnityEngine;

namespace SpringAutumn.Presentation.Camera
{
    public enum CameraMode
    {
        World,
        Region
    }

    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour worldCameraController;
        [SerializeField] private MonoBehaviour regionCameraController;

        public ICameraController WorldCamera => worldCameraController as ICameraController;
        public ICameraController RegionCamera => regionCameraController as ICameraController;
        public ICameraController ActiveController { get; private set; }
        public CameraMode Mode { get; private set; }

        public void SwitchToWorld()
        {
            Switch(CameraMode.World, WorldCamera);
        }

        public void SwitchToRegion(Vector3 regionCenter)
        {
            Switch(CameraMode.Region, RegionCamera);
            ActiveController?.Focus(regionCenter);
        }

        public void Pan(Vector2 delta)
        {
            ActiveController?.Pan(delta);
        }

        public void Zoom(float delta)
        {
            ActiveController?.Zoom(delta);
        }

        private void Switch(CameraMode mode, ICameraController next)
        {
            if (next == null)
                return;

            ActiveController?.SaveState();
            ActiveController?.Deactivate();
            Mode = mode;
            ActiveController = next;
            ActiveController.Activate();
            ActiveController.RestoreState();
        }
    }
}
