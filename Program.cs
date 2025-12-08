using Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// 2. Database Setup (Render PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// If we are on Render, use Postgres.
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback for now so the app starts even without a DB
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TempDb"));
}

var app = builder.Build();

// 3. Configure Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
