using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Logging; // IMPORTANT
using Dashboard;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context, ILogger logger)
    {
        try 
        {
            logger.LogInformation("==================================================");
            logger.LogInformation("[DB CHECK] STARTING INITIALIZER...");

            // 1. Check if seed.json exists on the server
            string basePath = AppContext.BaseDirectory;
            string filePath = Path.Combine(basePath, "seed.json");
            
            logger.LogInformation($"[DB CHECK] LOOKING FOR SEED FILE AT: {filePath}");

            if (!File.Exists(filePath))
            {
                 logger.LogError($"[DB ERROR] seed.json NOT FOUND! Did you commit it to GitHub?");
                 // List what files ARE there to help debug
                 var files = Directory.GetFiles(basePath);
                 logger.LogInformation($"[DB DEBUG] Files found in directory: {string.Join(", ", files.Select(Path.GetFileName))}");
                 return;
            }

            // 2. Ensure DB Created
            logger.LogInformation("[DB CHECK] ENSURING DATABASE EXISTS...");
            context.Database.EnsureCreated();

            // 3. Check Data
            int groupCount = context.LinkGroups.Count();
            logger.LogInformation($"[DB CHECK] FOUND {groupCount} LINK GROUPS.");

            if (groupCount > 0)
            {
                logger.LogInformation("[DB CHECK] DATA EXISTS. SKIPPING SEED.");
                return; 
            }

            // 4. Load Data
            logger.LogInformation("[DB CHECK] READING JSON...");
            string jsonString = File.ReadAllText(filePath);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seedData = JsonSerializer.Deserialize<SeedDataWrapper>(jsonString, options);

            if (seedData != null)
            {
                if (seedData.LinkGroups != null) 
                {
                    context.LinkGroups.AddRange(seedData.LinkGroups);
                    logger.LogInformation($"[DB CHECK] ADDING {seedData.LinkGroups.Count} GROUPS.");
                }
                if (seedData.Stocks != null) context.Stocks.AddRange(seedData.Stocks);
                if (seedData.Countdowns != null) context.Countdowns.AddRange(seedData.Countdowns);

                context.SaveChanges();
                logger.LogInformation("[DB CHECK] SEEDING COMPLETE.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DB CRITICAL FAILURE]");
        }
    }

    private class SeedDataWrapper
    {
        public List<LinkGroup>? LinkGroups { get; set; }
        public List<Stock>? Stocks { get; set; }
        public List<Countdown>? Countdowns { get; set; }
    }
}
