using CSharpCraft.Clases;
using System;
using System.Collections.Generic;
using System.Numerics;

public class Physics
{
    private float Gravity = 32f; // Acceleration due to gravity
    private float SimulationRate = 250f; // Physics updates per second
    private float StepSize => 1 / SimulationRate;
    private float Accumulator = 0f; // Accumulator to keep track of leftover dt
    private readonly ChunkService ChunkService;

    public Physics(ChunkService chunkService)
    {
        ChunkService = chunkService;
    }

    public void HandlePlayerInput(Player player, Vector3 movementDelta, Vector3 lookDirection, float dt)
    {
        Accumulator += dt;
        while (Accumulator >= StepSize)
        {
            // Apply gravity first
            ApplyGravity(player, StepSize);

            // Process movement input
            ProcessInput(player, movementDelta, lookDirection, StepSize);

            // Detect and resolve any collisions
            DetectCollisions(player);

            Accumulator -= StepSize;
        }
    }

    private void ApplyGravity(Player player, float stepSize)
    {
        // Apply gravity to the vertical velocity
        float newVelocityY = player.Velocity.Y - Gravity * stepSize;

        // Ensure the new vertical velocity does not exceed a maximum value
        // Assuming -1 is the maximum downward speed (negative because it's downward)
        if (newVelocityY < -10.0f)
        {
            newVelocityY = -10.0f;
        }

        // Update the player's velocity with the potentially clamped vertical velocity
        player.Velocity = new Vector3(player.Velocity.X, newVelocityY, player.Velocity.Z);
    }

    private void ProcessInput(Player player, Vector3 movementDelta, Vector3 lookDirection, float stepSize)
    {
        // Define the world up vector
        Vector3 worldUp = new Vector3(0, 1, 0);
        Vector3 right = Vector3.Cross(worldUp, lookDirection);
        right = Vector3.Normalize(right);
        Vector3 forward = Vector3.Normalize(lookDirection);

        // Calculate the movement vector based on current input
        Vector3 currentMovement = (forward * movementDelta.Z + right * -movementDelta.X) * player.CurrentSpeed;
        Console.WriteLine(player.CurrentSpeed);
        Console.WriteLine(currentMovement);

        // If deltaY is 1, treat it as a jump request
        if (movementDelta.Y == 1 && player.IsOnGround)
        {
            float jumpVelocity = CalculateJumpVelocity(player);

            // Jump, but maintain existing horizontal momentum
            player.Velocity = new Vector3(player.Velocity.X, jumpVelocity, player.Velocity.Z);
            player.IsOnGround = false;
        }

        // Apply lateral movement only if on the ground or when auto-jumping
        if (player.IsOnGround || player.AutoJumping)
        {
            // Apply new movement inputs
            player.Velocity = new Vector3(currentMovement.X, player.Velocity.Y, currentMovement.Z);
        }

        // Update player's position
        player.Position += player.Velocity * stepSize;
    }

    private float CalculateJumpVelocity(Player player)
    {
        // Using the formula v = sqrt(2 * g * h)
        return (float)Math.Sqrt(2 * Gravity * player.CurrentJumpHeight);
    }

    private void DetectCollisions(Player player)
    {
        player.IsOnGround = false;
        var candidates = BroadPhase(player);
        var collisions = NarrowPhase(candidates, player);

        if (collisions.Count > 0)
        {
            ResolveCollisions(collisions, player);
        }
    }

    private List<Vector3> BroadPhase(Player player)
    {
        var candidates = new List<Vector3>();

        int minX = (int)Math.Floor(player.Position.X - player.Radius);
        int maxX = (int)Math.Ceiling(player.Position.X + player.Radius);
        int minY = (int)Math.Floor(player.Position.Y);
        int maxY = (int)Math.Ceiling(player.Position.Y + player.Height);
        int minZ = (int)Math.Floor(player.Position.Z - player.Radius);
        int maxZ = (int)Math.Ceiling(player.Position.Z + player.Radius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (ChunkService.IsBlockSolid(x, y, z))
                    {
                        candidates.Add(new Vector3(x, y, z));
                    }
                }
            }
        }
        return candidates;
    }

    public List<Collision> NarrowPhase(List<Vector3> candidates, Player player)
    {
        var collisions = new List<Collision>();

        foreach (var block in candidates)
        {
            var closestPoint = new Vector3(
                Math.Max(block.X, Math.Min(player.Position.X, block.X + 1)),
                Math.Max(block.Y, Math.Min(player.Position.Y + (player.Height / 2), block.Y + 1)),
                Math.Max(block.Z, Math.Min(player.Position.Z, block.Z + 1))
            );

            float dx = closestPoint.X - player.Position.X;
            float dy = closestPoint.Y - (player.Position.Y + (player.Height / 2));
            float dz = closestPoint.Z - player.Position.Z;

            if (PointInPlayerBoundingCylinder(closestPoint, player))
            {
                float overlapY = (player.Height / 2) - Math.Abs(dy);
                float overlapXZ = player.Radius - (float)Math.Sqrt(dx * dx + dz * dz);

                Vector3 normal;
                float overlap;
                if (overlapY < overlapXZ)
                {
                    normal = new Vector3(0, -Math.Sign(dy), 0);
                    overlap = overlapY;
                    player.IsOnGround = true;
                }
                else
                {
                    if (dx == 0 && dz == 0)
                    {
                        if (Math.Abs(dy) < player.Height / 2)
                        {
                            normal = new Vector3(0, -Math.Sign(dy), 0);
                            overlap = player.Height / 2 - Math.Abs(dy);
                            player.IsOnGround = dy > 0;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        normal = new Vector3(-dx, 0, -dz);
                        normal = Vector3.Normalize(normal);
                    }
                    overlap = overlapXZ;
                }

                // Auto-jump logic when hitting a block with no block above
                if (overlapXZ > 0 && dy == 0 && player.IsOnGround)
                {
                    var aboveBlock = new Vector3(block.X, block.Y + 1, block.Z);
                    if (!ChunkService.IsBlockSolid((int)aboveBlock.X, (int)aboveBlock.Y, (int)aboveBlock.Z))
                    {
                        float jumpVelocity = CalculateJumpVelocity(player);
                        player.Velocity = new Vector3(player.Velocity.X, jumpVelocity, player.Velocity.Z);
                        player.IsOnGround = false;
                        player.AutoJumping = true; // Set AutoJumping flag
                    }
                }

                collisions.Add(new Collision(block, closestPoint, normal, overlap));
            }
        }
        return collisions;
    }

    private bool PointInPlayerBoundingCylinder(Vector3 point, Player player)
    {
        float dx = point.X - player.Position.X;
        float dz = point.Z - player.Position.Z;
        float distanceXZ = (float)Math.Sqrt(dx * dx + dz * dz);

        bool isWithinHorizontalBounds = distanceXZ <= player.Radius;

        float lowerBoundY = player.Position.Y;
        float upperBoundY = player.Position.Y + player.Height;
        bool isWithinVerticalBounds = (point.Y >= lowerBoundY) && (point.Y <= upperBoundY);

        return isWithinHorizontalBounds && isWithinVerticalBounds;
    }

    private void ResolveCollisions(List<Collision> collisions, Player player)
    {
        foreach (var collision in collisions)
        {
            if (collision.Normal.Y < 0) // Collision from above
            {
                float neededAdjustment = Math.Min(collision.Overlap, player.Velocity.Y);
                player.Velocity = new Vector3(player.Velocity.X, neededAdjustment, player.Velocity.Z);
            }

            Vector3 deltaPosition = collision.Normal * collision.Overlap;
            player.Position += deltaPosition; // This might be cumulatively too much
            float velocityAdjustment = Vector3.Dot(player.Velocity, collision.Normal);
            player.Velocity -= collision.Normal * velocityAdjustment;

            if (collision.Normal.Y > 0)
                player.IsOnGround = true;
        }

        // Reset AutoJumping flag after resolving collisions
        player.AutoJumping = false;
    }
}

public class Collision
{
    public Vector3 Block { get; set; }
    public Vector3 ContactPoint { get; set; }
    public Vector3 Normal { get; set; }
    public float Overlap { get; set; }

    public Collision(Vector3 block, Vector3 contactPoint, Vector3 normal, float overlap)
    {
        Block = block;
        ContactPoint = contactPoint;
        Normal = normal;
        Overlap = overlap;
    }
}
