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

        [HttpGet("tasks")]
        public async Task<IActionResult> ExportTasks()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "task" || i.Type == "Task")
                .Include(i => i.DbTaskDetail)
                .Include(i => i.Todos)
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

        [HttpGet("habits")]
        public async Task<IActionResult> ExportHabits()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "habit" || i.Type == "Habit")
                .Include(i => i.DbHabitDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletHabitExport.json");
        }

        [HttpGet("events")]
        public async Task<IActionResult> ExportEvents()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var eventTypes = new[] { "holiday", "birthday", "anniversary", "vacation" };

            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => eventTypes.Contains(i.Type.ToLower()))
                .Include(i => i.DbHolidayDetail)
                .Include(i => i.DbBirthdayDetail)
                .Include(i => i.DbAnniversaryDetail)
                .Include(i => i.DbVacationDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletEventsExport.json");
        }

        [HttpGet("sports")]
        public async Task<IActionResult> ExportSports()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var sportsTypes = new[] { "game", "sports", "match" };

            var events = await db.BulletItems
                .AsNoTracking()
                .Where(i => sportsTypes.Contains(i.Type.ToLower()))
                .Include(i => i.DbSportsDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            var leagues = await db.Leagues.AsNoTracking().ToListAsync();
            var seasons = await db.Seasons.AsNoTracking().ToListAsync();
            var teams = await db.Teams.AsNoTracking().ToListAsync();

            var payload = new 
            {
                Events = events,
                Leagues = leagues,
                Seasons = seasons,
                Teams = teams
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options));
            return File(bytes, "application/json", "BulletSportsExport.json");
        }

        // NEW HEALTH EXPORT
        [HttpGet("health")]
        public async Task<IActionResult> ExportHealth()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var items = await db.BulletItems
                .AsNoTracking()
                .Where(i => i.Type == "health" || i.Type == "Health")
                .Include(i => i.DbHealthDetail)
                .Include(i => i.Meals)
                .Include(i => i.Workouts)
                .Include(i => i.Notes)
                .ToListAsync();

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, options));
            return File(bytes, "application/json", "BulletHealthExport.json");
        }
    }
}