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
                    _context.Stocks.Add(new Stock { Symbol = s.Symbol });
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
}
