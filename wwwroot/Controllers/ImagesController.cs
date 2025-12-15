using Microsoft.AspNetCore.Mvc;
using Dashboard;

[Route("api/images")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ImagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetImage(int id)
    {
        var img = await _db.StoredImages.FindAsync(id);
        if (img == null) return NotFound();
        return File(img.Data, img.ContentType);
    }
}