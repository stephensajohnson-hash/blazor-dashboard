using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dashboard; 
using Dashboard.Services;

namespace Dashboard.Services
{
    [Route("api/[controller]")]
    [ApiController]
    [IgnoreAntiforgeryToken]
    public class ExportController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public ExportController(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet("media")]
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

        [HttpGet("meetings")]
        public async Task<IActionResult> ExportMeetings()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "meeting" || i.Type == "Meeting")
                .Include(i => i.DbMeetingDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletMeetingExport.json");
        }

        // NEW TASK EXPORT
        [HttpGet("tasks")]
        public async Task<IActionResult> ExportTasks()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "task" || i.Type == "Task")
                .Include(i => i.DbTaskDetail) // Brings in status, priority, due date
                .Include(i => i.Todos)        // Brings in sub-checklist items
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletTaskExport.json");
        }
    }
}