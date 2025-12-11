using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Dashboard.Controllers;

[Route("api/images")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly AppDbContext _context;
    public ImagesController(AppDbContext context) { _context = context; }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetImage(int id)
    {
        var img = await _context.StoredImages.FindAsync(id);
        if (img == null) return NotFound();
        return File(img.Data, img.ContentType);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
        if (file.Length > 5 * 1024 * 1024) return BadRequest("File size limit is 5MB.");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var img = new StoredImage
        {
            OriginalName = file.FileName,
            ContentType = file.ContentType,
            Data = memoryStream.ToArray(),
            UploadedAt = DateTime.UtcNow
        };

        _context.StoredImages.Add(img);
        await _context.SaveChangesAsync();

        // Return the URL that can be used to view this image
        var url = $"/api/images/{img.Id}";
        return Ok(new { Id = img.Id, Url = url });
    }
}