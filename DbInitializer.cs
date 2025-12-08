using System;
using System.Linq;
using Dashboard; // Make sure this matches your namespace

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // 1. Ensure the DB is created
        context.Database.EnsureCreated();

        // 2. DIAGNOSTIC: Print counts to the Console (Check Render Logs for this!)
        int groupCount = context.LinkGroups.Count();
        int linkCount = context.Links.Count();
        Console.WriteLine($"[DB CHECK] LinkGroups found: {groupCount}");
        Console.WriteLine($"[DB CHECK] Links found: {linkCount}");

        // 3. If data exists, do nothing
        if (context.LinkGroups.Any())
        {
            return; 
        }

        // 4. If empty, ADD DATA (Seeding)
        Console.WriteLine("[DB CHECK] Database is empty. Seeding data...");

        var newsGroup = new LinkGroup { Name = "News", Color = "blue", IsStatic = false };
        var financeGroup = new LinkGroup { Name = "Finance", Color = "green", IsStatic = false };

        context.LinkGroups.AddRange(newsGroup, financeGroup);
        context.SaveChanges(); // Save groups first to get IDs

        var links = new Link[]
        {
            new Link { Name = "CNN", Url = "https://cnn.com", LinkGroupId = newsGroup.Id },
            new Link { Name = "Fox", Url = "https://foxnews.com", LinkGroupId = newsGroup.Id },
            new Link { Name = "Google Finance", Url = "https://google.com/finance", LinkGroupId = financeGroup.Id }
        };

        context.Links.AddRange(links);
        context.SaveChanges();
        
        Console.WriteLine("[DB CHECK] Data seeded successfully.");
    }
}
