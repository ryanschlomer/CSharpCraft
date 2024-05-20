using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public static class VoxelData
    {

        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 128;
        public static int ChunkViewRadius = 1; //This needs to be on the player object

        public static readonly int TextureAtlasSizeInBlocks = 4;
        public static float NormalizedBlockTextureSize
        {

            get { return 1f / (float)TextureAtlasSizeInBlocks; }

        }

        public static readonly UVPoint[] VoxelUvs = new UVPoint[4] {

            new UVPoint (0.0f, 0.0f),
            new UVPoint (0.0f, 1.0f),
            new UVPoint (1.0f, 0.0f),
            new UVPoint (1.0f, 1.0f)

        };

        public static readonly Dictionary<byte, BlockType> BlockTypes = new Dictionary<byte, BlockType>()
        {
            {
                0, new BlockType
                {
                    IsSolid = false, // Air block
                    TextureAtlas = "/Graphics/Blocks.png",
                    // Assign texture IDs for each face of the block
                    BackFaceTexture = 0,
                    FrontFaceTexture = 0,
                    TopFaceTexture = 0,
                    BottomFaceTexture = 0,
                    LeftFaceTexture = 0,
                    RightFaceTexture = 0
                }
            },
            {
                1, new BlockType
                {
                    IsSolid = true, // Grass block
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 2,
                    FrontFaceTexture = 2,
                    TopFaceTexture = 7,
                    BottomFaceTexture = 1,
                    LeftFaceTexture = 2,
                    RightFaceTexture = 2
                }
            },
            {
                2, new BlockType
                {
                    IsSolid = true, // Stone block
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 0,
                    FrontFaceTexture = 0,
                    TopFaceTexture = 0,
                    BottomFaceTexture = 0,
                    LeftFaceTexture = 0,
                    RightFaceTexture = 0,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
            {
                 3, new BlockType
                {
                    IsSolid = true, // Brick block
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 11,
                    FrontFaceTexture =11,
                    TopFaceTexture =11,
                    BottomFaceTexture = 11,
                    LeftFaceTexture = 11,
                    RightFaceTexture = 11,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
              {
                 4, new BlockType
                {
                    IsSolid = true, // Oak Plank block
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 4,
                    FrontFaceTexture =4,
                    TopFaceTexture =4,
                    BottomFaceTexture = 4,
                    LeftFaceTexture = 4,
                    RightFaceTexture = 4,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
              {
                 5, new BlockType
                {
                    IsSolid = true, // Furnace block
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 13,
                    FrontFaceTexture =12,
                    TopFaceTexture =15,
                    BottomFaceTexture = 15,
                    LeftFaceTexture = 13,
                    RightFaceTexture = 13,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
               {
                 6, new BlockType
                {
                    IsSolid = true, // Dirt
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 1,
                    FrontFaceTexture =1,
                    TopFaceTexture =1,
                    BottomFaceTexture = 1,
                    LeftFaceTexture = 1,
                    RightFaceTexture = 1,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
               {
                 7, new BlockType
                {
                    IsSolid = true, //Oak Leaves
                    TextureAtlas = "/Graphics/Blocks2.png",
                    BackFaceTexture = 0,
                    FrontFaceTexture =0,
                    TopFaceTexture =0,
                    BottomFaceTexture = 0,
                    LeftFaceTexture = 0,
                    RightFaceTexture = 0,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            },
               {
                 8, new BlockType
                {
                    IsSolid = true, // Oark Trunk
                    TextureAtlas = "/Graphics/Blocks.png",
                    BackFaceTexture = 5,
                    FrontFaceTexture =5,
                    TopFaceTexture =6,
                    BottomFaceTexture = 6,
                    LeftFaceTexture = 5,
                    RightFaceTexture = 5,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            }
            // Add more block types as needed
        };


        //    public static readonly Vector3[] VoxelVerts = new Vector3[8] {

        //    new Vector3(0.0f, 0.0f, 0.0f),
        //    new Vector3(1.0f, 0.0f, 0.0f),
        //    new Vector3(1.0f, 1.0f, 0.0f),
        //    new Vector3(0.0f, 1.0f, 0.0f),
        //    new Vector3(0.0f, 0.0f, 1.0f),
        //    new Vector3(1.0f, 0.0f, 1.0f),
        //    new Vector3(1.0f, 1.0f, 1.0f),
        //    new Vector3(0.0f, 1.0f, 1.0f),

        //};

        public static readonly Vector3[] FaceChecks = new Vector3[6] {

        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)

    };

        //       public static readonly int[,] VoxelTris = new int[6, 4] {

        //       // Back, Front, Top, Bottom, Left, Right

        //	// 0 1 2 2 1 3
        //	{0, 3, 1, 2}, // Back Face
        //	{5, 6, 4, 7}, // Front Face
        //	{3, 7, 2, 6}, // Top Face
        //	{1, 5, 0, 4}, // Bottom Face
        //	{4, 7, 0, 3}, // Left Face
        //	{1, 2, 5, 6} // Right Face

        //};

  

        public static Vector2 CalculateChunkPosition(float globalX, float globalZ)
        {
            // Calculate which chunk the position is in.
            int x = (int)Math.Floor(globalX / VoxelData.ChunkWidth);
            int z = (int)Math.Floor(globalZ / VoxelData.ChunkWidth);
            return new Vector2(x, z);
        }
        public static Vector2 CalculateChunkPosition(Vector2 globalPos)
        {
            // Calculate which chunk the position is in.
            int x = (int)Math.Floor(globalPos.X / VoxelData.ChunkWidth);
            int z = (int)Math.Floor(globalPos.Y / VoxelData.ChunkWidth);
            return new Vector2(x, z);
        }
        public static Vector2 CalculateChunkPosition(Vector3 globalPos)
        {
            // Calculate which chunk the position is in.
            int x = (int)Math.Floor(globalPos.X / VoxelData.ChunkWidth);
            int z = (int)Math.Floor(globalPos.Z / VoxelData.ChunkWidth);


            return new Vector2(x, z);
        }

        public static int GetChunkCoordinate(float position)
        {
            // Adjust position downwards if negative before performing integer division
            return (int)Math.Floor(position / VoxelData.ChunkWidth);
        }

        public static bool DidCameraMoveToNewChunk(Vector3 prevPosition, Vector3 nextPosition)
        {
            Vector2 prevChunkPosition = VoxelData.CalculateChunkPosition(prevPosition);    // Derive chunk coordinates directly from positions
            Vector2 nextChunkPosition = VoxelData.CalculateChunkPosition(nextPosition);

           
            // Check if the camera has moved to a new chunk
            return nextChunkPosition.X != prevChunkPosition.X || nextChunkPosition.Y != prevChunkPosition.Y;
        }


        public static List<UVData> CompileUVData()
        {
            var uvDataList = new List<UVData>();

            foreach (var kvp in VoxelData.BlockTypes)
            {
                var blockType = kvp.Key;
                var block = kvp.Value;

                var uvData = new UVData
                {
                    BlockType = blockType,
                    TextureAtlas = block.TextureAtlas,
                    BlockName   = block.BlockName,
                    IsSolid = block.IsSolid,
                    Faces = new Dictionary<string, UVPoint[]>()
                };

                uvData.Faces["BackFace"] = CalculateUV(block.BackFaceTexture);
                uvData.Faces["FrontFace"] = CalculateUV(block.FrontFaceTexture);
                uvData.Faces["TopFace"] = CalculateUV(block.TopFaceTexture);
                uvData.Faces["BottomFace"] = CalculateUV(block.BottomFaceTexture);
                uvData.Faces["LeftFace"] = CalculateUV(block.LeftFaceTexture);
                uvData.Faces["RightFace"] = CalculateUV(block.RightFaceTexture);

                uvDataList.Add(uvData);
            }

            return uvDataList;
        }

        public static UVPoint[] CalculateUV(int textureIndex)
        {
            int atlasSizeInBlocks = VoxelData.TextureAtlasSizeInBlocks;
            float normalizedBlockTextureSize = VoxelData.NormalizedBlockTextureSize;

            // Calculate the x and y offsets based on the texture index
            float xOffset = (textureIndex % atlasSizeInBlocks) * normalizedBlockTextureSize;
            float yOffset = 1.0f - ((textureIndex / atlasSizeInBlocks + 1) * normalizedBlockTextureSize);

            return new UVPoint[]
            {
        new UVPoint(xOffset, yOffset + normalizedBlockTextureSize),
        new UVPoint(xOffset + normalizedBlockTextureSize, yOffset + normalizedBlockTextureSize),
        new UVPoint(xOffset, yOffset),
        new UVPoint(xOffset + normalizedBlockTextureSize, yOffset)
            };
        }


    }



}