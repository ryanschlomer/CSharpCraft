namespace CSharpCraft.Clases
{

    public class Block
    {
        public int X {  get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public bool BlockNeedsUpdating { get; set; } = false;

        public BlockType Type { get; set; }
        public static int[][] NeighborOffsets = new int[][]
        {
        new int[] { 0, 1, 0 }, new int[] { 0, -1, 0 },
        new int[] { 1, 0, 0 }, new int[] { -1, 0, 0 },
        new int[] { 0, 0, 1 }, new int[] { 0, 0, -1 }
        };

        public Block(BlockType type, int x, int y, int z)
        {
            Type = type;
            X = x;
            Y = y;
            Z = z;
            BlockNeedsUpdating = true;
        }
    }

    public enum BlockType
    {
        Air = 0,
        Grass = 1,
        Trunk = 2,
        Leaves = 3,
        Dirt = 4,
        Stone = 5
    }
}

   