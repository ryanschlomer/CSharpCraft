using Microsoft.AspNetCore.Mvc;

namespace CSharpCraft.Controllers
{
    [ApiController]
    [Route("api/textures")]
    public class TexturesController : ControllerBase
    {
        [HttpGet()]
        public IActionResult GetTextureAtlasPath()
        {
            var textureAtlasPath = TextureManager.GetTextureAtlasPath(); // Adjust this method to return the path to the texture atlas
            if (textureAtlasPath != null)
            {
                return Ok(textureAtlasPath);
            }
            return NotFound();
        }
    }
}
