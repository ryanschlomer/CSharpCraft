using Microsoft.AspNetCore.SignalR;
using System;
using System.Numerics;
using System.Threading;

//This class might not be used

namespace CSharpCraft.Clases
{
    // Scoped service to handle user-specific game state
    public class GameStateService
    {
        public IHubContext<GameHub> _gameHubContext;
        public ChunkManager _chunkManager { get; }

        // public Vector3 CameraPosition { get; set; } //maybe start here and remove this. and to see why the camera isn't starting in the right place

        public (int, int) PreviousChunkCoords { get; set; } = (int.MaxValue, int.MaxValue);
        public Dictionary<string, object> PlayerSettings { get; } = new Dictionary<string, object>();

        public Player player { get; set; } = new Player();



        public GameStateService(IHubContext<GameHub> gameHubContext, ChunkManager chunkManager)
        {
            _gameHubContext = gameHubContext;
            _chunkManager = chunkManager;
        }

        //public async Task<bool> UpdateCameraPosition(Vector3 deltas, Vector3 direction)
        //{
        //    player.DeltaMovement = deltas;
        //    player.Direction = direction;

        //    // Define the up vector
        //    Vector3 up = new Vector3(0, 1, 0);
        //    // Calculate the right vector
        //    Vector3 right = Vector3.Normalize(Vector3.Cross(direction, up));

        //    // Perform collision detection before updating the camera position

        //    //I am sure the same movement logic below will need to be apply to the collision detection temp movement
        //    bool collisionDetected = CheckForCollision(player);

        //    // If no collision, update camera position and player position

        //    if (!collisionDetected)
        //    {
        //        // Normalize the direction vector to ensure consistent movement speed
        //        direction = Vector3.Normalize(direction);
        //        // Calculate the movement vector:
        //        // - `deltas.Z` controls forward/backward movement
        //        // - `deltas.X` controls left/right movement
        //        // - `deltas.Y` controls vertical movement (unaffected by direction in this context)
        //        Vector3 movementVector = direction * deltas.Z + right * deltas.X + up * deltas.Y;


        //        // Update the player's position
        //        player.Position += movementVector;
        //    }

        //    return collisionDetected;
        //}

        //private bool CheckForCollision(Player player)
        //{
        //    Vector3 collisionResponse = Vector3.Zero;
        //    bool collisionDetected = false;

        //    Vector3 tempPosition = player.GetTempPosition();
        //    BoundingBox playerBox = new BoundingBox(
        //        new Vector3(tempPosition.X - player.Radius, tempPosition.Y, tempPosition.Z - player.Radius),
        //        new Vector3(tempPosition.X + player.Radius, tempPosition.Y + player.Height, tempPosition.Z + player.Radius)
        //    );

        //    int minX = (int)Math.Floor(tempPosition.X - player.Radius);
        //    int maxX = (int)Math.Ceiling(tempPosition.X + player.Radius);
        //    int minY = (int)Math.Floor(tempPosition.Y);
        //    int maxY = (int)Math.Ceiling(tempPosition.Y + player.Height);
        //    int minZ = (int)Math.Floor(tempPosition.Z - player.Radius);
        //    int maxZ = (int)Math.Ceiling(tempPosition.Z + player.Radius);

        //    for (int x = minX - 1; x <= maxX + 1; x++)
        //    {
        //        for (int y = minY - 1; y <= maxY + 1; y++)
        //        {
        //            for (int z = minZ - 1; z <= maxZ + 1; z++)
        //            {
        //                if (_chunkManager.chunkService.IsBlockSolid(x, y, z))
        //                {
        //                    BoundingBox blockBox = new BoundingBox(
        //                        new Vector3(x, y, z),
        //                        new Vector3(x + 1, y + 1, z + 1)
        //                    );
        //                    if (playerBox.Intersects(blockBox))
        //                    {
        //                        collisionDetected = true;
        //                        // Calculate a simple response vector; this should ideally be based on collision normals and penetration depth
        //                        collisionResponse += CalculateCollisionResponse(playerBox, blockBox);

        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return collisionDetected; // No collision
        //}

        //private Vector3 CalculateCollisionResponse(BoundingBox playerBox, BoundingBox blockBox)
        //{
        //    // This is a placeholder function. You'd include logic here to calculate the correct response vector
        //    // based on the direction and depth of the collision.
        //    Vector3 response = new Vector3(0, 0, 0);

        //    // Example: if the block is directly above, push down slightly
        //    if (blockBox.Min.Y >= playerBox.Max.Y)
        //    {
        //        float overlap = playerBox.Max.Y - blockBox.Min.Y;
        //        response.Y -= overlap;
        //    }
        //    // Additional conditions for other directions

        //    return response;
        //}




        public void UpdatePlayerSetting(string key, object value)
        {
            if (PlayerSettings.ContainsKey(key))
            {
                PlayerSettings[key] = value;
            }
            else
            {
                PlayerSettings.Add(key, value);
            }
        }

    }

    public class PlayerProfile
    {
        public string Username { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        // Additional player attributes
    }

}
