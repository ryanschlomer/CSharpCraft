using System.Diagnostics;
using System.Numerics;

namespace CSharpCraft.Clases
{
    public class BlockType
    {

        public string BlockName { get; set; }
        public bool IsSolid { get; set; }

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
}