namespace CSharpCraft.Clases.Item
{
    public abstract class Tool : Item
    {
        public int Damage { get; set; }
        public float Speed { get; set; }

        protected Tool(string name, int durability, string modelPath, int damage, float speed)
            : base(name, "Tool", durability, modelPath)
        {
            Damage = damage;
            Speed = speed;
        }

        public abstract void UseTool();
    }

}
