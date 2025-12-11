using Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. SERVICES
// =========================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true);

// 1a. Register Controllers (Crucial for Image Uploads)
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// 1b. Self-Referencing HttpClient
builder.Services.AddScoped(sp => 
{
    var navMan = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { 
        BaseAddress = new Uri(navMan.BaseUri) 
    };
});

// 1c. Database Connection
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
// 2. MIDDLEWARE
// =========================================================

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// =========================================================
// 3. ROUTING
// =========================================================

// 3a. Map Controllers (Enables /api/images/upload)
app.MapControllers();

// 3b. Map UI
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// =========================================================
// 4. STARTUP TASKS
// =========================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); // Creates StoredImages table if missing
        
        // DbInitializer.Initialize(context, logger); // Uncomment if you have a seeder
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "--- ERROR IN PROGRAM STARTUP ---");
    }
}

app.Run();