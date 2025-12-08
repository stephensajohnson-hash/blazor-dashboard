using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Dashboard; // Update with your actual namespace

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
        // Delete children first to avoid Foreign Key errors
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
            return NotFound($"seed.json not found at {path}");

        var json = await System.IO.File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<SeedDataWrapper>(json, options);

        if (data == null) return BadRequest("JSON was empty or invalid.");

        if (data.LinkGroups != null) _context.LinkGroups.AddRange(data.LinkGroups);
        if (data.Stocks != null) _context.Stocks.AddRange(data.Stocks);
        if (data.Countdowns != null) _context.Countdowns.AddRange(data.Countdowns);

        await _context.SaveChangesAsync();
        return Ok("Database Seeded from JSON.");
    }

    // Helper class for deserialization
    public class SeedDataWrapper
    {
        public List<LinkGroup>? LinkGroups { get; set; }
        public List<Stock>? Stocks { get; set; }
        public List<Countdown>? Countdowns { get; set; }
    }
}
