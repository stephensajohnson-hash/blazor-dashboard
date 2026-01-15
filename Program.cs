using Dashboard;
using Dashboard.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Components; 
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Inside Program.cs, at the very top
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// =========================================================
// 1. SERVICES
// =========================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true);

builder.Services.AddControllers(); 
builder.Services.AddHttpClient();

// Self-Referencing HttpClient
builder.Services.AddScoped(sp => 
{
    var navMan = sp.GetRequiredService<NavigationManager>();
    return new HttpClient 
    { 
        BaseAddress = new Uri(navMan.BaseUri) 
    };
});

// Domain Services
builder.Services.AddScoped<BulletBaseService>();
builder.Services.AddScoped<BulletTaskService>();
builder.Services.AddScoped<BulletMeetingService>();
builder.Services.AddScoped<BulletHabitService>();
builder.Services.AddScoped<BulletMediaService>();
builder.Services.AddScoped<BulletHolidayService>();
builder.Services.AddScoped<BulletBirthdayService>();
builder.Services.AddScoped<BulletAnniversaryService>();
builder.Services.AddScoped<BulletVacationService>();
builder.Services.AddScoped<BulletHealthService>();
builder.Services.AddScoped<SportsService>();
builder.Services.AddScoped<BulletSportsService>();
builder.Services.AddScoped<BulletMoviesService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<BulletOrchestratorService>();

// --- DATABASE REGISTRATION ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContextFactory<AppDbContext>(options => 
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContextFactory<AppDbContext>(options => 
        options.UseInMemoryDatabase("TempDb"));
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
// 3. API ROUTES (Image Uploads)
// =========================================================

// --- IMAGE ENDPOINT (DIRECTLY MAPPED) ---
app.MapGet("/db-images/{id}", async (int id, AppDbContext db) =>
{
    var img = await db.StoredImages.FindAsync(id);
    if (img == null) 
    {
        return Results.NotFound();
    }
    return Results.File(img.Data, img.ContentType);
});

// Endpoint to Upload Images: /api/images/upload
app.MapPost("/api/images/upload", async (HttpRequest request, AppDbContext db) =>
{
    if (!request.HasFormContentType) 
    {
        return Results.BadRequest("Not a form upload");
    }
    
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    
    if (file == null || file.Length == 0) 
    {
        return Results.BadRequest("No file found");
    }
    
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    
    var img = new StoredImage
    {
        OriginalName = file.FileName,
        ContentType = file.ContentType,
        Data = ms.ToArray(),
        UploadedAt = DateTime.UtcNow
    };
    
    db.StoredImages.Add(img);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Id = img.Id, Url = $"/api/images/{img.Id}" });
}).DisableAntiforgery();

app.MapControllers(); 

// =========================================================
// 4. MAP UI & START
// =========================================================

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = factory.CreateDbContext();
        
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "--- ERROR IN PROGRAM STARTUP ---");
    }
}

app.Run();