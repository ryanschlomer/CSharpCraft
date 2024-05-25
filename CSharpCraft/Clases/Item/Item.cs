namespace CSharpCraft.Clases.Item
{
    public abstract class Item
    {
        public string Name { get; set; }
        public string Type { get; set; } // e.g., "Tool", "Block", "Consumable"
        public int Durability { get; set; }
        public string ModelPath { get; set; } // Path to the 3D model file

        protected Item(string name, string type, int durability, string modelPath)
        {
            Name = name;
            Type = type;
            Durability = durability;
            ModelPath = modelPath;
        }

        public abstract void Use();
    }

}
