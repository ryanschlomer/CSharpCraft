namespace CSharpCraft.Clases
{

    public class Block
    {
        public int X {  get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public bool BlockNeedsUpdating { get; set; } = false;

        public int Type { get; set; }
        public static int[][] NeighborOffsets = new int[][]
        {
        new int[] { 0, 1, 0 }, new int[] { 0, -1, 0 },
        new int[] { 1, 0, 0 }, new int[] { -1, 0, 0 },
        new int[] { 0, 0, 1 }, new int[] { 0, 0, -1 }
        };

        public Block(int type, int x, int y, int z)
        {
            Type = type;
            X = x;
            Y = y;
            Z = z;
            BlockNeedsUpdating = true;
        }
    }
}

   