using Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

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

// 3. AUTO-MIGRATION & SEEDING (The Magic Step)
// This forces the database to create the tables if they don't exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates tables!

    // Seed Data: If no groups exist, add some defaults so you see something cool.
    if (!db.LinkGroups.Any())
    {
        var finance = new LinkGroup { Name = "Finance", SortOrder = 1 };
        var news = new LinkGroup { Name = "News", SortOrder = 2 };
        
        db.LinkGroups.AddRange(finance, news);
        db.SaveChanges(); // Save groups to get IDs

        db.Links.Add(new Link { Title = "My Budget", Url = "https://mint.com", LinkGroupId = finance.Id });
        db.Links.Add(new Link { Title = "Stock Market", Url = "https://finance.yahoo.com", LinkGroupId = finance.Id });
        db.Links.Add(new Link { Title = "CNN", Url = "https://cnn.com", LinkGroupId = news.Id });
        
        db.SaveChanges();
    }
}

// 4. Configure Pipeline
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
