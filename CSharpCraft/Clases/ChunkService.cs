using System.Collections.Generic;
using System.Numerics;
using DotnetNoise;
using static System.Reflection.Metadata.BlobBuilder;


namespace CSharpCraft.Clases
{


    public class ChunkService
    {
        private Dictionary<(int, int), Chunk> loadedChunks;
        public static int MaxChunkSize { get; } = 16;  // Standard size for simplicity
        public static int MaxChunkHeight { get; } = 64;  // Height for each chunk

        // Create a noise generator
        public static FastNoise Noise { get; set; }
        public static int Seed { get; set; }
        

        public ChunkService()
        {
            InitializeNoise();

            loadedChunks = new Dictionary<(int, int), Chunk>();
        }

        public static void InitializeNoise()
        {
            Seed = 12;
            Noise = new FastNoise(Seed);
            Noise.Frequency = 0.009f; // Adjust frequency to change the scale of terrain features
            Noise.UsedNoiseType = FastNoise.NoiseType.PerlinFractal;
            Noise.Octaves = 5; // More octaves for more detail
            Noise.Lacunarity = 2.0f;
            Noise.Gain = 0.5f;
        }

        public static int GetChunkCoordinate(float position, int size)
        {
            // Adjust position downwards if negative before performing integer division
            return (int)Math.Floor(position / size);
        }

        public IEnumerable<Chunk> GetChunksForCamera(CameraInfo camera)
        {
            List<Chunk> visibleChunks = new List<Chunk>();
            int cameraChunkX = GetChunkCoordinate(camera.Position.X, MaxChunkSize);
            int cameraChunkZ = GetChunkCoordinate(camera.Position.Z, MaxChunkSize);

            Console.WriteLine($"Camera Position: {camera.Position.X}, {camera.Position.Z}");
            Console.WriteLine($"Chunk Coordinates: {cameraChunkX}, {cameraChunkZ}");

            for (int x = cameraChunkX - 2; x <= cameraChunkX + 2; x++)
            {
                for (int z = cameraChunkZ - 2; z <= cameraChunkZ + 2; z++)
                {
                    if (loadedChunks.TryGetValue((x, z), out Chunk chunk))
                    {
                        visibleChunks.Add(chunk);
                    }
                    else
                    {
                        Console.WriteLine($"Chunk at {x}, {z} not found");
                    }
                }
            }

            return visibleChunks;
        }




        public Vector3Int FindSpawnPoint()
        {
            // Assuming each chunk is loaded and accessible via loadedChunks
            foreach (var chunk in loadedChunks.Values)
            {
                for (int x = 0; x < MaxChunkSize; x++)
                {
                    for (int z = 0; z < MaxChunkSize; z++)
                    {
                        for (int y = 0; y < MaxChunkHeight; y++)
                        {
                            Block block = chunk.GetBlock(x, y, z);
                            if (block != null)
                                if (block.Type == BlockType.Grass)
                                {
                                    while (block != null)
                                    {
                                        block = chunk.GetBlock(x, ++y, z);

                                    }
                                    return new Vector3Int(x, y + 1, z);
                                }
                                }
                    }
                }
            }

            // Default spawn point if none found
            return new Vector3Int(0, 0, 0);
        }


        private bool IsSuitableSpawnPoint()
        {
            return true;
        }




        public async Task<Chunk> GetOrCreateChunkAsync(int chunkX, int chunkZ)
        {
            if (!loadedChunks.TryGetValue((chunkX, chunkZ), out Chunk chunk))
            {
                chunk = await GenerateChunkAsync(chunkX, chunkZ);
                loadedChunks.Add((chunkX, chunkZ), chunk);
            }
            return chunk;
        }

        private async Task<Chunk> GenerateChunkAsync(int chunkX, int chunkZ)
        {
            // Generate blocks for the chunk
            Block[,,] blocks = new Block[MaxChunkSize, MaxChunkHeight, MaxChunkSize];
            Random random = new Random();

            int preHeight = -1; // Previous height, initially undefined
            int preHeightDelta = 0; // Previous height change
            double percentChange = 0.01; // Chance of changing height
            int[,] heights = new int[MaxChunkSize, MaxChunkSize];


            for (int x = 0; x < MaxChunkSize; x++)
            {
                for (int z = 0; z < MaxChunkSize; z++)
                {
                    // Calculate global coordinates
                    int globalX = chunkX * MaxChunkSize + x;
                    int globalZ = chunkZ * MaxChunkSize + z;

                    // Generate noise at these coordinates
                    float heightNoise = Noise.GetNoise(globalX / 0.5f, globalZ / 0.5f);
                    int height = Math.Clamp((int)(Math.Pow((heightNoise + 1) / 2, 2) * MaxChunkHeight), 0, MaxChunkHeight - 1);


                    // Set blocks in the chunk
                    for (int y = 0; y < height; y++)
                    {
                        // If y is 10 or higher, set as Stone, otherwise set as Dirt or Grass depending on height
                        if (y >= 25)
                        {
                            blocks[x, y, z] = new Block(BlockType.Stone, globalX, y, globalZ);
                        }
                        else
                        {
                            blocks[x, y, z] = (y < height - 1) ? new Block(BlockType.Dirt, globalX, y, globalZ) : new Block(BlockType.Grass, globalX, y, globalZ);
                        }
                    }
                }
            }




            // Add rock outcroppings before trees
            double rockProbability = 0.05;  // Chance of a rock outcropping in a given chunk
            if (random.NextDouble() < rockProbability)
            {
                // Calculate the center position in global coordinates
                int centerX = random.Next(3, MaxChunkSize - 3) + chunkX * MaxChunkSize;
                int centerZ = random.Next(3, MaxChunkSize - 3) + chunkZ * MaxChunkSize;
                int baseHeight = 0;  // Finding the height at the global center position

                // Convert global coordinates back to local coordinates for accessing the block array
                int localX = centerX % MaxChunkSize;
                int localZ = centerZ % MaxChunkSize;

                // Ensure local coordinates are within bounds (should always be true by construction)
                localX = Math.Clamp(localX, 0, MaxChunkSize - 1);
                localZ = Math.Clamp(localZ, 0, MaxChunkSize - 1);

                while (baseHeight < MaxChunkHeight && blocks[localX, baseHeight, localZ] != null)
                {
                    baseHeight++;
                }

                int radius = random.Next(2, 5);  // Random radius of rock outcropping
                int heightIncrease = random.Next(2, 4);  // Height variation of the rock

                // Generate a roughly spherical rock outcropping using global coordinates
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int z = centerZ - radius; z <= centerZ + radius; z++)
                    {
                        for (int y = baseHeight; y <= baseHeight + heightIncrease; y++)
                        {
                            int localBlockX = x % MaxChunkSize;
                            int localBlockZ = z % MaxChunkSize;

                            // Check bounds within the chunk
                            if (localBlockX >= 0 && localBlockX < MaxChunkSize && localBlockZ >= 0 && localBlockZ < MaxChunkSize && y < MaxChunkHeight)
                            {
                                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2) + Math.Pow(y - baseHeight, 2));
                                if (distance <= radius)
                                {
                                    blocks[localBlockX, y, localBlockZ] = new Block(BlockType.Stone, x, y, z); // Set block type to Stone
                                }
                            }
                        }
                    }
                }
            }


            double treeProbability = 0.01;

            // Tree generation after terrain
            for (int x = 1; x < MaxChunkSize - 1; x++)
            {
                for (int z = 1; z < MaxChunkSize - 1; z++)
                {
                    // Calculate global coordinates
                    int globalX = chunkX * MaxChunkSize + x;
                    int globalZ = chunkZ * MaxChunkSize + z;

                    if (random.NextDouble() < treeProbability) // Check if a tree should be generated here
                    {
                        int baseHeight = FindTopBlockAt(blocks, x, z); // Use the function to find the top block locally

                        if (baseHeight >= 0 && blocks[x, baseHeight, z].Type == BlockType.Grass) // Ensure the top block is grass
                        {
                            if (baseHeight + 5 < MaxChunkHeight) // Ensure there's enough space for the tree
                            {
                                // Create trunk
                                for (int y = baseHeight + 1; y <= baseHeight + 5; y++)
                                {
                                    blocks[x, y, z] = new Block(BlockType.Trunk, globalX, y, globalZ); // Trunk blocks with global coordinates
                                }

                                // Create a simple leaf canopy above the trunk
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        for (int dy = 4; dy <= 6; dy++)
                                        {
                                            int leafX = x + dx;
                                            int leafZ = z + dz;
                                            if (leafX >= 0 && leafX < MaxChunkSize && leafZ >= 0 && leafZ < MaxChunkSize && (baseHeight + dy) < MaxChunkHeight)
                                            {
                                                blocks[leafX, baseHeight + dy, leafZ] = new Block(BlockType.Leaves, globalX + dx, baseHeight + dy, globalZ + dz); // Leaf blocks with global coordinates
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }




            // Initialize the chunk with the generated blocks
            Chunk chunk = new Chunk(chunkX, chunkZ, MaxChunkSize, MaxChunkHeight);
            chunk.SetBlocks(blocks);

            return chunk;
        }

        public int FindTopBlockAt(Block[,,] blocks, int x, int z)
        {
            int topBlockY = -1; // Start with -1 to indicate no block found yet
            for (int y = 0; y < MaxChunkHeight; y++)
            {
                if (blocks[x, y, z] != null)
                {
                    topBlockY = y; // Update topBlockY to the latest non-null block
                }
            }
            return topBlockY;
        }




        public async Task LoadChunksAroundAsync(int centerX, int centerZ, int radius)
        {
            List<Task> loadingTasks = new List<Task>();
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    loadingTasks.Add(GetOrCreateChunkAsync(x, z)); // Use GetOrCreateChunkAsync instead of direct creation
                }
            }
            await Task.WhenAll(loadingTasks);
        }

        //Can we create methods to load and unload chunks and then call a javascript method in the razor page
        //to send the info to javascript?
    }
}
