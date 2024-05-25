using System;
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


        public async Task<Chunk> GetOrCreateChunkAsync(int chunkX, int chunkZ, bool onlyCreateChunk = false)
        {
            if (!onlyCreateChunk)
            {
                // Check if the chunk is already loaded
                if (LoadedChunks.TryGetValue((chunkX, chunkZ), out Chunk c))
                {
                    // Return the chunk if it's already loaded
                    return c;
                }
            }
            // Generate the chunk asynchronously
            Chunk chunk = await GenerateChunkAsync(chunkX, chunkZ);

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
                            blocks[x, y, z] = 9;//bedrock
                        }
                        else if (y < height - 6)
                        {
                            blocks[x, y, z] = 2;//stone
                        }
                        else if (y < height-1)
                        {
                            // Set blocks of type 1 up to the height determined by the perlin noise
                            blocks[x, y, z] = 6; //dirt 
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
            if (chunkX == 0 && chunkZ == 0) // Check if it's the chunk (0, 0) where you want to build
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

            //Add stone brick box
            if (chunkX == 0 && chunkZ == 0) // Check if it's the chunk (0, 0) where you want to build

            {
                Vector3 c1 = new Vector3(3, 30, 3);
                Vector3 c2 = new Vector3(7, 36, 10);
                blocks = CreateRectangularPrism(blocks, c1, c2, 11);

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


            if (chunkX == 1 && chunkZ == 1)
            {
                MazeGenerator generator = new MazeGenerator();
                int mazeYLevel = 20; // Specify the Y level for the maze
                byte wallBlockType = 10; // Assuming 11 is the wall block type
                byte floorBlockType = 4; // Assuming 12 is the floor block type
                byte airBlockType = 0; // Assuming 0 is air

                blocks = generator.GenerateMazeChunk(blocks, mazeYLevel, wallBlockType, floorBlockType, airBlockType);
            }

            #region trees and rocks



            double treeProbability = 0.01;
            //This needs to change to use noise generation and not random

            //Tree generation after terrain

            for (int x = 1; x < VoxelData.ChunkWidth - 1; x++)
            {
                for (int z = 1; z < VoxelData.ChunkWidth - 1; z++)
                {
                    // Calculate global coordinates
                    int globalX = chunkX * VoxelData.ChunkWidth + x;
                    int globalZ = chunkZ * VoxelData.ChunkWidth + z;

                    if (random.NextDouble() < treeProbability) // Check if a tree should be generated here
                    {
                        int baseHeight = FindTopBlockAt(blocks, x, z); // Use the function to find the top block locally

                        if (baseHeight >= 0 && blocks[x, baseHeight, z] == 1) // Ensure the top block is grass
                        {
                            if (baseHeight + 10 < VoxelData.ChunkHeight) // Ensure there's enough space for the tree
                            {
                                // Create trunk
                                for (int y = baseHeight + 1; y <= baseHeight + 7; y++)
                                {
                                    blocks[x, y, z] = 8;
                                }

                                // Create branches
                                //CreateBranch(blocks, x, baseHeight + 5, z, 2);
                                //CreateBranch(blocks, x, baseHeight + 6, z, 2);
                                //CreateBranch(blocks, x, baseHeight + 7, z, 1);

                                // Create leaf canopy
                                CreateLeafCanopy(blocks, x, baseHeight + 6, z);
                            }
                        }
                    }
                }
            }

            #endregion


            // Initialize the chunk with the generated blocks
            Chunk chunk = new Chunk(this, chunkX, chunkZ);
            chunk.SetBlocks(blocks);

            return chunk;
        }

        // Function to create a branch
        void CreateBranch(byte[,,] blocks, int startX, int startY, int startZ, int length)
        {
            Random random = new Random();
            int directionX = random.Next(0, 2) == 0 ? -1 : 1; // Randomize branch direction
            int directionZ = random.Next(0, 2) == 0 ? -1 : 1;

            for (int i = 0; i < length; i++)
            {
                int branchX = startX + (i * directionX);
                int branchZ = startZ + (i * directionZ);

                if (branchX >= 0 && branchX < VoxelData.ChunkWidth && branchZ >= 0 && branchZ < VoxelData.ChunkWidth && (startY + i) < VoxelData.ChunkHeight)
                {
                    blocks[branchX, startY + i, branchZ] = 8;

                    // Add leaves around the branch
                    CreateLeafCanopy(blocks, branchX, startY + i, branchZ, 1);
                }
            }
        }

        // Function to create a leaf canopy
        void CreateLeafCanopy(byte[,,] blocks, int centerX, int centerY, int centerZ, int radius = 2)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int leafX = centerX + dx;
                        int leafY = centerY + dy;
                        int leafZ = centerZ + dz;

                        if (leafX >= 0 && leafX < VoxelData.ChunkWidth && leafY >= 0 && leafY < VoxelData.ChunkHeight && leafZ >= 0 && leafZ < VoxelData.ChunkWidth)
                        {
                            // Add leaves in a spherical shape
                            if (Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz) <= radius)
                            {
                                blocks[leafX, leafY, leafZ] = 7;
                            }
                        }
                    }
                }
            }
        }
        public int FindTopBlockAt(byte[,,] blocks, int x, int z)
        {
            for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
            {
                if (blocks[x, y, z] != 0) //not air //will need to change to something else maybe
                {
                    return y;
                }
            }
            // Return -1 if no solid block is found (indicating all blocks in the column are empty)
            return -1;
        }



        public async Task LoadChunksAroundAsync(int centerX, int centerZ, int radius, bool onlyCreateChunk = false)
        {
            List<Task> loadingTasks = new List<Task>();
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int z = centerZ - radius; z <= centerZ + radius; z++)
                {
                    loadingTasks.Add(GetOrCreateChunkAsync(x, z, onlyCreateChunk));
                }
            }
            await Task.WhenAll(loadingTasks);
        }

        //Can we create methods to load and unload chunks and then call a javascript method in the razor page
        //to send the info to javascript?


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
            try
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
            }
            catch (Exception e) { }
            return blocks;
        }

        public async Task<bool> AddBlock(int x, int y, int z, byte blockType)
        {
            //get the chunk
            Vector2 chunkCoords = VoxelData.CalculateChunkPosition(x, z);
            Chunk chunk = await GetOrCreateChunkAsync((int)chunkCoords.X, (int)chunkCoords.Y);

            Vector3 localPos = new Vector3(x, y, z) - new Vector3(chunkCoords.X * VoxelData.ChunkWidth, 0, chunkCoords.Y * VoxelData.ChunkWidth);

            //add the block
            return chunk.AddBlock((int)localPos.X, (int)localPos.Y, (int)localPos.Z, blockType);
        }

        public async Task<bool> RemoveBlock(int x, int y, int z)
        {
            //get the chunk
            Vector2 chunkCoords = VoxelData.CalculateChunkPosition(x, z);
            Chunk chunk = await GetOrCreateChunkAsync((int)chunkCoords.X, (int)chunkCoords.Y);

            //These might need to get converted to local coords
            Vector3 localPos = new Vector3(x, y, z) - new Vector3(chunkCoords.X * VoxelData.ChunkWidth, 0, chunkCoords.Y * VoxelData.ChunkWidth);

            //remove the block
            return chunk.RemoveBlock((int)localPos.X, (int)localPos.Y,(int)localPos.Z);
        }

        public async Task<byte> GetBlockType(int x, int y, int z)
        {
            //get the chunk
            Vector2 chunkCoords = VoxelData.CalculateChunkPosition(x, z);
            Chunk chunk = await GetOrCreateChunkAsync((int)chunkCoords.X, (int)chunkCoords.Y);

            //These might need to get converted to local coords
            Vector3 localPos = new Vector3(x, y, z) - new Vector3(chunkCoords.X * VoxelData.ChunkWidth, 0, chunkCoords.Y * VoxelData.ChunkWidth);

            return chunk.GetBlockType((int)localPos.X, (int)localPos.Y, (int)localPos.Z);
        }
    }
}