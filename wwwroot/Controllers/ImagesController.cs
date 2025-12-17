using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Dashboard;

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
        
        return File(img.Data, img.ContentType);
    }
}