using System.Numerics;
using System;

namespace CSharpCraft.Clases
{
    public class World
    {
        public Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();


        public bool CheckVoxel(Vector3 globalPos)
        {
            Vector3 chunkPos = CalculateChunkPosition(globalPos);
            Chunk chunk = chunks[chunkPos];

            if (chunk == null) return false; // No chunk at this position.

            Vector3 localPos = globalPos - chunkPos * VoxelData.ChunkWidth; // Convert global to local chunk position.
            return chunk.IsVoxelSolid(localPos);
        }

        private Vector3 CalculateChunkPosition(Vector3 globalPos)
        {
            // Calculate which chunk the position is in.
            int x = (int)Math.Floor(globalPos.X / VoxelData.ChunkWidth);
            int y = (int)Math.Floor(globalPos.Y / VoxelData.ChunkHeight);
            int z = (int)Math.Floor(globalPos.Z / VoxelData.ChunkWidth);
            return new Vector3(x, y, z);
        }

        public Chunk GetChunk(Vector3 chunkPosition)
        {
            if (!chunks.ContainsKey(chunkPosition))
            {
                // Optionally generate a chunk if it doesn't exist
                chunks[chunkPosition] = GenerateNewChunk(chunkPosition);
            }
            return chunks[chunkPosition];
        }

        public Chunk GenerateNewChunk(Vector3 chunkPosition)
        {
            throw new NotImplementedException();
        }

        
    }
}
