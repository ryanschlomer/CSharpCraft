using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public static class VoxelData
    {

        public static readonly int ChunkWidth = 16;
        public static readonly int ChunkHeight = 64;
        public static int ChunkViewRadius = 2;

        public static readonly Dictionary<byte, BlockType> BlockTypes = new Dictionary<byte, BlockType>()
        {
            {
                0, new BlockType
                {
                    IsSolid = false, // Air block
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
                    BackFaceTexture = 13,
                    FrontFaceTexture =12,
                    TopFaceTexture =15,
                    BottomFaceTexture = 15,
                    LeftFaceTexture = 13,
                    RightFaceTexture = 13,
                    Scale = new Vector3(20,10,20),
                    Scarcity = 0.05f,
                }
            }
            // Add more block types as needed
        };

        public static readonly int TextureAtlasSizeInBlocks = 4;
        public static float NormalizedBlockTextureSize
        {

            get { return 1f / (float)TextureAtlasSizeInBlocks; }

        }

        public static readonly Vector3[] VoxelVerts = new Vector3[8] {

        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),

    };

        public static readonly Vector3[] FaceChecks = new Vector3[6] {

        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)

    };

        public static readonly int[,] VoxelTris = new int[6, 4] {

        // Back, Front, Top, Bottom, Left, Right

		// 0 1 2 2 1 3
		{0, 3, 1, 2}, // Back Face
		{5, 6, 4, 7}, // Front Face
		{3, 7, 2, 6}, // Top Face
		{1, 5, 0, 4}, // Bottom Face
		{4, 7, 0, 3}, // Left Face
		{1, 2, 5, 6} // Right Face

	};

        public static readonly Vector2[] VoxelUvs = new Vector2[4] {

        new Vector2 (0.0f, 0.0f),
        new Vector2 (0.0f, 1.0f),
        new Vector2 (1.0f, 0.0f),
        new Vector2 (1.0f, 1.0f)

    };

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


    }
}