
using System.Diagnostics;
using System;
using System.Numerics;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.SignalR;
using CSharpCraft.Clases.Item;
using CSharpCraft.Classes;

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
        public PlayerHotbar Hotbar { get; set; } = new PlayerHotbar();

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
        public float CurrentSpeed { get; set; } = 3f;
        public float RunSpeed { get; } = 10f;
        public float MaxSpeed { get; } = 10f;
        public float SneakSpeed { get; } = 1.5f;
        public bool AutoJumping { get; set; } = false;
        //public Item.Item CurrentItem { get; set; }

        public float CurrentJumpHeight { get; set; } = 1.1f;
        public float JumpHeight { get; } = 1.1f;

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

            SetPlayerHotbar();
            //UpdateBoundingBox();
        }

        public void SetPlayerHotbar()
        {
            //set the pickax image as a grass block until e have an image
            Hotbar.SetItem(0, new Pickaxe("Pickaxe", 100, "/Graphics/Models/pickaxe.glb", "/Graphics/Blocks2.png", 1, 10, 10));
            Hotbar.SetItem(1, new Block(VoxelData.BlockTypes[1].BlockName, 1, "", VoxelData.BlockTypes[1].TextureAtlas, VoxelData.BlockTypes[1].TopFaceTexture, 1, 10, 1));
            Hotbar.SetItem(2, new Block(VoxelData.BlockTypes[2].BlockName, 1, "", VoxelData.BlockTypes[2].TextureAtlas, VoxelData.BlockTypes[2].TopFaceTexture, 1, 10, 2));
            Hotbar.SetItem(3, new Block(VoxelData.BlockTypes[3].BlockName, 1, "", VoxelData.BlockTypes[3].TextureAtlas, VoxelData.BlockTypes[3].TopFaceTexture, 1, 10, 3));
            Hotbar.SetItem(4, new Block(VoxelData.BlockTypes[4].BlockName, 1, "", VoxelData.BlockTypes[4].TextureAtlas, VoxelData.BlockTypes[4].TopFaceTexture, 1, 10, 4));
            Hotbar.SetItem(5, new Block(VoxelData.BlockTypes[5].BlockName, 1, "", VoxelData.BlockTypes[5].TextureAtlas, VoxelData.BlockTypes[5].TopFaceTexture, 1, 10, 5));
            Hotbar.SetItem(6, new Block(VoxelData.BlockTypes[6].BlockName, 1, "", VoxelData.BlockTypes[6].TextureAtlas, VoxelData.BlockTypes[6].TopFaceTexture, 1, 10, 6));
            Hotbar.SetItem(7, new Block(VoxelData.BlockTypes[7].BlockName, 1, "", VoxelData.BlockTypes[7].TextureAtlas, VoxelData.BlockTypes[7].TopFaceTexture, 1, 10, 7));
            Hotbar.SetItem(8, new Block(VoxelData.BlockTypes[8].BlockName, 1, "", VoxelData.BlockTypes[8].TextureAtlas, VoxelData.BlockTypes[8].TopFaceTexture, 1, 10, 8));

            Hotbar.SelectItem(0);

        }
        

        //// Calculate the new movement vector based on input
        //private Vector3 CalculateMovement(float deltaTime, Vector3 movementDelta)
        //{
        //    Vector3 forward = Vector3.Normalize(new Vector3(Direction.X, 0, Direction.Z));
        //    Vector3 right = Vector3.Normalize(Vector3.Cross(Direction, Vector3.UnitY));

        //    Vector3 forwardMovement = forward * movementDelta.Z;
        //    Vector3 rightMovement = right * movementDelta.X;
        //    Vector3 verticalMovement = Vector3.UnitY * movementDelta.Y;

        //    return (forwardMovement + rightMovement + verticalMovement) * Speed;
        //}

        //// Check for collisions and return a response vector to adjust position if needed
        //private Vector3 CheckForCollision(float deltaTime, Vector3 potentialPosition)
        //{
        //    // Simulated collision detection logic
        //    // Assume collision detected and return a response vector
        //    // This should be replaced with actual game world collision checks
        //    Vector3 response = Vector3.Zero; // No collision assumed here for simplicity
        //                                     // If there's a collision, modify `response` to adjust the player's position appropriately
        //    return response;
        //}

        //public override string ToString()
        //{
        //    return $"Position: {Position}, Direction: {Direction}";
        //}
    }
}