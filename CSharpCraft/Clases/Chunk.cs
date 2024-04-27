using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpCraft.Clases
{
    public enum ChunkType
    {
        Plains = 0,
        Hills,
        Mountains

    }

    public class Chunk
    {
        public int MaxChunkSize { get; private set; }
        public int MaxChunkHeight { get; private set; }
    
        public int ChunkX { get; }
        public int ChunkZ { get; }
        public Block[] blocks { get; private set; }
        public string ChunkID { get; private set; }  // Chunk ID property

        public ChunkType Type { get; set; }

        public bool ChunkNeedsUpdating { get; set; } = false;

        public Chunk(int chunkX, int chunkZ, int maxChunkSize, int maxChunkHeight)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            MaxChunkSize = maxChunkSize;
            MaxChunkHeight = maxChunkHeight;

            CreateChunkId();
        }

        private void CreateChunkId()
        {
            // Create the ID by padding the ChunkX and ChunkZ values
            ChunkID = $"{FormatCoordinate(ChunkX)}{FormatCoordinate(ChunkZ)}";
        }

        private string FormatCoordinate(int coordinate)
        {
            // Format the coordinate with a sign (+/-) and pad it to 5 digits
            return string.Format("{0:+00000;-00000}", coordinate);
        }

        public void SetBlocks(Block[,,] blocks)
        {
            this.blocks = new Block[MaxChunkSize * MaxChunkSize * MaxChunkHeight];
            for (int x = 0; x < MaxChunkSize; x++)
            {
                for (int y = 0; y < MaxChunkHeight; y++)
                {
                    for (int z = 0; z < MaxChunkSize; z++)
                    {
                        this.blocks[x + z * MaxChunkSize + y * MaxChunkSize * MaxChunkSize] = blocks[x, y, z];
                    }
                }
            }
        }


        public Block GetBlock(int x, int y, int z)
        {
            if (x < 0 || x >= MaxChunkSize || y < 0 || y >= MaxChunkHeight || z < 0 || z >= MaxChunkSize)
                return null;
            return blocks[x + z * MaxChunkSize + y * MaxChunkSize * MaxChunkSize];
        }

        public IEnumerable<(Block block, int index)> GetAllBlocksWithIndex()
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                yield return (blocks[i], i);
            }
        }

        public List<Block> GetBlocksData()
        {
            return blocks.ToList();
        }

    }
}
