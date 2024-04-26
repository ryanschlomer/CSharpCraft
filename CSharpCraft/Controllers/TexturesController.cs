using Microsoft.AspNetCore.Mvc;

namespace CSharpCraft.Controllers
{
    [ApiController]
    [Route("api/textures")]
    public class TexturesController : ControllerBase
    {
        [HttpGet("{type}")]
        public IActionResult GetTexturePaths(int type)
        {
            var textures = TextureManager.GetTexturePathsByType(type);
            if (textures != null)
            {
                return Ok(textures);
            }
            return NotFound();
        }
    }
}
