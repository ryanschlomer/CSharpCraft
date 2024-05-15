using Microsoft.JSInterop;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class CameraInfo
    {
        // Camera position in the world space
        public Vector3 Position { get; set; }

        // Camera's forward direction vector
        public Vector3 Direction { get; set; }

        // Up vector, typically (0, 1, 0) in most 3D environments
        public Vector3 Up { get; set; }

        // Field of view (in degrees), typically between 60 and 90
        public float Fov { get; set; }

        // Aspect ratio, typically the viewport's width divided by its height
        public float AspectRatio { get; set; }

        // The closest distance the camera can see
        public float NearClip { get; set; }

        // The farthest distance the camera can see
        public float FarClip { get; set; }

        public CameraInfo()
        {
            // Initialize with default values
            Position = new Vector3(0, 0, 0);
            Direction = new Vector3(0, 0, -1);
            Up = new Vector3(0, 1, 0);
            Fov = 75f; // A common field of view
            AspectRatio = 1.33f; // Common aspect ratio (e.g., 800px by 600px)
            NearClip = 0.1f;
            FarClip = 1000f;
        }

      
        public CameraInfo(Vector3 position, Vector3 direction, Vector3 up, float fov, float aspectRatio, float nearClip, float farClip)
        {
            Position = position;
            Direction = direction;
            Up = up;
            Fov = fov;
            AspectRatio = aspectRatio;
            NearClip = nearClip;
            FarClip = farClip;
        }
    }

}
