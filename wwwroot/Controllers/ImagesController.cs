using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Dashboard;

namespace Dashboard.Controllers
{
    [ApiController]
    [Route("images")]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ImagesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("db/{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            var img = await _db.StoredImages.FindAsync(id);
            if (img == null) return NotFound();
            
            // Serve the byte array as an image file
            return File(img.Data, img.ContentType);
        }
    }
}