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
        Console.WriteLine("[ADMIN] Wiping Database...");
        _context.Links.RemoveRange(_context.Links);
        _context.LinkGroups.RemoveRange(_context.LinkGroups);
        _context.Stocks.RemoveRange(_context.Stocks);
        _context.Countdowns.RemoveRange(_context.Countdowns);
        await _context.SaveChangesAsync();
        Console.WriteLine("[ADMIN] Database Wiped.");
        return Ok("Database Cleared.");
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        Console.WriteLine("[ADMIN] STARTING SEED PROCESS...");

        // 1. debug path
        var folder = AppContext.BaseDirectory;
        var path = Path.Combine(folder, "seed.json");
        Console.WriteLine($"[ADMIN] Looking for file at: {path}");

        // 2. Check if file exists
        if (!System.IO.File.Exists(path))
        {
            Console.WriteLine("[ADMIN] ERROR: seed.json NOT FOUND!");
            
            // List what IS there to help debug
            var files = Directory.GetFiles(folder);
            Console.WriteLine($"[ADMIN] Files actually found here: {string.Join(", ", files.Select(Path.GetFileName))}");
            
            return NotFound($"seed.json not found. See logs for file list.");
        }

        // 3. Read File
        try 
        {
            var json = await System.IO.File.ReadAllTextAsync(path);
            Console.WriteLine($"[ADMIN] JSON File read. Length: {json.Length} characters.");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SeedDataWrapper>(json, options);

            if (data == null) 
            {
                Console.WriteLine("[ADMIN] ERROR: JSON deserialized to NULL.");
                return BadRequest("JSON was null.");
            }

            // 4. Insert
            int groupCount = 0;
            if (data.LinkGroups != null) 
            {
                _context.LinkGroups.AddRange(data.LinkGroups);
                groupCount = data.LinkGroups.Count;
            }
            if (data.Stocks != null) _context.Stocks.AddRange(data.Stocks);
            if (data.Countdowns != null) _context.Countdowns.AddRange(data.Countdowns);

            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[ADMIN] SUCCESS! Added {groupCount} LinkGroups to DB.");
            return Ok($"Database Seeded with {groupCount} groups.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ADMIN] CRITICAL ERROR: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"[ADMIN] Inner: {ex.InnerException.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    public class SeedDataWrapper
    {
        public List<LinkGroup>? LinkGroups { get; set; }
        public List<Stock>? Stocks { get; set; }
        public List<Countdown>? Countdowns { get; set; }
    }
}
