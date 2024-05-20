using System.Collections.Generic;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class ChunkData
    {
        //not sending these anymore. Need to have the list of blocks for instanced meshes and selecting a block
        //public List<Vector3> Vertices { get; set; }
        //public List<int> Triangles { get; set; }
        //public List<Vector2> Uvs { get; set; }
        public List<BlockInfo> Blocks { get; set; }

        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public string ChunkId { get; set; }

        // Default constructor
        public ChunkData() { }
    }

  
}
