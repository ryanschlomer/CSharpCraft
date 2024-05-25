namespace CSharpCraft.Clases.Item
{
    public class Pickaxe : Tool
    {
        public Pickaxe(string name, int durability, string modelPath, int damage, float speed)
            : base(name, durability, modelPath, damage, speed)
        {
        }

        public override void Use()
        {
            UseTool();
        }

        public override void UseTool()
        {
            // Logic for using the pickaxe
            Console.WriteLine($"{Name} is being used with damage {Damage} and speed {Speed}.");
            Durability--;
        }
    }

}
