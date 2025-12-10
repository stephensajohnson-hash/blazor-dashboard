using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System;

namespace Dashboard.Controllers;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    public AdminController(AppDbContext context) { _context = context; }

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

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        return Ok("Use specific seeders.");
    }

    [HttpPost("fix-ordering")]
    public async Task<IActionResult> FixOrdering()
    {
        var groups = await _context.LinkGroups.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < groups.Count; i++) { if (groups.Count > 1 && groups.All(g => g.Order == 0)) groups[i].Order = i; }

        foreach (var group in groups)
        {
            var links = await _context.Links.Where(l => l.LinkGroupId == group.Id).OrderBy(l => l.Id).ToListAsync();
            for (int i = 0; i < links.Count; i++) { if (links.Count > 1 && links.All(l => l.Order == 0)) links[i].Order = i; }
        }

        var stocks = await _context.Stocks.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < stocks.Count; i++) { if (stocks.Count > 1 && stocks.All(s => s.Order == 0)) stocks[i].Order = i; }

        var countdowns = await _context.Countdowns.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < countdowns.Count; i++) { if (countdowns.Count > 1 && countdowns.All(c => c.Order == 0)) countdowns[i].Order = i; }

        await _context.SaveChangesAsync();
        return Ok("Ordering Fixed!");
    }

    [HttpPost("upgrade-users")]
    public async Task<IActionResult> UpgradeUsers()
    {
        try 
        {
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" serial PRIMARY KEY, ""Username"" text, ""PasswordHash"" text, ""ZipCode"" text, ""AvatarUrl"" text
                );
            ");
            if (!await _context.Users.AnyAsync())
            {
                _context.Users.Add(new User { Username = "admin", PasswordHash = PasswordHelper.HashPassword("password"), ZipCode = "75482" });
                await _context.SaveChangesAsync();
            }
            return Ok("Users table checked.");
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromBody] JsonElement json)
    {
        try
        {
            // 1. Ensure Tables Exist
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Recipes"" (
                    ""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Title"" text, ""Description"" text, ""Category"" text,
                    ""Servings"" integer, ""PrepTime"" text, ""CookTime"" text, ""ImageUrl"" text, 
                    ""SourceName"" text, ""SourceUrl"" text, ""TagsJson"" text
                );
                CREATE TABLE IF NOT EXISTS ""RecipeIngredients"" (
                    ""Id"" serial PRIMARY KEY, ""RecipeId"" integer, ""Section"" text, ""Name"" text, ""Quantity"" text, ""Unit"" text, ""Notes"" text,
                    ""Calories"" double precision, ""Protein"" double precision, ""Carbs"" double precision, ""Fat"" double precision, ""Fiber"" double precision
                );
                CREATE TABLE IF NOT EXISTS ""RecipeInstructions"" (
                    ""Id"" serial PRIMARY KEY, ""RecipeId"" integer, ""Section"" text, ""StepNumber"" integer, ""Text"" text
                );
                CREATE TABLE IF NOT EXISTS ""RecipeCategories"" (
                    ""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Name"" text
                );
            ");

            // 2. Parse Categories
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

            // 3. Parse Recipes
            if (json.TryGetProperty("recipes", out var recipes))
            {
                foreach (var r in recipes.EnumerateArray())
                {
                    string title = GetStr(r, "title");
                    // Skip duplicates based on title to avoid double import
                    if (await _context.Recipes.AnyAsync(x => x.Title == title)) continue;

                    // SAFE PARSING LOGIC
                    string srcName = "";
                    string srcUrl = "";
                    
                    if (r.TryGetProperty("source", out var srcObj) && srcObj.ValueKind == JsonValueKind.Object)
                    {
                        srcName = GetStr(srcObj, "description");
                        srcUrl = GetStr(srcObj, "url");
                    }

                    var newRecipe = new Recipe
                    {
                        UserId = 1, 
                        Title = title,
                        Description = GetStr(r, "description"),
                        Category = GetStr(r, "category"),
                        Servings = r.TryGetProperty("servings", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0,
                        PrepTime = GetStr(r, "prepTime"),
                        CookTime = GetStr(r, "cookTime"),
                        ImageUrl = GetStr(r, "image"),
                        SourceName = srcName, // Now safely extracted
                        SourceUrl = srcUrl,   // Now safely extracted
                        TagsJson = r.TryGetProperty("tags", out var t) ? t.GetRawText() : "[]"
                    };

                    _context.Recipes.Add(newRecipe);
                    await _context.SaveChangesAsync();

                    // Ingredients
                    if (r.TryGetProperty("ingredients", out var ingList) && ingList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var section in ingList.EnumerateArray())
                        {
                            string secTitle = GetStr(section, "sectionTitle");
                            if (string.IsNullOrEmpty(secTitle)) secTitle = "Main";

                            if (section.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in list.EnumerateArray())
                                {
                                    var ing = new RecipeIngredient
                                    {
                                        RecipeId = newRecipe.Id,
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
                                }
                            }
                        }
                    }

                    // Instructions
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
                                        RecipeId = newRecipe.Id,
                                        Section = secTitle,
                                        StepNumber = stepNum++,
                                        Text = step.GetString() ?? ""
                                    });
                                }
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Recipes Imported Successfully!");
        }
        catch (Exception ex)
        {
            // Log the inner exception to help debug if it fails again
            var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return StatusCode(500, "Import Error: " + msg);
        }
    }

    // Safely get string, return "" if null/missing
    private string GetStr(JsonElement el, string prop) 
    {
        if (el.TryGetProperty(prop, out var p))
        {
            return p.GetString() ?? "";
        }
        return "";
    }

    // Safely get double, return 0 if null/missing/wrong type
    private double GetDbl(JsonElement el, string prop) 
    {
        if (el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number)
        {
            return p.GetDouble();
        }
        return 0;
    }
}