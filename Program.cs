using Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides; // NEW: Required for Render Proxies
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true); // <--- SHOW ME THE ERROR
builder.Services.AddHttpClient();

// 2. Database Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TempDb"));
}

var app = builder.Build();

// 3. FIX FOR RENDER: Forwarded Headers
// This tells the app "Trust the HTTPS signal from Render's Load Balancer"
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 4. Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection(); // Force HTTPS now that headers are fixed

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); // Turn on the Engine

// 5. Database Seeder (Keep your existing data logic)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.LinkGroups.Any() && File.Exists("seed.json"))
    {
        Console.WriteLine("SEEDING DATABASE...");
        try 
        {
            var jsonContent = File.ReadAllText("seed.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var importData = JsonSerializer.Deserialize<JsonRoot>(jsonContent, options);

            if (importData != null)
            {
                foreach (var g in importData.LinkGroups)
                {
                    var newGroup = new LinkGroup { Name = g.Name, Color = g.Color, IsStatic = g.IsStatic };
                    foreach (var l in g.Links) newGroup.Links.Add(new Link { Name = l.Name, Url = l.Url, Img = l.Img });
                    db.LinkGroups.Add(newGroup);
                }
                foreach (var c in importData.Countdowns)
                {
                    db.Countdowns.Add(new Countdown { Name = c.Name, TargetDate = c.TargetDate, LinkUrl = c.LinkUrl, Img = c.Img });
                }
                foreach (var s in importData.Stocks)
                {
                    db.Stocks.Add(new Stock { Symbol = s.Symbol });
                }
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seeding Error: {ex.Message}");
        }
    }
}

// --- ADD THIS BLOCK ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Run the check/seed
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB ERROR] An error occurred while checking the DB: {ex.Message}");
    }
}
// ----------------------

// Configure the HTTP request pipeline...
// (Your existing middleware code)

app.Run();

// --- Helper Classes ---
class JsonRoot {
    public List<JsonGroup> LinkGroups { get; set; } = new();
    public List<JsonCountdown> Countdowns { get; set; } = new();
    public List<JsonStock> Stocks { get; set; } = new();
}
class JsonGroup {
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public bool IsStatic { get; set; }
    public List<JsonLink> Links { get; set; } = new();
}
class JsonLink {
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Img { get; set; } = "";
}
class JsonCountdown {
    public string Name { get; set; } = "";
    public DateTime TargetDate { get; set; }
    public string LinkUrl { get; set; } = "";
    public string Img { get; set; } = "";
}
class JsonStock {
    public string Symbol { get; set; } = "";
}


