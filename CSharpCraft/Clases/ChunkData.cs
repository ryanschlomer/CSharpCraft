using System.Collections.Generic;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class ChunkData
    {
        public List<Vector3> Vertices { get; set; }
        public List<int> Triangles { get; set; }
        public List<Vector2> Uvs { get; set; }

        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public string ChunkId { get; set; }

        // Default constructor
        public ChunkData() { }
    }
}
