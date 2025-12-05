using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services (Make Blazor work)
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// 2. Add Database (The "Hello World" data connection)
// In a real app, we pull this from a secure config file.
// For this test, paste your connection string below.
var connectionString = "Server=SQLxxxx.site4now.net;Database=...;Uid=...;Pwd=...";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// 3. Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// 4. Create the "Hello World" Page
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// --- Simple Database Objects ---
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<LogEntry> LogEntries { get; set; }
}

public class LogEntry
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// --- The UI Component (The "HTML" part) ---
// Usually this is in a separate .razor file, but we can put it here for simplicity!
// We will rely on a basic 'App.razor' structure next.