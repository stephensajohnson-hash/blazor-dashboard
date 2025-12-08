using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Dashboard; // Ensure this matches your namespace

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        try 
        {
            // 1. Ensure the DB is created
            context.Database.EnsureCreated();

            // 2. DIAGNOSTIC: Force print to console
            int groupCount = context.LinkGroups.Count();
            int stockCount = context.Stocks.Count();

            Console.WriteLine("**************************************************");
            Console.WriteLine($"[DB CHECK] EXISTING DATA FOUND: {groupCount} Groups, {stockCount} Stocks");
            
            if (groupCount > 0)
            {
                Console.WriteLine("[DB CHECK] SKIPPING SEED (Data already exists)");
                Console.WriteLine("**************************************************");
                return; 
            }

            Console.WriteLine("[DB CHECK] DATABASE IS EMPTY. ATTEMPTING TO LOAD seed.json...");

            // 3. Read JSON File
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed.json");
            
            if (!File.Exists(filePath))
            {
                 // Fallback: Try looking in current directory if BaseDirectory fails
                 filePath = "seed.json";
                 if (!File.Exists(filePath))
                 {
                    Console.WriteLine($"[DB ERROR] seed.json NOT FOUND at {Path.GetFullPath(filePath)}");
                    return;
                 }
            }

            string jsonString = File.ReadAllText(filePath);
            
            // 4. Deserialize
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var seedData = JsonSerializer.Deserialize<SeedDataWrapper>(jsonString, options);

            // 5. Add to DB
            if (seedData != null)
            {
                if (seedData.LinkGroups != null) context.LinkGroups.AddRange(seedData.LinkGroups);
                if (seedData.Stocks != null) context.Stocks.AddRange(seedData.Stocks);
                if (seedData.Countdowns != null) context.Countdowns.AddRange(seedData.Countdowns);

                context.SaveChanges();
                Console.WriteLine("[DB CHECK] SUCCESS! DATA LOADED FROM JSON.");
            }
            else 
            {
                Console.WriteLine("[DB ERROR] JSON file was found but returned null data.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB CRITICAL ERROR] {ex.Message}");
            if(ex.InnerException != null) Console.WriteLine($"[Inner] {ex.InnerException.Message}");
        }
        finally
        {
            Console.WriteLine("**************************************************");
            // FORCE the logs to write to Render console immediately
            Console.Out.Flush(); 
        }
    }

    // Helper class to match the JSON structure
    private class SeedDataWrapper
    {
        public List<LinkGroup>? LinkGroups { get; set; }
        public List<Stock>? Stocks { get; set; }
        public List<Countdown>? Countdowns { get; set; }
    }
}
