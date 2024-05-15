//using CSharpCraft.Clases;
//using CSharpCraft.Classes;
//using System;
//using System.Collections.Generic;
//using System.Numerics;
//using static System.Reflection.Metadata.BlobBuilder;

//namespace CSharpCraft.Classes
//{
//    public class Physics
//    {
//        public static float Gravity { get; } = 9.81f; // Standard gravity
//        public static int SimulationRate { get; } = 60; // Physics updates per second
//        public static float StepSize => 1f / SimulationRate;
//        public static float Accumulator { get; private set; } = 0f;

//        public List<HelperBlock> Helpers = new List<HelperBlock>();
//        public bool HelpersVisible = true;

//        ChunkManager _chunkManager;

//        public Physics(ChunkManager chunkManager)
//        {
//            _chunkManager = chunkManager;
//        }

//        public void HandlePlayerInput(Player player, Vector3 movementDelta, Vector3 lookDirection, float deltaTime)
//        {
//            // Set direction for potential use in movement calculations
//            player.Direction = lookDirection;

//            // Calculate intended movement based on input
//            Vector3 intendedVelocity = CalculateIntendedVelocity(player, movementDelta, lookDirection, deltaTime);
//            player.Velocity += intendedVelocity;  // Consider existing velocity
//            //player.Velocity = intendedVelocity;

//            // Clamp the velocity to a maximum speed to prevent excessive movement speeds
//            //player.Velocity = Vector3.Clamp(player.Velocity, -player.MaxSpeed, player.MaxSpeed);

//            // Now handle regular updates
//            Update(deltaTime, player);


//            //TODO: Send the HelpersBlocks to the renderer
//        }

//        public Vector3 CalculateIntendedVelocity(Player player, Vector3 movementDelta, Vector3 lookDirection, float deltaTime)
//        {
//            // Normalize the look direction to ensure it has a unit length
//            lookDirection = Vector3.Normalize(lookDirection);

//            // Calculate forward and right vectors
//            Vector3 forward = new Vector3(lookDirection.X, 0, lookDirection.Z); // Assuming Y is up and should not affect horizontal movement
//            Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward)); // Cross product to get the right vector orthogonal to up and forward

//            // Calculate the intended movement by projecting the movement delta onto the forward and right vectors
//            Vector3 intendedMovement = (forward * movementDelta.Z + right * movementDelta.X + Vector3.UnitY * movementDelta.Y) * player.Speed;

//            // Multiply by deltaTime to convert from speed to velocity
//            return intendedMovement * deltaTime;
//        }



//        public void Update(float dt, Player player)
//        {
//            Accumulator += dt;
//            while (Accumulator >= StepSize)
//            {
//                ApplyGravity(player, StepSize);
//                DetectCollisions(player);
//                //player.UpdatePosition(StepSize);  // Now handling position update here
//                Accumulator -= StepSize;
//            }
//        }


//        private void ApplyGravity(Player player, float stepSize)
//        {
//            // Simulating gravity effect on the player's vertical velocity
//            player.Velocity = new Vector3(player.Velocity.X, player.Velocity.Y - Gravity * stepSize, player.Velocity.Z);

//            //The ground is not at Y=0
//            if (player.Position.Y <= 0 && player.Velocity.Y < 0)
//            {
//                player.Velocity = new Vector3(player.Velocity.X, 0, player.Velocity.Z);
//                player.Position = new Vector3(player.Position.X, 0, player.Position.Z); // Reset position if below ground level
//                player.IsOnGround = true;
//            }
//        }


//        private void DetectCollisions(Player player)
//        {
//            player.IsOnGround = false;
//            Helpers.Clear();

//            var candidates = BroadPhase(player);
//            var collisions = NarrowPhase(candidates, player);

//            if (collisions.Count > 0)
//            {
//                ResolveCollisions(collisions, player);
//            }
//        }

//        private List<Vector3> BroadPhase(Player player)
//        {
//            var candidates = new List<Vector3>();

//            // Calculating potential block collision areas based on the player's bounding box
//            int minX = (int)Math.Floor(player.Position.X - player.Radius);
//            int maxX = (int)Math.Ceiling(player.Position.X + player.Radius);
//            int minY = (int)Math.Floor(player.Position.Y);
//            int maxY = (int)Math.Ceiling(player.Position.Y + player.Height);
//            int minZ = (int)Math.Floor(player.Position.Z - player.Radius);
//            int maxZ = (int)Math.Ceiling(player.Position.Z + player.Radius);

//            for (int x = minX; x <= maxX; x++)
//            {
//                for (int y = minY; y <= maxY; y++)
//                {
//                    for (int z = minZ; z <= maxZ; z++)
//                    {
//                        if (_chunkManager.chunkService.IsBlockSolid(x, y, z))
//                        {
//                            var block = new Vector3(x, y, z);
//                            candidates.Add(block);
//                            if (HelpersVisible) AddCollisionHelper(new HelperBlock(block));
//                        }
//                    }
//                }
//            }

//            return candidates;
//        }

//        private List<Collision> NarrowPhase(List<Vector3> candidates, Player player)
//        {
//            var collisions = new List<Collision>();

//            foreach (var block in candidates)
//            {
//                var dx = block.X + 0.5f - player.Position.X;
//                var dz = block.Z + 0.5f - player.Position.Z;
//                var dy = block.Y + 0.5f - player.Position.Y;
//                var distanceSq = dx * dx + dy * dy + dz * dz;
//                var radiusSq = player.Radius * player.Radius;

//                if (distanceSq < radiusSq)
//                {
//                    var overlap = player.Radius - (float)Math.Sqrt(distanceSq);
//                    var normal = new Vector3(dx, dy, dz) / (float)Math.Sqrt(distanceSq);
//                    collisions.Add(new Collision(block, new Vector3(block.X + 0.5f, block.Y + 0.5f, block.Z + 0.5f), normal, overlap));
//                }
//            }

//            return collisions;
//        }

//        private void ResolveCollisions(List<Collision> collisions, Player player)
//        {
//            foreach (var collision in collisions)
//            {
//                player.Position += collision.Normal * collision.Overlap;
//                var velocityAdjustment = Vector3.Dot(player.Velocity, collision.Normal) * collision.Normal;
//                player.Velocity -= velocityAdjustment;

//                if (collision.Normal.Y > 0) // Assuming positive Y is up
//                {
//                    player.IsOnGround = true;
//                }
//            }
//        }

//        private void AddCollisionHelper(HelperBlock block)
//        {
//            // Visualization or debugging aid for collision blocks
//            Helpers.Add(block);
//        }
//    }

//    public class HelperBlock
//    {
//        public int X, Y, Z;
//        public HelperBlock(int x, int y, int z) { X = x; Y = y; Z = z; }
//        public HelperBlock(Vector3 block) { X = (int)block.X; Y = (int)block.Y; Z = (int)block.Z; }
//    }

//    public class Collision
//    {
//        public Vector3 Block;
//        public Vector3 ContactPoint, Normal;
//        public float Overlap;
//        public Collision(Vector3 block, Vector3 contactPoint, Vector3 normal, float overlap)
//        {
//            Block = block;
//            ContactPoint = contactPoint;
//            Normal = normal;
//            Overlap = overlap;
//        }
//    }


//}
