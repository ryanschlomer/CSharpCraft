using System.Numerics;

namespace CSharpCraft.Clases
{
    public class BoundingCylinder
    {
        public Vector3 Bottom { get; set; }
        public float Radius { get; set; }
        public float Height { get; set; }

        public BoundingCylinder(Vector3 bottom, float radius, float height)
        {
            Bottom = bottom;
            Radius = radius;
            Height = height;
        }

        // Check if this bounding cylinder intersects with another bounding cylinder
        public bool Intersects(BoundingCylinder other)
        {
            // Check horizontal overlap (XZ plane)
            float dx = Bottom.X - other.Bottom.X;
            float dz = Bottom.Z - other.Bottom.Z;
            float distanceXZ = (float)Math.Sqrt(dx * dx + dz * dz);
            bool horizontalOverlap = distanceXZ <= (Radius + other.Radius);

            // Check vertical overlap
            bool verticalOverlap = (Bottom.Y <= other.Bottom.Y + other.Height) && (Bottom.Y + Height >= other.Bottom.Y);

            return horizontalOverlap && verticalOverlap;
        }

        // Check if this bounding cylinder intersects with a bounding box
        public bool Intersects(BoundingBox box)
        {
            // Find the closest point on the bounding box to the center of the cylinder's base
            float closestX = Math.Max(box.Min.X, Math.Min(Bottom.X, box.Max.X));
            float closestZ = Math.Max(box.Min.Z, Math.Min(Bottom.Z, box.Max.Z));

            // Calculate the distance from the closest point to the cylinder's base center
            float dx = Bottom.X - closestX;
            float dz = Bottom.Z - closestZ;
            float distanceXZ = (float)Math.Sqrt(dx * dx + dz * dz);

            // Check if the closest point is within the cylinder's radius
            //bool horizontalOverlap = distanceXZ <= Radius;
            // Check vertical overlap
            //bool verticalOverlap = (Bottom.Y <= box.Max.Y) && (Bottom.Y + Height >= box.Min.Y);

            bool horizontalOverlap = distanceXZ < Radius;

            // Check vertical overlap
            bool verticalOverlap = (Bottom.Y < box.Max.Y) && (Bottom.Y + Height > box.Min.Y);

            return horizontalOverlap && verticalOverlap;
        }
    }


}
