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

        // NEW SPORTS EXPORT
        [HttpGet("sports")]
        public async Task<IActionResult> ExportSports()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var sportsTypes = new[] { "game", "sports", "match" };

            // Fetch the BulletItems along with the DbSportsDetail 
            // (Which contains LeagueId, SeasonId, HomeTeamId, AwayTeamId, etc.)
            var events = await db.BulletItems
                .AsNoTracking()
                .Where(i => sportsTypes.Contains(i.Type.ToLower()))
                .Include(i => i.DbSportsDetail)
                .Include(i => i.Notes)
                .ToListAsync();

            /* NOTE: If you also want to dump the raw Master tables into this same file 
               and you know your exact DbSet names, you can wrap them in an anonymous object like this:
               
               var payload = new {
                   Games = events,
                   Leagues = await db.Leagues.ToListAsync(),
                   Seasons = await db.Seasons.ToListAsync(),
                   Teams = await db.Teams.ToListAsync()
               };
               
               For now, returning the 'events' array guarantees the build won't fail.
            */

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(events, options));
            return File(bytes, "application/json", "BulletSportsExport.json");
        }
    }
}