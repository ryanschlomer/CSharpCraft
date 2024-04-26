public static class TextureManager
{
    private static Dictionary<int, Dictionary<string, string>> texturePaths = new Dictionary<int, Dictionary<string, string>>
    {
        {1, new Dictionary<string, string> {
            {"top", "graphics/GrassTop.jpg"},
            {"side", "graphics/GrassSide.jpg"},
            {"bottom", "graphics/Dirt.jpg"}
        }},
        {2, new Dictionary<string, string> {
            {"top", "graphics/trunk.jpg"},
            {"side", "graphics/trunk.jpg"},
            {"bottom", "graphics/trunk.jpg"}
        }},
         {3, new Dictionary<string, string> {
            {"top", "graphics/leaf.png"},
            {"side", "graphics/leaf.png"},
            {"bottom", "graphics/leaf.png"}
        }},
         {4, new Dictionary<string, string> {
            {"top", "graphics/Dirt.jpg"},
            {"side", "graphics/Dirt.jpg"},
            {"bottom", "graphics/Dirt.jpg"}
        }},
          {5, new Dictionary<string, string> {
            {"top", "graphics/Stone.jpg"},
            {"side", "graphics/Stone.jpg"},
            {"bottom", "graphics/Stone.jpg"}
        }},
        // Add more types as necessary
    };

    public static Dictionary<string, string> GetTexturePathsByType(int type)
    {
        if (texturePaths.ContainsKey(type))
        {
            return texturePaths[type];
        }
        return null;
    }
}
