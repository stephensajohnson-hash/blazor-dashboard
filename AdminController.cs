using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Dashboard;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
        _context.Links.RemoveRange(_context.Links);
        _context.LinkGroups.RemoveRange(_context.LinkGroups);
        _context.Stocks.RemoveRange(_context.Stocks);
        _context.Countdowns.RemoveRange(_context.Countdowns);
        await _context.SaveChangesAsync();
        return Ok("Database Cleared.");
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "seed.json");

        if (!System.IO.File.Exists(path))
            return NotFound("seed.json not found.");

        try 
        {
            var json = await System.IO.File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rawData = JsonSerializer.Deserialize<JsonRoot>(json, options);

            if (rawData == null) return BadRequest("JSON was null.");

            int groupsAdded = 0;

            // --- LINK GROUPS ---
            if (rawData.LinkGroups != null)
            {
                foreach (var rawGroup in rawData.LinkGroups)
                {
                    var newGroup = new LinkGroup 
                    { 
                        Name = rawGroup.Name, 
                        Color = rawGroup.Color, 
                        IsStatic = rawGroup.IsStatic 
                    };

                    if (rawGroup.Links != null)
                    {
                        foreach (var rawLink in rawGroup.Links)
                        {
                            newGroup.Links.Add(new Link 
                            { 
                                Name = rawLink.Name, 
                                Url = rawLink.Url, 
                                Img = rawLink.Img 
                            });
                        }
                    }
                    _context.LinkGroups.Add(newGroup);
                    groupsAdded++;
                }
            }

            // --- COUNTDOWNS (THE FIX IS HERE) ---
            if (rawData.Countdowns != null)
            {
                foreach (var c in rawData.Countdowns)
                {
                    // PostgreSQL requires dates to be explicitly UTC
                    var utcDate = DateTime.SpecifyKind(c.TargetDate, DateTimeKind.Utc);

                    _context.Countdowns.Add(new Countdown
                    {
                        Name = c.Name,
                        TargetDate = utcDate, // <--- FIXED
                        LinkUrl = c.LinkUrl,
                        Img = c.Img
                    });
                }
            }

            // --- STOCKS ---
            if (rawData.Stocks != null)
            {
                foreach (var s in rawData.Stocks)
                {
                    _context.Stocks.Add(new Stock 
                    { 
                        Symbol = s.Symbol,
                        ImgUrl = "",
                        LinkUrl = "",
                        Shares = 0  // <--- NEW FIX
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok($"Success! Seeded {groupsAdded} groups.");
        }
        catch (Exception ex)
        {
            // Print the inner exception too, it usually hides the real SQL error
            var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            Console.WriteLine($"[SEED ERROR] {msg}");
            return StatusCode(500, msg);
        }
    }

    // ==========================================
    // TEMPORARY CLASSES (DTOs)
    // ==========================================

    public class JsonRoot
    {
        public List<JsonGroup>? LinkGroups { get; set; }
        public List<JsonStock>? Stocks { get; set; }
        public List<JsonCountdown>? Countdowns { get; set; }
    }

    public class JsonGroup
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public string Color { get; set; } = "blue";
        public bool IsStatic { get; set; }
        public List<JsonLink>? Links { get; set; }
    }

    public class JsonLink
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Img { get; set; } = "";
    }

    public class JsonCountdown
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public DateTime TargetDate { get; set; }
        public string LinkUrl { get; set; } = "";
        public string Img { get; set; } = "";
    }

    public class JsonStock
    {
        public string Symbol { get; set; } = "";
    }

    // ... inside AdminController class ...

    [HttpPost("fix-ordering")]
    public async Task<IActionResult> FixOrdering()
    {
        // 1. Fix Groups
        var groups = await _context.LinkGroups.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < groups.Count; i++) 
        { 
            // Only update if order is default 0 (collision)
            if (groups.Count > 1 && groups.All(g => g.Order == 0)) 
                groups[i].Order = i; 
        }

        // 2. Fix Links (per group)
        foreach (var group in groups)
        {
            var links = await _context.Links.Where(l => l.LinkGroupId == group.Id).OrderBy(l => l.Id).ToListAsync();
            for (int i = 0; i < links.Count; i++)
            {
                if (links.Count > 1 && links.All(l => l.Order == 0))
                    links[i].Order = i;
            }
        }

        // 3. Fix Stocks
        var stocks = await _context.Stocks.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks.Count > 1 && stocks.All(s => s.Order == 0))
                stocks[i].Order = i;
        }

        // 4. Fix Countdowns
        var countdowns = await _context.Countdowns.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < countdowns.Count; i++)
        {
            if (countdowns.Count > 1 && countdowns.All(c => c.Order == 0))
                countdowns[i].Order = i;
        }

        await _context.SaveChangesAsync();
        return Ok("Ordering Fixed!");
    }

    [HttpPost("upgrade-users")]
    public async Task<IActionResult> UpgradeUsers()
    {
        try 
        {
            // 1. Create Table using Raw SQL (Postgres syntax)
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Username"" text NOT NULL,
                    ""PasswordHash"" text NOT NULL,
                    ""ZipCode"" text NOT NULL,
                    ""AvatarUrl"" text NOT NULL
                );
            ");

            // 2. Create Default Admin User if table is empty
            if (!await _context.Users.AnyAsync())
            {
                var admin = new User
                {
                    Username = "admin",
                    // Default password is 'password' - change this later!
                    PasswordHash = PasswordHelper.HashPassword("password"), 
                    ZipCode = "75482",
                    AvatarUrl = "https://i.imgur.com/7Y5j5Yx.png" // Generic Avatar
                };
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();
                return Ok("Users table created and Default Admin added.");
            }

            return Ok("Users table already exists.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
