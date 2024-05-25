
using System.Diagnostics;
using System;
using System.Numerics;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.SignalR;
using CSharpCraft.Clases.Item;

namespace CSharpCraft.Clases
{
    public class BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        // Check if this bounding box intersects with another bounding box
        public bool Intersects(BoundingBox other)
        {
            return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
                   (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
                   (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
        }
    }

    public class Player
    {
        public ChunkManager ChunkManager { get; set; }

        public string ConnectionId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 PreviousPosition { get; set; }
        public (int, int) PreviousChunkCoords { get; set; } = (int.MaxValue, int.MaxValue);
        public Vector3 Direction { get; set; }
        public Vector3 Velocity { get; set; }
        public bool IsOnGround { get; set; }
        public BoundingCylinder BoundingCylinder
        {
            get
            {
                return new BoundingCylinder(Position, Radius, Height);
            }
        }

        // Player dimensions and movement properties
        public float Height { get; } = 1.75f;
        public float Radius { get; } = 0.45f;
        public float EyeHeight { get; }
        public float Speed { get; } = 3f;
        public float MaxSpeed { get; } = 6f;
        public bool AutoJumping { get; set; } = false;
        public Item.Item CurrentItem { get; set; }

        public Position CameraPosition { 
            get {
                return new Position(Position.X, Position.Y+EyeHeight, Position.Z);
            } }

        public Player()
        {
            ConnectionId = "";
            Position = new Vector3(0, 0, 0); // Initial position
            Direction = new Vector3(0, 0, -1); // Initial direction
            Velocity = new Vector3(0, 0, 0); // Initial velocity
            EyeHeight = Height - 0.2f; // Eye height from the ground
            //UpdateBoundingBox();
        }

        // Update the player's bounding box based on the current position
        //private void UpdateBoundingBox()
        //{
        //    Vector3 min = new Vector3(Position.X - Radius, Position.Y, Position.Z - Radius);
        //    Vector3 max = new Vector3(Position.X + Radius, Position.Y + Height, Position.Z + Radius);
        //    BoundingBox = new BoundingBox(min, max);
        //}

        // Process movement input and check for collisions
        //public void UpdatePosition(float deltaTime, Vector3 movementDelta, Vector3 lookDirection)
        //{
        //    if(movementDelta.Length() > 0)
        //    {
        //        int asdfadsf = 0;
        //    }
        //    Direction = lookDirection;
        //    Vector3 proposedMovement = CalculateMovement(deltaTime, movementDelta);
        //    Vector3 potentialPosition = Position + proposedMovement;

        //    // Perform collision detection
        //    Vector3 collisionResponse = CheckForCollision(deltaTime, potentialPosition);
        //    Position += proposedMovement + collisionResponse;

        //    UpdateBoundingBox();
        //}

        // Calculate the new movement vector based on input
        private Vector3 CalculateMovement(float deltaTime, Vector3 movementDelta)
        {
            Vector3 forward = Vector3.Normalize(new Vector3(Direction.X, 0, Direction.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(Direction, Vector3.UnitY));

            Vector3 forwardMovement = forward * movementDelta.Z;
            Vector3 rightMovement = right * movementDelta.X;
            Vector3 verticalMovement = Vector3.UnitY * movementDelta.Y;

            return (forwardMovement + rightMovement + verticalMovement) * Speed;
        }

        // Check for collisions and return a response vector to adjust position if needed
        private Vector3 CheckForCollision(float deltaTime, Vector3 potentialPosition)
        {
            // Simulated collision detection logic
            // Assume collision detected and return a response vector
            // This should be replaced with actual game world collision checks
            Vector3 response = Vector3.Zero; // No collision assumed here for simplicity
                                             // If there's a collision, modify `response` to adjust the player's position appropriately
            return response;
        }

        public override string ToString()
        {
            return $"Position: {Position}, Direction: {Direction}";
        }
    }
}