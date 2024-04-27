using System.Collections.Generic;
using System.Numerics;
using DotnetNoise;


namespace CSharpCraft.Clases
{


    public class ChunkService
    {
        private Dictionary<(int, int), Chunk> loadedChunks;
        public static int MaxChunkSize { get; } = 16;  // Standard size for simplicity
        public static int MaxChunkHeight { get; } = 32;  // Height for each chunk

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
            Seed = 11;
            Noise = new FastNoise(Seed);
            Noise.Frequency = 0.01f; // Adjust frequency to change the scale of terrain features
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

        public bool HasCameraMovedToNewChunk(Vector3 previousPosition, Vector3 newPosition, out (int, int) newChunk)
        {
            int oldChunkX = GetChunkCoordinate(previousPosition.X, MaxChunkSize);
            int oldChunkZ = GetChunkCoordinate(previousPosition.Z, MaxChunkSize);
            int newChunkX = GetChunkCoordinate(newPosition.X, MaxChunkSize);
            int newChunkZ = GetChunkCoordinate(newPosition.Z, MaxChunkSize);

            newChunk = (newChunkX, newChunkZ);

            // Return true if the chunk coordinates have changed
            return (oldChunkX != newChunkX || oldChunkZ != newChunkZ);
        }
        public IEnumerable<Chunk> GetChunksForCamera(CameraInfo camera)
        {
            List<Chunk> visibleChunks = new List<Chunk>();
            int cameraChunkX = GetChunkCoordinate(camera.Position.X, MaxChunkSize);
            int cameraChunkZ = GetChunkCoordinate(camera.Position.Z, MaxChunkSize);

            Console.WriteLine($"Camera Position: {camera.Position.X}, {camera.Position.Z}");
            Console.WriteLine($"Chunk Coordinates: {cameraChunkX}, {cameraChunkZ}");

            for (int x = cameraChunkX - 1; x <= cameraChunkX + 1; x++)
            {
                for (int z = cameraChunkZ - 1; z <= cameraChunkZ + 1; z++)
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



        private bool IsChunkVisible(Chunk chunk, CameraInfo camera)
        {
            int chunkWorldX = chunk.ChunkX * MaxChunkSize;
            int chunkWorldZ = chunk.ChunkZ * MaxChunkSize;
            double distance = Math.Sqrt(Math.Pow(camera.Position.X - chunkWorldX, 2) + Math.Pow(camera.Position.Z - chunkWorldZ, 2));

            return distance < camera.FarClip; // Using FarClip as the visibility threshold
        }


        public Block GetBlock(int globalX, int globalY, int globalZ)
        {
            Chunk chunk = GetChunkByGlobalCoords(globalX, globalZ);
            return chunk?.GetBlock(globalX % MaxChunkSize, globalY, globalZ % MaxChunkSize);
        }

        public List<Block> GetNeighboringBlocks(int globalX, int globalY, int globalZ)
        {
            List<Block> neighbors = new List<Block>();
            foreach (var offset in Block.NeighborOffsets)
            {
                Block neighbor = GetBlock(globalX + offset[0], globalY + offset[1], globalZ + offset[2]);
                if (neighbor != null)
                    neighbors.Add(neighbor);
            }
            return neighbors;
        }


        public Vector3Int FindSpawnPoint()
        {
            foreach (var chunk in loadedChunks.Values)
            {
                foreach (var (block, index) in chunk.GetAllBlocksWithIndex())
                {
                    if (block != null && IsSuitableSpawnPoint(chunk, block, index))
                    {
                        int x = index % MaxChunkSize;
                        int y = (index / (MaxChunkSize * MaxChunkSize)) % MaxChunkHeight;
                        int z = (index / MaxChunkSize) % MaxChunkSize;
                        return new Vector3Int(x, y + 1, z); // +1 to be on top of the block
                    }
                }
            }
            // Default spawn point if none found
            return new Vector3Int(0, 0, 0);
        }



        private bool IsSuitableSpawnPoint(Chunk chunk, Block block, int index)
        {
            int x = index % MaxChunkSize;
            int y = (index / (MaxChunkSize * MaxChunkSize)) % MaxChunkHeight;
            int z = (index / MaxChunkSize) % MaxChunkSize;

            // Check for blocks directly above the current block
            for (int i = 1; i <= 3; i++)
            {
                if (BlockExistsAt(chunk, x, y + i, z))
                    return false; // There is a block within 3 units above, not suitable
            }
            return true; // Suitable if there are no blocks directly above for 3 blocks
        }


        private bool BlockExistsAt(Chunk chunk, int x, int y, int z)
        {
            if (y >= MaxChunkHeight) return false; // Exceeds chunk height
            Block block = chunk.GetBlock(x, y, z);
            return block != null;
        }

        private Chunk GetChunkByGlobalCoords(int globalX, int globalZ)
        {
            int chunkX = globalX / MaxChunkSize;
            int chunkZ = globalZ / MaxChunkSize;
            return loadedChunks.TryGetValue((chunkX, chunkZ), out Chunk chunk) ? chunk : LoadChunk(chunkX, chunkZ);
        }

        private Chunk LoadChunk(int chunkX, int chunkZ)
        {
            // Load or generate chunk
            return new Chunk(chunkX, chunkZ, MaxChunkSize, MaxChunkHeight);
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

                    //Set blocks in the chunk
                    for (int y = 0; y < height; y++)
                    {
                        blocks[x, y, z] = (y < height - 1) ? new Block(BlockType.Dirt, globalX, y, globalZ) : new Block(BlockType.Grass, globalX, y, globalZ);
                    }

                    //this code doesn;t work...
                    //int yMin = height - 2;
                    //if (yMin < 0) yMin = 0;
                    //for (int y = yMin; y < height; y++)
                    //{
                    //    blocks[x, y, z] = (y < height - 1) ? new Block(4, globalX, y, globalZ) : new Block(1, globalX, y, globalZ);
                    //}
                }
            }




            // Add rock outcroppings before trees
            double rockProbability = 0.05;  // Chance of a rock outcropping in a given chunk
            if (random.NextDouble() < rockProbability)
            {
                int centerX = random.Next(3, MaxChunkSize - 3);
                int centerZ = random.Next(3, MaxChunkSize - 3);
                int baseHeight = 0;  // Finding the height at (centerX, centerZ)
                while (baseHeight < MaxChunkHeight && blocks[centerX, baseHeight, centerZ] != null)
                {
                    baseHeight++;
                }
                int radius = random.Next(2, 5);  // Random radius of rock outcropping
                int heightIncrease = random.Next(2, 4);  // Height variation of the rock

                // Generate a roughly spherical rock outcropping
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int z = centerZ - radius; z <= centerZ + radius; z++)
                    {
                        for (int y = baseHeight; y <= baseHeight + heightIncrease; y++)
                        {
                            if (x >= 0 && x < MaxChunkSize && z >= 0 && z < MaxChunkSize && y < MaxChunkHeight)
                            {
                                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(z - centerZ, 2) + Math.Pow(y - baseHeight, 2));
                                if (distance <= radius)
                                {
                                    blocks[x, y, z] = new Block(BlockType.Stone, x, y, z); // Set block type to 5 (rock)
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
                    if (random.NextDouble() < treeProbability) // Check if a tree should be generated here
                    {
                        // Determine the height at the current column directly
                        int baseHeight = 0;
                        while (baseHeight < MaxChunkHeight && blocks[x, baseHeight, z] != null)
                        {
                            baseHeight++;
                        }
                        baseHeight--; // Adjust to get the top block's position
                        if (blocks[x, baseHeight, z].Type == BlockType.Grass) //block must be grass
                        {
                            if (baseHeight + 5 < MaxChunkSize) // Ensure there's enough space for the tree
                            {
                                // Create trunk (block type 2)
                                for (int y = baseHeight + 1; y <= baseHeight + 5; y++) // Start from baseHeight + 1
                                {
                                    blocks[x, y, z] = new Block(BlockType.Trunk, x, y, z); // trunk blocks
                                }

                                // Create a simple leaf canopy above the trunk
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    for (int dz = -1; dz <= 1; dz++)
                                    {
                                        for (int dy = 4; dy <= 6; dy++)
                                        {
                                            if (x + dx < MaxChunkSize && z + dz < MaxChunkSize)
                                                blocks[x + dx, baseHeight + dy, z + dz] = new Block(BlockType.Leaves, x + dx, baseHeight + dy, z + dz); // leaf blocks
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




        private async Task<Chunk> LoadChunkAsync(int chunkX, int chunkZ)
        {
            // Load or generate chunk asynchronously
            await Task.Delay(100); // Simulate loading delay
            return new Chunk(chunkX, chunkZ, MaxChunkSize, MaxChunkHeight);
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
