using Dashboard;
using Dashboard.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. SERVICES
// =========================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true);

// 1a. Register Controllers for Image Upload API
builder.Services.AddControllers();

// 1b. Self-Referencing HttpClient (Adjusted for Prod/Dev)
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

// Render.com Proxy Fix (Crucial for HTTPS redirect)
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

// 3a. Map the API Controllers (Images, Admin)
app.MapControllers();

// 3b. Map the UI
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
        // Ensure the new StoredImages table exists
        context.Database.EnsureCreated(); 
        
        // Run your existing initializer
        DbInitializer.Initialize(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "--- ERROR IN PROGRAM STARTUP ---");
    }
}

app.Run();