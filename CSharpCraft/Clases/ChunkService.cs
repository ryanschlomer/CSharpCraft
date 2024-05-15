using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using DotnetNoise;
using Microsoft.JSInterop;
using static System.Reflection.Metadata.BlobBuilder;


namespace CSharpCraft.Clases
{


    public class ChunkService
    {
        public ConcurrentDictionary<(int, int), Chunk> LoadedChunks { get; private set; } = new ConcurrentDictionary<(int, int), Chunk>();


        // Create a noise generator
        public static FastNoise Noise { get; set; }
        public static int Seed { get; set; }

        // Initialize a semaphore to control concurrent chunk generation
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public ChunkService()
        {
            InitializeNoise();

           // LoadedChunks = new Dictionary<(int, int), Chunk>();
        }

        public static void InitializeNoise()
        {
            Seed = 12;
            Noise = new FastNoise(Seed);
            Noise.Frequency = 0.005f; // Adjust frequency to change the scale of terrain features
            Noise.UsedNoiseType = FastNoise.NoiseType.PerlinFractal;
            Noise.Octaves = 5; // More octaves for more detail
            Noise.Lacunarity = 2.0f;
            Noise.Gain = 0.5f;
        }



        public Vector3 FindSpawnPoint()
        {
            // Assuming each chunk is loaded and accessible via loadedChunks
            foreach (var chunk in LoadedChunks.Values)
            {
                for (int x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        for (int y = 0; y < VoxelData.ChunkHeight; y++)
                        {
                            Vector3 pos = chunk.GetTopSolidBlock(new Vector3(x, y, z));
                            if(pos != Vector3.Zero)
                            {
                                pos = new Vector3(pos.X + .5f, pos.Y+10.0f, pos.Z + .5f);
                                return chunk.ConvertLocalCoordsToGlobalCoords(pos);
                            }
                        }
                    }
                }
            }

            // Default spawn point if none found
            return new Vector3(0, 0, 0);
        }


        private bool IsSuitableSpawnPoint()
        {
            return true;
        }


        public async Task<Chunk> GetOrCreateChunkAsync(int chunkX, int chunkZ)
        {
            // Check if the chunk is already loaded
            if (LoadedChunks.TryGetValue((chunkX, chunkZ), out Chunk chunk))
            {
                // Return the chunk if it's already loaded
                return chunk;
            }

            // Generate the chunk asynchronously
            chunk = await GenerateChunkAsync(chunkX, chunkZ);

            // Add the fully loaded chunk to the ConcurrentDictionary
            LoadedChunks[(chunkX, chunkZ)] = chunk;

            // Return the fully loaded chunk
            return chunk;
        }

        //public async Task<Chunk> GetOrCreateChunkAsync(int chunkX, int chunkZ)
        //{
        //    if (!loadedChunks.TryGetValue((chunkX, chunkZ), out Chunk chunk))
        //    {
        //        chunk = await GenerateChunkAsync(chunkX, chunkZ);
        //        loadedChunks.Add((chunkX, chunkZ), chunk);
        //    }
        //    return chunk;
        //}

        private byte[,,] GenerateResources(int chunkX, int chunkZ, byte[,,] blocks)
        {
            bool showOnlyResources = false;
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    // Calculate global coordinates
                    int globalX = chunkX * VoxelData.ChunkWidth + x;
                    int globalZ = chunkZ * VoxelData.ChunkWidth + z;

                    // Set blocks in the chunk
                    for (int y = 0; y < VoxelData.ChunkHeight; y++)
                    {
                        // Generate noise at these coordinates
                        float stoneNoise = Noise.GetNoise(globalX /VoxelData.BlockTypes[2].Scale.X , y / VoxelData.BlockTypes[2].Scale.Y, globalZ / VoxelData.BlockTypes[2].Scale.Z);

                        //TODO: Add a for loop to wrap these 3 for loops when we add I generating other resources.
                        BlockType block = VoxelData.BlockTypes[blocks[x, y, z]];
                        if(block.IsSolid && stoneNoise > VoxelData.BlockTypes[2].Scarcity)
                        {//set to stone
                            blocks[x, y, z] = 2;
                        }
                        else if(showOnlyResources)
                        {//set these to air so that we can overwrite all the other block types to see resources
                            blocks[x, y, z] = 0;
                        }

                    }
                }
            }
            return blocks;
        }

        private async Task<Chunk> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            // Generate blocks for the chunk
            byte[,,] blocks = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
            Random random = new Random();

            int preHeight = -1; // Previous height, initially undefined
            int preHeightDelta = 0; // Previous height change
            double percentChange = 0.01; // Chance of changing height
            int[,] heights = new int[VoxelData.ChunkWidth, VoxelData.ChunkWidth];


            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    // Calculate global coordinates
                    int globalX = chunkX * VoxelData.ChunkWidth + x;
                    int globalZ = chunkZ * VoxelData.ChunkWidth + z;

                    // Generate noise at these coordinates
                    float heightNoise = Noise.GetNoise(globalX / 0.5f, globalZ / 0.5f);
                    int height = Math.Clamp((int)(Math.Pow((heightNoise + 1) / 2, 2) * VoxelData.ChunkHeight), 0, VoxelData.ChunkHeight - 1);


                    // Set blocks in the chunk
                    for (int y = 0; y < VoxelData.ChunkHeight; y++)
                    {
                        if (y == 0)
                        {
                            // Set blocks of type 1 up to the height determined by the perlin noise
                            blocks[x, y, z] = 2;//stone
                        }
                        else if (y < height - 6)
                        {
                            blocks[x, y, z] = 2;//stone
                        }
                        else if (y < height-3)
                        {
                            // Set blocks of type 1 up to the height determined by the perlin noise
                            blocks[x, y, z] = 1; //dirt eventually
                        }
 
                        else if (y < height)
                        {
                            // Set blocks of type 1 up to the height determined by the perlin noise
                            blocks[x, y, z] = 1; //grass
                        }
                        else
                        {
                            // Set all other blocks above that height to type 0
                            blocks[x, y, z] = 0; //air
                        }
                    }
                }
            }

            // After your existing nested loops for setting blocks in the chunk
          




            blocks = GenerateResources(chunkX, chunkZ, blocks);

            //create a test building
            //if (chunkX == 0 && chunkZ == 0) // Check if it's the chunk (0, 0) where you want to build
            {
                Vector3 c1 = new Vector3(1, 18, 1);
                Vector3 c2 = new Vector3(10, 25, 10);
                blocks = CreateRectangularPrism(blocks, c1, c2, 3);

                Vector3 c4 = c1 + new Vector3(1, 1, 1);
                Vector3 c3 = c2 + new Vector3(-1, -1, -1);
                blocks = CreateRectangularPrism(blocks, c3, c4, 0);

                // Assuming the door should be on the front face, calculate the middle position for the door
                int doorX = (int)((c1.X + c2.X) / 2); // middle of x plane
                int doorZ = (int)c1.Z; //door on Zplane of c1 vector
                int doorY = (int)c1.Y + 1;

                blocks[doorX, doorY, doorZ] = 0; // Set the first block of the door to air
                blocks[doorX, doorY + 1, doorZ] = 0; // Set the second block of the door to air
                                                     //hole on top

                doorX = (int)((c1.X + c2.X) / 2); // middle of x plane
                doorZ = (int)((c1.Z + c2.X) / 2);
                doorY = (int)c2.Y;


                blocks[doorX, doorY, doorZ] = 0; // Set the first block of the door to air
                blocks[doorX, doorY + 1, doorZ] = 0; // Set the second block of the door to air

                //set furnace
                int fX = (int)(c1.X+1); // middle of x plane
                int fZ = (int)(c1.Z+1); //door on Zplane of c1 vector
                int fY = (int)c1.Y + 1;
                blocks[fX, fY, fZ] = 5;
            }
            if (chunkX == 0 && chunkZ == 0) // Check if it's the chunk (0, 0) where you want to build

            {
                Vector3 c1 = new Vector3(3, 30, 3);
                Vector3 c2 = new Vector3(7, 36, 10);
                blocks = CreateRectangularPrism(blocks, c1, c2, 4);

                Vector3 c4 = c1 + new Vector3(1, 1, 1);
                Vector3 c3 = c2 + new Vector3(-1, -1, -1);
                blocks = CreateRectangularPrism(blocks, c3, c4, 0);

                // Assuming the door should be on the front face, calculate the middle position for the door
                int doorX = (int)((c1.X + c2.X) / 2); // middle of x plane
                int doorZ = (int)c1.Z; //door on Zplane of c1 vector
                int doorY = (int)c1.Y + 1;

                blocks[doorX, doorY, doorZ] = 0; // Set the first block of the door to air
                blocks[doorX, doorY + 1, doorZ] = 0; // Set the second block of the door to air
                                                     //hole on top


                doorX = (int)((c1.X + c2.X) / 2); // middle of x plane
                doorZ = (int)((c1.Z + c2.X) / 2);
                doorY = (int)c2.Y;


                blocks[doorX, doorY, doorZ] = 0; // Set the first block of the door to air
                blocks[doorX, doorY + 1, doorZ] = 0; // Set the second block of the door to air
            }


            #region trees and rocks
            //// Add rock outcroppings before trees
            //double rockProbability = 0.05;  // Chance of a rock outcropping in a given chunk
            //if (random.NextDouble() < rockProbability)
            //{
            //    // Calculate the center position in global coordinates
            //    int centerX = random.Next(3, MaxChunkSize - 3) + chunkX * MaxChunkSize;
            //    int centerZ = random.Next(3, MaxChunkSize - 3) + chunkZ * MaxChunkSize;
            //    int baseHeight = 0;  // Finding the height at the global center position

            //    // Convert global coordinates back to local coordinates for accessing the block array
            //    int localX = centerX % MaxChunkSize;
            //    int localZ = centerZ % MaxChunkSize;

            //    // Ensure local coordinates are within bounds (should always be true by construction)
            //    localX = Math.Clamp(localX, 0, MaxChunkSize - 1);
            //    localZ = Math.Clamp(localZ, 0, MaxChunkSize - 1);

            //    while (baseHeight < MaxChunkHeight && blocks[localX, baseHeight, localZ] != null)
            //    {
            //        baseHeight++;
            //    }

            //    int radius = random.Next(2, 5);  // Random radius of rock outcropping
            //    int heightIncrease = random.Next(2, 4);  // Height variation of the rock

            //    // Generate a roughly spherical rock outcropping using global coordinates
            //    for (int x = centerX - radius; x <= centerX + radius; x++)
            //    {
            //        for (int z = centerZ - radius; z <= centerZ + radius; z++)
            //        {
            //            for (int y = baseHeight; y <= baseHeight + heightIncrease; y++)
            //            {
            //                int localBlockX = x % MaxChunkSize;
            //                int localBlockZ = z % MaxChunkSize;

            //                // Check bounds within the chunk
            //                if (localBlockX >= 0 && localBlockX < MaxChunkSize && localBlockZ >= 0 && localBlockZ < MaxChunkSize && y < MaxChunkHeight)
            //                {
            //                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2) + Math.Pow(y - baseHeight, 2));
            //                    if (distance <= radius)
            //                    {
            //                        blocks[localBlockX, y, localBlockZ] = new Block(BlockType.Stone, x, y, z); // Set block type to Stone
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}


            //double treeProbability = 0.01;

            //// Tree generation after terrain
            //for (int x = 1; x < MaxChunkSize - 1; x++)
            //{
            //    for (int z = 1; z < MaxChunkSize - 1; z++)
            //    {
            //        // Calculate global coordinates
            //        int globalX = chunkX * MaxChunkSize + x;
            //        int globalZ = chunkZ * MaxChunkSize + z;

            //        if (random.NextDouble() < treeProbability) // Check if a tree should be generated here
            //        {
            //            int baseHeight = FindTopBlockAt(blocks, x, z); // Use the function to find the top block locally

            //            if (baseHeight >= 0 && blocks[x, baseHeight, z].Type == BlockType.Grass) // Ensure the top block is grass
            //            {
            //                if (baseHeight + 5 < MaxChunkHeight) // Ensure there's enough space for the tree
            //                {
            //                    // Create trunk
            //                    for (int y = baseHeight + 1; y <= baseHeight + 5; y++)
            //                    {
            //                        blocks[x, y, z] = new Block(BlockType.Trunk, globalX, y, globalZ); // Trunk blocks with global coordinates
            //                    }

            //                    // Create a simple leaf canopy above the trunk
            //                    for (int dx = -1; dx <= 1; dx++)
            //                    {
            //                        for (int dz = -1; dz <= 1; dz++)
            //                        {
            //                            for (int dy = 4; dy <= 6; dy++)
            //                            {
            //                                int leafX = x + dx;
            //                                int leafZ = z + dz;
            //                                if (leafX >= 0 && leafX < MaxChunkSize && leafZ >= 0 && leafZ < MaxChunkSize && (baseHeight + dy) < MaxChunkHeight)
            //                                {
            //                                    blocks[leafX, baseHeight + dy, leafZ] = new Block(BlockType.Leaves, globalX + dx, baseHeight + dy, globalZ + dz); // Leaf blocks with global coordinates
            //                                }
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            #endregion


            // Initialize the chunk with the generated blocks
            Chunk chunk = new Chunk(this, chunkX, chunkZ);
            chunk.SetBlocks(blocks);

            return chunk;
        }




        public async Task LoadChunksAroundAsync(int centerX, int centerZ, int radius)
        {
            List<Task> loadingTasks = new List<Task>();
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    loadingTasks.Add(GetOrCreateChunkAsync(x, z));
                }
            }
            await Task.WhenAll(loadingTasks);
        }

        //Can we create methods to load and unload chunks and then call a javascript method in the razor page
        //to send the info to javascript?


        //public static void RemoveBlock(string chunkId, int blockX, int blockY, int blockZ)
        //{
        //    // Find the chunk with the given chunkId
        //    (int x, int z) = ChunkService.ConvertChunkIDToCoordinates(chunkId);

        //    if (loadedChunks.TryGetValue((x, z), out Chunk chunk))
        //    {
        //        // Remove the block at the specified position
        //        chunk.RemoveBlock(blockX, blockY, blockZ);
        //    }
        //}

        public async Task<Chunk> GetChunkAsync(string chunkId)
        {
            // Find the chunk with the given chunkId
            (int x, int z) = ChunkService.ConvertChunkIDToCoordinates(chunkId);

            if (LoadedChunks.TryGetValue((x, z), out Chunk chunk))
            {
                return chunk;
            }
            return null;
        }

        //public static async Task<string> DeleteBlock(string chunkId, int x, int y, int z)
        //{
        //    Chunk chunk = await GetChunkAsync(chunkId);
        //    await chunk.RemoveBlockAsync(x, y, z);

        //    var update = new
        //    {
        //        ChunkId = chunkId,
        //        UpdatedBlock = new { X = x, Y = y, Z = z, Type = BlockType.Air }
        //    };

        //    return JsonSerializer.Serialize(update);
        //}

        public static (int x, int z) ConvertChunkIDToCoordinates(string chunkId)
        {
            // Ensure chunkID is not null or empty
            if (string.IsNullOrEmpty(chunkId))
            {
                throw new ArgumentException("ChunkID cannot be null or empty.");
            }

            // Ensure chunkID has a length of at least 10 characters (+/-00000+/-00000)
            if (chunkId.Length < 10)
            {
                throw new ArgumentException("Invalid ChunkID format.");
            }

            // Extract x and z coordinates from the chunkID
            string xStr = chunkId.Substring(0, 6);
            string zStr = chunkId.Substring(6, 6);

            // Parse the coordinates
            int x = int.Parse(xStr);
            int z = int.Parse(zStr);

            return (x, z);
        }



 

      
        public async Task<bool> IsVoxelExposed(Vector3 globalPos)
        {
            //get the chunk the position is in
            Vector2 chunkPosition = VoxelData.CalculateChunkPosition(globalPos);
            Chunk chunk = await GetOrCreateChunkAsync((int)chunkPosition.X, (int)chunkPosition.Y);
            if (chunk == null) 
                return true;  // Assume exposed if chunk is not loaded

            //convert to local coords and return IsSolid.
            Vector3 localPos = globalPos - new Vector3(chunkPosition.X * VoxelData.ChunkWidth, 0, chunkPosition.Y * VoxelData.ChunkWidth);
            return chunk.IsVoxelSolid(localPos);
           
        }




        internal bool IsBlockSolid(int x, int y, int z)
        {
            Vector2 chunkPosition = VoxelData.CalculateChunkPosition(new Vector3(x, y, z));

            // Convert global coordinates to chunk-relative coordinates
            int chunkX = (int)chunkPosition.X;
            int chunkZ = (int)chunkPosition.Y;
            // Adjust global coordinates to be within chunk boundaries
            //int localX = x < 0 ? VoxelData.ChunkWidth - Math.Abs(x % VoxelData.ChunkWidth) : x % VoxelData.ChunkWidth;
            //int localZ = z < 0 ? VoxelData.ChunkWidth - Math.Abs(z % VoxelData.ChunkWidth) : z % VoxelData.ChunkWidth;

            int localX = x % VoxelData.ChunkWidth;
            int localZ = z % VoxelData.ChunkWidth;
            if (localX < 0) localX += VoxelData.ChunkWidth;
            if (localZ < 0) localZ += VoxelData.ChunkWidth;


            // Check if the chunk containing the block is loaded
            if (LoadedChunks.TryGetValue((chunkX, chunkZ), out Chunk chunk))
            {
                // Check if the block is within the bounds of the chunk
                if (localX >= 0 && localX < VoxelData.ChunkWidth &&
                    y >= 0 && y < VoxelData.ChunkHeight &&
                    localZ >= 0 && localZ < VoxelData.ChunkWidth)
                {
                    // Check if the block at the local coordinates is solid
                    return chunk.IsVoxelSolid(new Vector3(localX, y, localZ));
                }
            }

            // If the chunk is not loaded or the block is out of bounds, assume it's not solid
            return false;
        }

        /// <summary>
        /// This method only works if the blocks are in the current chunk.
        /// It's only used during initial generation for testing.
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="blockType"></param>
        /// <returns></returns>
        public byte[,,] CreateRectangularPrism(byte[,,] blocks, Vector3 start, Vector3 end, byte blockType)
        {
            // Calculate the minimum and maximum bounds to handle any order of corners
            int minX = (int)Math.Min(start.X, end.X);
            int maxX = (int)Math.Max(start.X, end.X);
            int minY = (int)Math.Min(start.Y, end.Y);
            int maxY = (int)Math.Max(start.Y, end.Y);
            int minZ = (int)Math.Min(start.Z, end.Z);
            int maxZ = (int)Math.Max(start.Z, end.Z);

            // Loop through all coordinates within the bounds and set the block type
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        blocks[x, y, z] = blockType;
                    }
                }
            }
            return blocks;
        }

    }
}