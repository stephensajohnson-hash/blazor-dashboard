using Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. SERVICES (The Setup)
// =========================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true);

builder.Services.AddHttpClient();

// FIX A: Register Controllers so the Admin API works
builder.Services.AddControllers(); 

// FIX B: Allow the app to talk to itself (for the Admin Buttons)
// Note: When testing locally, you might need to change this URL to localhost
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri("https://dashboard-app-rmm4.onrender.com/") 
});

// Database Connection
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

// =========================================================
// 2. MIDDLEWARE (The Pipeline)
// =========================================================

// Render Proxy Fix
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// =========================================================
// 3. ROUTING (The Map)
// =========================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// FIX C: Actually map the controllers so /api/admin works
app.MapControllers(); 

// =========================================================
// 4. STARTUP TASKS (The Seeder)
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("--- APPLICATION STARTING ---");
        var context = services.GetRequiredService<AppDbContext>();
        
        // This calls your DbInitializer.cs file
        DbInitializer.Initialize(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "--- ERROR IN PROGRAM STARTUP ---");
    }
}

app.Run();
