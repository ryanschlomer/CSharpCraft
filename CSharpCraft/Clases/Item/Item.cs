namespace CSharpCraft.Clases.Item
{
    public abstract class Item
    {
        public string Name { get; set; }
        public string Type { get; set; } // e.g., "Tool", "Block", "Consumable"
        public int Durability { get; set; }
        public string ModelPath { get; set; } // Path to the 3D model file
        public string TextureAtlas { get; set; }
        public int TextureAtlasPosition { get; set; }
        public int BlockId { get; set; }


        protected Item(string name, string type, int durability, string modelPath, string textureAtlas, int textureAtlasPosition, int blockId)
        {
            Name = name;
            Type = type;
            Durability = durability;
            ModelPath = modelPath;
            TextureAtlas = textureAtlas;
            TextureAtlasPosition = textureAtlasPosition;
            BlockId = blockId;
        }

        public abstract void Use();
    }

    public class EmptyItem : Item
    {
        public EmptyItem() : base("", "Empty", 0, string.Empty, string.Empty, 0, -1)
        {
        }

        public override void Use()
        {
            // Do nothing since it's an empty item
        }
    }
}
