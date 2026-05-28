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

        [HttpGet("dashboard")]
        public async Task<IActionResult> ExportDashboard()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var linkGroups = await db.LinkGroups.AsNoTracking().Include(g => g.Links).ToListAsync();
            var countdowns = await db.Countdowns.AsNoTracking().ToListAsync();
            var stocks = await db.Stocks.AsNoTracking().ToListAsync();

            var payload = new 
            {
                LinkGroups = linkGroups,
                Countdowns = countdowns,
                Stocks = stocks
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options));
            return File(bytes, "application/json", "DashboardExport.json");
        }

        [HttpGet("recipes")]
        public async Task<IActionResult> ExportRecipes()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var recipes = await db.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Include(r => r.Instructions)
                .ToListAsync();

            var categories = await db.RecipeCategories
                .AsNoTracking()
                .ToListAsync();

            var payload = new 
            {
                Recipes = recipes,
                Categories = categories
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options));
            return File(bytes, "application/json", "BulletRecipeExport.json");
        }

        [HttpGet("budget")]
        public async Task<IActionResult> ExportBudget()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var incomeSources = await db.BudgetIncomeSources
                .AsNoTracking()
                .ToListAsync();

            var periods = await db.BudgetPeriods
                .AsNoTracking()
                .Include(p => p.Cycles).ThenInclude(c => c.Items)
                .Include(p => p.Transactions).ThenInclude(t => t.Splits)
                .Include(p => p.Transfers)
                .Include(p => p.ExpectedIncome)
                .Include(p => p.WatchList)
                .AsSplitQuery() 
                .ToListAsync();

            // Explicitly project the data to map names to the IDs so Replit can link them
            var projectedPeriods = periods.Select(p => new 
            {
                p.Id,
                p.DisplayName,
                p.StartDate,
                p.InitialBankBalance,
                Cycles = p.Cycles.Select(c => new 
                {
                    c.Id,
                    c.Label,
                    c.CycleNumber,
                    Items = c.Items.Select(i => new 
                    {
                        i.Id,
                        i.Name,
                        i.ImgUrl,
                        i.PlannedAmount,
                        i.CarriedOver
                    }).ToList()
                }).ToList(),
                
                Transactions = p.Transactions.Select(t => new 
                {
                    t.Id,
                    t.Date,
                    t.Description,
                    t.Amount,
                    t.SourceStringId,
                    SourceName = incomeSources.FirstOrDefault(s => s.StringId == t.SourceStringId)?.Name,
                    t.ResolvedBudgetItemId,
                    LinkedBudgetItemName = p.Cycles.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == t.ResolvedBudgetItemId)?.Name,
                    Splits = t.Splits.Select(s => new 
                    {
                        s.Amount,
                        s.ResolvedBudgetItemId,
                        LinkedBudgetItemName = p.Cycles.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == s.ResolvedBudgetItemId)?.Name
                    }).ToList()
                }).ToList(),

                Transfers = p.Transfers.Select(tr => new 
                {
                    tr.Date,
                    tr.Amount,
                    tr.ResolvedFromId,
                    LinkedFromItemName = p.Cycles.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == tr.ResolvedFromId)?.Name,
                    tr.ResolvedToId,
                    LinkedToItemName = p.Cycles.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == tr.ResolvedToId)?.Name,
                    tr.Note
                }).ToList(),

                ExpectedIncome = p.ExpectedIncome.Select(e => new 
                {
                    e.Id,
                    e.Date,
                    e.Amount,
                    e.SourceStringId,
                    SourceName = incomeSources.FirstOrDefault(s => s.StringId == e.SourceStringId)?.Name
                }).ToList(),

                WatchList = p.WatchList.Select(w => new 
                {
                    w.Id,
                    w.Description,
                    w.Amount,
                    w.DueDate,
                    w.ResolvedBudgetItemId,
                    LinkedBudgetItemName = p.Cycles.SelectMany(c => c.Items).FirstOrDefault(i => i.Id == w.ResolvedBudgetItemId)?.Name
                }).ToList()
            }).ToList();

            var payload = new 
            {
                Periods = projectedPeriods,
                IncomeSources = incomeSources
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options));
            return File(bytes, "application/json", "BulletBudgetExport.json");
        }
    }
}