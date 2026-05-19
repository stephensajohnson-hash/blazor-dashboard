using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard; // Explicitly imports the root project namespace
using Dashboard.Services;

namespace Dashboard.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ExportController(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpPost("media")]
        public async Task<IActionResult> ExportMedia()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "media" || i.Type == "Media")
                .Include(i => i.DbMediaDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletMediaExport.json");
        }
    }
}