using UnityEngine;

namespace SpringAutumn.Presentation.Camera
{
    [System.Serializable]
    public class CameraState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float OrthographicSize;
        public float FieldOfView;

        public CameraState() { }

        public CameraState(UnityEngine.Camera camera)
        {
            Capture(camera);
        }

        public void Capture(UnityEngine.Camera camera)
        {
            if (camera == null)
                return;

            Position = camera.transform.position;
            Rotation = camera.transform.rotation;
            OrthographicSize = camera.orthographicSize;
            FieldOfView = camera.fieldOfView;
        }

        public void Apply(UnityEngine.Camera camera)
        {
            if (camera == null)
                return;

            camera.transform.position = Position;
            camera.transform.rotation = Rotation;
            camera.orthographicSize = OrthographicSize;
            camera.fieldOfView = FieldOfView;
        }
    }
}
