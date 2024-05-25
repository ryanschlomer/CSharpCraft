using System.Diagnostics;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class BlockType
    {

        public string BlockName { get; set; }
        public bool IsSolid { get; set; }

        public int Durablitly { get; set; }
        public string TextureAtlas { get; set; }
        //[Header("Texture Values")]
        public int BackFaceTexture { get; set; }
        public int FrontFaceTexture { get; set; }
        public int TopFaceTexture { get; set; }
        public int BottomFaceTexture { get; set; }
        public int LeftFaceTexture { get; set; }
        public int RightFaceTexture { get; set; }

        // Back, Front, Top, Bottom, Left, Right
        public Vector3 Scale { get; set; }
        public float Scarcity { get; set; }

     

        public int GetTextureID(int faceIndex)
        {

            switch (faceIndex)
            {

                case 0:
                    return BackFaceTexture;
                case 1:
                    return FrontFaceTexture;
                case 2:
                    return TopFaceTexture;
                case 3:
                    return BottomFaceTexture;
                case 4:
                    return LeftFaceTexture;
                case 5:
                    return RightFaceTexture;
                default:
                    Console.Write("Error in GetTextureID; invalid face index");
                    return 0;

            }
        }
    }

    public class BlockInfo
    {
        public byte BlockId { get; set; }
        public Position Position { get; set; }
    }

    public class UVPoint
    {
        public float X { get; set; }
        public float Y { get; set; }

        public UVPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public class UVData
    {
        public byte BlockType { get; set; }
        public string TextureAtlas { get; set; }
        public string BlockName { get; set; }
        public bool IsSolid { get; set; }


        public Dictionary<string, UVPoint[]> Faces { get; set; }
    }

    public class BlockUpdate
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public byte BlockType { get; set; }
    }
}