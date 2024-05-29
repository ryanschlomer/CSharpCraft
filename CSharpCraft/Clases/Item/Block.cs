namespace CSharpCraft.Clases.Item
{
    public class Block : Item
    {
        public int Damage { get; set; }
        public float Speed { get; set; }

        public int BlockId { get; set; }

        public Block(string name, int durability, string modelPath, string textureAtlas, int textureAtlasPosition, int damage, float speed, int blockId)
            : base(name, "Block", durability, modelPath, textureAtlas, textureAtlasPosition, blockId)
        {
            Damage = damage;
            Speed = speed;
            BlockId = blockId;
        }

        public override void Use()
        {
            //Do nothing
        }
    }

}
