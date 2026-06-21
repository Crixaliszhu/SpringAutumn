using UnityEngine;

namespace SpringAutumn.Presentation.Camera
{
    public interface ICameraController
    {
        UnityEngine.Camera Camera { get; }
        CameraState State { get; }
        bool IsActive { get; }
        void Activate();
        void Deactivate();
        void Pan(Vector2 screenDelta);
        void Zoom(float delta);
        void Focus(Vector3 worldPosition);
        void SaveState();
        void RestoreState();
    }
}
