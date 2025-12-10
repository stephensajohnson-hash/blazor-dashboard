using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System;

namespace Dashboard.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    public AdminController(AppDbContext context) { _context = context; }

    // --- DIAGNOSTICS ---

    [HttpGet("debug-recipe")]
    public async Task<IActionResult> DebugFirstRecipe()
    {
        var recipe = await _context.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Instructions)
            .FirstOrDefaultAsync();

        if (recipe == null) return Ok("Database is empty.");

        return Ok(new {
            Message = "First Recipe Data Dump",
            RecipeId = recipe.Id,
            Title = recipe.Title,
            IngredientCount = recipe.Ingredients.Count,
            InstructionCount = recipe.Instructions.Count,
            OwnerId = recipe.UserId,
            RawData = recipe
        });
    }

    // --- DESTRUCTIVE ACTIONS ---

    [HttpPost("nuke-recipes")]
    public async Task<IActionResult> NukeRecipes()
    {
        _context.RecipeIngredients.RemoveRange(_context.RecipeIngredients);
        _context.RecipeInstructions.RemoveRange(_context.RecipeInstructions);
        _context.Recipes.RemoveRange(_context.Recipes);
        _context.RecipeCategories.RemoveRange(_context.RecipeCategories);
        await _context.SaveChangesAsync();
        return Ok("All Recipe Data Deleted.");
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
        _context.LinkGroups.RemoveRange(_context.LinkGroups);
        _context.Links.RemoveRange(_context.Links);
        _context.Countdowns.RemoveRange(_context.Countdowns);
        _context.Stocks.RemoveRange(_context.Stocks);
        _context.Recipes.RemoveRange(_context.Recipes); 
        _context.RecipeIngredients.RemoveRange(_context.RecipeIngredients);
        _context.RecipeInstructions.RemoveRange(_context.RecipeInstructions);
        _context.RecipeCategories.RemoveRange(_context.RecipeCategories);
        await _context.SaveChangesAsync();
        return Ok("Database cleared.");
    }

    // --- IMPORT LOGIC ---

    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromBody] JsonElement json)
    {
        // 1. Ensure Tables
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Recipes"" (""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Title"" text, ""Description"" text, ""Category"" text, ""Servings"" integer, ""PrepTime"" text, ""CookTime"" text, ""ImageUrl"" text, ""SourceName"" text, ""SourceUrl"" text, ""TagsJson"" text);
            CREATE TABLE IF NOT EXISTS ""RecipeIngredients"" (""Id"" serial PRIMARY KEY, ""RecipeId"" integer, ""Section"" text, ""Name"" text, ""Quantity"" text, ""Unit"" text, ""Notes"" text, ""Calories"" double precision, ""Protein"" double precision, ""Carbs"" double precision, ""Fat"" double precision, ""Fiber"" double precision);
            CREATE TABLE IF NOT EXISTS ""RecipeInstructions"" (""Id"" serial PRIMARY KEY, ""RecipeId"" integer, ""Section"" text, ""StepNumber"" integer, ""Text"" text);
            CREATE TABLE IF NOT EXISTS ""RecipeCategories"" (""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Name"" text);
        ");

        var logs = new List<string>();
        int addedRecipes = 0;

        try
        {
            // 2. Parse Recipes
            if (json.TryGetProperty("recipes", out var recipes))
            {
                foreach (var r in recipes.EnumerateArray())
                {
                    string title = GetStr(r, "title");
                    if (string.IsNullOrEmpty(title)) continue;
                    
                    // Duplicate Check
                    if (await _context.Recipes.AnyAsync(x => x.Title == title)) 
                    {
                        logs.Add($"Skipped '{title}' (Exists)");
                        continue; 
                    }

                    // Extract Source
                    string srcName = "", srcUrl = "";
                    if (r.TryGetProperty("source", out var srcObj) && srcObj.ValueKind == JsonValueKind.Object)
                    {
                        srcName = GetStr(srcObj, "description");
                        srcUrl = GetStr(srcObj, "url");
                    }

                    // Create Parent
                    var newRecipe = new Recipe
                    {
                        UserId = 1, // Default, will be migrated by UI later
                        Title = title,
                        Description = GetStr(r, "description"),
                        Category = GetStr(r, "category"),
                        Servings = r.TryGetProperty("servings", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0,
                        PrepTime = GetStr(r, "prepTime"),
                        CookTime = GetStr(r, "cookTime"),
                        ImageUrl = GetStr(r, "image"),
                        SourceName = srcName,
                        SourceUrl = srcUrl,
                        TagsJson = r.TryGetProperty("tags", out var t) ? t.GetRawText() : "[]"
                    };

                    _context.Recipes.Add(newRecipe);
                    await _context.SaveChangesAsync(); // Save immediately to generate Id for children

                    int ingCount = 0;
                    int instCount = 0;

                    // INGREDIENTS
                    if (r.TryGetProperty("ingredients", out var ingList) && ingList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var section in ingList.EnumerateArray())
                        {
                            // JSON uses 'sectionTitle', DB uses 'Section'
                            string secTitle = GetStr(section, "sectionTitle");
                            if (string.IsNullOrEmpty(secTitle)) secTitle = "Main";

                            if (section.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in list.EnumerateArray())
                                {
                                    var ing = new RecipeIngredient
                                    {
                                        RecipeId = newRecipe.Id, // Link to Parent
                                        Section = secTitle,
                                        Name = GetStr(item, "name"),
                                        Quantity = GetStr(item, "quantity"),
                                        Unit = GetStr(item, "unit"),
                                        Notes = GetStr(item, "notes")
                                    };

                                    if (item.TryGetProperty("macros", out var m) && m.ValueKind == JsonValueKind.Object)
                                    {
                                        ing.Calories = GetDbl(m, "calories");
                                        ing.Protein = GetDbl(m, "protein");
                                        ing.Carbs = GetDbl(m, "carbs");
                                        ing.Fat = GetDbl(m, "fat");
                                        ing.Fiber = GetDbl(m, "fiber");
                                    }
                                    _context.RecipeIngredients.Add(ing);
                                    ingCount++;
                                }
                            }
                        }
                    }

                    // INSTRUCTIONS
                    if (r.TryGetProperty("instructions", out var instList) && instList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var section in instList.EnumerateArray())
                        {
                            string secTitle = GetStr(section, "sectionTitle");
                            if (string.IsNullOrEmpty(secTitle)) secTitle = "Directions";

                            if (section.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                            {
                                int stepNum = 1;
                                foreach (var step in steps.EnumerateArray())
                                {
                                    _context.RecipeInstructions.Add(new RecipeInstruction
                                    {
                                        RecipeId = newRecipe.Id, // Link to Parent
                                        Section = secTitle,
                                        StepNumber = stepNum++,
                                        Text = step.GetString() ?? ""
                                    });
                                    instCount++;
                                }
                            }
                        }
                    }

                    logs.Add($"Added '{title}' with {ingCount} ingredients and {instCount} steps.");
                    addedRecipes++;
                }
            }

            // Categories
            if (json.TryGetProperty("categories", out var cats))
            {
                foreach (var cat in cats.EnumerateArray())
                {
                    string name = cat.GetString() ?? "";
                    if (!await _context.RecipeCategories.AnyAsync(c => c.Name == name))
                    {
                        _context.RecipeCategories.Add(new RecipeCategory { UserId = 1, Name = name });
                    }
                }
            }

            await _context.SaveChangesAsync(); // Save all children
            return Ok(new { Count = addedRecipes, Logs = logs });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Trace = ex.StackTrace });
        }
    }

    // --- HELPERS ---
    [HttpPost("fix-ordering")] public async Task<IActionResult> FixOrdering() { return Ok("Ok"); } // Stub
    [HttpPost("seed")] public async Task<IActionResult> SeedDatabase() { return Ok("Use specific seeders."); } // Stub

    private string GetStr(JsonElement el, string prop) 
    {
        if (el.TryGetProperty(prop, out var p) && (p.ValueKind == JsonValueKind.String)) return p.GetString() ?? "";
        if (el.TryGetProperty(prop, out var p2) && (p2.ValueKind == JsonValueKind.Number)) return p2.ToString();
        return "";
    }
    private double GetDbl(JsonElement el, string prop) => el.TryGetProperty(prop, out var p) && (p.ValueKind == JsonValueKind.Number) ? p.GetDouble() : 0;

    // ... inside AdminController class ...

    [HttpPost("seed-feeds")]
    public async Task<IActionResult> SeedFeeds()
    {
        // 1. Ensure Table Exists
        await _context.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Feeds"" (
                ""Id"" serial PRIMARY KEY, 
                ""UserId"" integer, 
                ""Name"" text, 
                ""Url"" text, 
                ""Category"" text, 
                ""IsEnabled"" boolean DEFAULT FALSE
            );
        ");

        // 2. The List of Suggested Feeds
        var suggestions = new List<Feed>
        {
            new Feed { Name = "Hacker News", Url = "https://news.ycombinator.com/rss", Category = "Tech" },
            new Feed { Name = "Visual Studio Blog", Url = "https://devblogs.microsoft.com/visualstudio/feed/", Category = "Tech" },
            new Feed { Name = "Dallas Cowboys", Url = "https://www.dallascowboys.com/rss/news", Category = "Sports" },
            new Feed { Name = "Texas Rangers", Url = "https://www.mlb.com/rangers/feeds/news/rss.xml", Category = "Sports" },
            new Feed { Name = "Tesla Stock News", Url = "https://news.google.com/rss/search?q=Tesla+Stock", Category = "Finance" },
            new Feed { Name = "Woot Daily Deals", Url = "https://www.woot.com/blog/feed", Category = "Shopping" }
        };

        // 3. Assign to Admin User (Id 1) if missing
        int userId = 1; 

        foreach (var s in suggestions)
        {
            // Check if this feed URL already exists for this user
            if (!await _context.Feeds.AnyAsync(f => f.UserId == userId && f.Url == s.Url))
            {
                s.UserId = userId;
                s.IsEnabled = false; // Default OFF
                _context.Feeds.Add(s);
            }
        }

        await _context.SaveChangesAsync();
        return Ok("Feeds seeded (default off).");
    }
    
    // Toggle Endpoint
    [HttpPost("toggle-feed/{id}")]
    public async Task<IActionResult> ToggleFeed(int id)
    {
        var feed = await _context.Feeds.FindAsync(id);
        if (feed == null) return NotFound();
        feed.IsEnabled = !feed.IsEnabled;
        await _context.SaveChangesAsync();
        return Ok(feed.IsEnabled);
    }
}