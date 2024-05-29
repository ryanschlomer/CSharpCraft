namespace CSharpCraft.Clases.Item
{
    public abstract class Tool : Item
    {
        public int Damage { get; set; }
        public float Speed { get; set; }

        protected Tool(string name, int durability, string modelPath, string textureAtlas, int textureAtlasPosition, int damage, float speed)
            : base(name, "Tool", durability, modelPath, textureAtlas, textureAtlasPosition, -1)
        {
            Damage = damage;
            Speed = speed;
        }

        public abstract void UseTool();
    }

}
