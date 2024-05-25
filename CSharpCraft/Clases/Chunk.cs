using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class Chunk
    {
        //Need getters and setters for these?
        //int VertexIndex = 0;
        //public List<Vector3> Vertices { get; private set; } = new List<Vector3>();
        //public List<int> Triangles { get; private set; } = new List<int>();
        //public List<Vector2> Uvs { get; private set; } = new List<Vector2>();

        public List<BlockInfo> BlocksInfoList { get; set; } = new List<BlockInfo>();


        byte[,,] Blocks = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

        public int ChunkX { get; private set; }
        public int ChunkZ { get; private set; }
        public string ChunkId { get; private set; }
        private ChunkService chunkService { get; }

        public bool MeshDataChanged { get; set; } = true;
        public bool ChunkFullyLoaded { get; set; } = false;


        public Chunk(ChunkService chunkService, int chunkX, int chunkZ)
        {
            this.chunkService = chunkService;

            ChunkX = chunkX;
            ChunkZ = chunkZ;


            CreateChunkId();
            //CreateMeshData();
        }

        private void CreateChunkId()
        {
            // Create the ID by padding the ChunkX and ChunkZ values
            ChunkId = $"{FormatCoordinate(ChunkX)}{FormatCoordinate(ChunkZ)}";
        }

        private string FormatCoordinate(int coordinate)
        {
            // Format the coordinate with a sign (+/-) and pad it to 5 digits
            return string.Format("{0:+00000;-00000}", coordinate);
        }

        public void SetBlocks(byte[,,] blocks)
        {
            Blocks = blocks;
            MeshDataChanged = true;
            ChunkFullyLoaded = true;
        }

        //This is setting the whole chunk full of blocks for now
        //public void SetBlocks(byte[,,] blocks)
        //{
        //    This code will eventually be deleted and just fill in the chunk byte[] with what we generated.
        //    For now, this is to troubleshoot
        //    for (int y = 0; y < VoxelData.ChunkHeight; y++)
        //    {
        //        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        //        {
        //            for (int z = 0; z < VoxelData.ChunkWidth; z++)
        //            {
        //                if (y == 0)
        //                    Blocks[x, y, z] = 2; // Set only the center bottom block to type 2
        //                else if (y < VoxelData.ChunkHeight -2)
        //                    Blocks[x, y, z] = 1;
        //                else
        //                    Blocks[x, y, z] = 0; // All other blocks are set to type 0
        //            }
        //        }
        //    }
        //    MeshDataChanged = true;
        //    ChunkFullyLoaded = true;
        //}



        public void CreateMeshData()
        {
            BlocksInfoList.Clear();

            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        AddBlockInfoDataToChunk(new Vector3(x, y, z));

                    }
                }
            }
        }
        private void AddBlockInfoDataToChunk(Vector3 pos)
        {
            if (VoxelData.BlockTypes[Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z]] == VoxelData.BlockTypes[0])
                return; //don't add air block
            for (int p = 0; p < 6; p++)
            {

                if (!IsVoxelSolid(pos + VoxelData.FaceChecks[p]))
                {
                    byte blockID = Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];

                    BlockInfo block = new BlockInfo
                    {
                        BlockId = blockID,
                        Position = new Position(pos.X, pos.Y, pos.Z),
                    };
                    BlocksInfoList.Add(block);
                }
                else
                {
                    //the block is solid; we might have to eventually do something in the future,
                    ////like when we are adding blocks.
                }
            }

        }

        //VertexIndex = 0;
        //Vertices.Clear();
        //Triangles.Clear();
        //Uvs.Clear();

        //for (int y = 0; y < VoxelData.ChunkHeight; y++)
        //{
        //    for (int x = 0; x < VoxelData.ChunkWidth; x++)
        //    {
        //        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        //        {

        //            AddVoxelDataToChunk(new Vector3(x, y, z));

        //        }
        //    }
        //}

   // }

        public bool IsVoxelSolid(Vector3 localPos)
        {
            if (localPos.Y < 0 || localPos.Y >= VoxelData.ChunkHeight)
                //out of bounds below or above chunk
                //We can render them or not. We can prevent the camera from going above or below these levels and return true then.
                return false;
            if (localPos.X < 0 || localPos.X >= VoxelData.ChunkWidth ||
                localPos.Z < 0 || localPos.Z >= VoxelData.ChunkWidth)
                //check a block in adjacent chunk
                return chunkService.IsVoxelExposed(ConvertLocalCoordsToGlobalCoords(localPos)).Result;
            else
            {
                //return IsSolid on the block
                return VoxelData.BlockTypes[Blocks[(int)localPos.X, (int)localPos.Y, (int)localPos.Z]].IsSolid;
            }
        }

        public Vector3 ConvertLocalCoordsToGlobalCoords(Vector3 localPos)
        {
            return new Vector3(localPos.X + ChunkX * VoxelData.ChunkWidth, localPos.Y, localPos.Z + ChunkZ * VoxelData.ChunkWidth);
        }


        //async void AddVoxelDataToChunk(Vector3 pos)
        //{
        //    if (VoxelData.BlockTypes[Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z]] == VoxelData.BlockTypes[0])
        //        return; //don't add air block
        //    for (int p = 0; p < 6; p++)
        //    {

        //        if(!IsVoxelSolid(pos+ VoxelData.FaceChecks[p]))
        //        {
        //            byte blockID = Blocks[(int)pos.X, (int)pos.Y, (int)pos.Z];

        //            Vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 0]]);
        //            Vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 1]]);
        //            Vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 2]]);
        //            Vertices.Add(pos + VoxelData.VoxelVerts[VoxelData.VoxelTris[p, 3]]);

        //            AddTexture(VoxelData.BlockTypes[blockID].GetTextureID(p));

        //            Triangles.Add(VertexIndex);
        //            Triangles.Add(VertexIndex + 1);
        //            Triangles.Add(VertexIndex + 2);
        //            Triangles.Add(VertexIndex + 2);
        //            Triangles.Add(VertexIndex + 1);
        //            Triangles.Add(VertexIndex + 3);

        //            VertexIndex += 4;

        //        }
        //        else
        //        {
        //            //the block is solid; we might have to eventually do something in the future,
        //            ////like when we are adding blocks.
        //        }
        //    }

        //}

        //void AddTexture(int textureID)
        //{
        //    // Calculate texture coordinates based on texture ID
        //    float y = textureID / VoxelData.TextureAtlasSizeInBlocks* VoxelData.NormalizedBlockTextureSize;
        //    float x = textureID % VoxelData.TextureAtlasSizeInBlocks* VoxelData.NormalizedBlockTextureSize;

        //    // Normalize texture coordinates

        //    // Adjust y coordinate to match UV coordinate system
        //    y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        //    // Add texture coordinates for each vertex
        //    Uvs.Add(new Vector2(x, y));
        //    Uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        //    Uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        //    Uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
        //}

        public ChunkData GetChunkData()
        {
            if (!ChunkFullyLoaded)
            {
                int x = 0; //just break here for now. blocks aren't set up.
            }
            if (ChunkFullyLoaded)
                if (MeshDataChanged)
                {
                    //this stuff might need to be locked so no other threads can create the mesh at the same time
                    MeshDataChanged = false;
                    CreateMeshData();
                }
            return new ChunkData
            {
                //Vertices = this.Vertices,
                //Triangles = this.Triangles,
                //Uvs = this.Uvs,
                ChunkX = this.ChunkX,
                ChunkZ = this.ChunkZ,
                ChunkId = this.ChunkId,
                Blocks = this.BlocksInfoList
            };
        }


        public Vector3 GetTopSolidBlock(Vector3 pos)
        {
            for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
            {
                if (VoxelData.BlockTypes[Blocks[(int)pos.X, y, (int)pos.Z]].IsSolid)
                {
                    if (y + 1 < VoxelData.ChunkHeight)
                        return new Vector3(pos.X, y + 1, pos.Z);
                }
            }

            // If no solid block is found, return a default position
            return Vector3.Zero;
        }

        internal bool AddBlock(int x, int y, int z, byte blockType)
        {
            //check current block. Make sure there isn't something there that shouldn't be,
            //like a block that isn't air or water eventually.
            //basically, did another block already get added
            if (!ChunkFullyLoaded)
                return false;
            byte block = Blocks[x, y, z];
            if (block == 0) //add block if there's an air block there now
                //This will eventually have to handle other types of non-solid blocks like water and lava
            {
                Blocks[x, y, z] = blockType;
                MeshDataChanged = true;
                return true;
            }
            return false;
        }

        internal bool RemoveBlock(int x, int y, int z)
        {
            if (!ChunkFullyLoaded)
                return false;
            //set block to air
            Blocks[x, y, z] = 0;
            MeshDataChanged = true;
            return true;
        }

        internal byte GetBlockType(int x, int y, int z)
        {
            return Blocks[x, y, z];
        }
    }
}