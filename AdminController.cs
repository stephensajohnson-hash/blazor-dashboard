using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Dashboard;

[Route("api/admin")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
        _context.Links.RemoveRange(_context.Links);
        _context.LinkGroups.RemoveRange(_context.LinkGroups);
        _context.Stocks.RemoveRange(_context.Stocks);
        _context.Countdowns.RemoveRange(_context.Countdowns);
        await _context.SaveChangesAsync();
        return Ok("Database Cleared.");
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "seed.json");

        if (!System.IO.File.Exists(path))
            return NotFound("seed.json not found.");

        try 
        {
            var json = await System.IO.File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rawData = JsonSerializer.Deserialize<JsonRoot>(json, options);

            if (rawData == null) return BadRequest("JSON was null.");

            int groupsAdded = 0;

            // --- LINK GROUPS ---
            if (rawData.LinkGroups != null)
            {
                foreach (var rawGroup in rawData.LinkGroups)
                {
                    var newGroup = new LinkGroup 
                    { 
                        Name = rawGroup.Name, 
                        Color = rawGroup.Color, 
                        IsStatic = rawGroup.IsStatic 
                    };

                    if (rawGroup.Links != null)
                    {
                        foreach (var rawLink in rawGroup.Links)
                        {
                            newGroup.Links.Add(new Link 
                            { 
                                Name = rawLink.Name, 
                                Url = rawLink.Url, 
                                Img = rawLink.Img 
                            });
                        }
                    }
                    _context.LinkGroups.Add(newGroup);
                    groupsAdded++;
                }
            }

            // --- COUNTDOWNS (THE FIX IS HERE) ---
            if (rawData.Countdowns != null)
            {
                foreach (var c in rawData.Countdowns)
                {
                    // PostgreSQL requires dates to be explicitly UTC
                    var utcDate = DateTime.SpecifyKind(c.TargetDate, DateTimeKind.Utc);

                    _context.Countdowns.Add(new Countdown
                    {
                        Name = c.Name,
                        TargetDate = utcDate, // <--- FIXED
                        LinkUrl = c.LinkUrl,
                        Img = c.Img
                    });
                }
            }

            // --- STOCKS ---
            if (rawData.Stocks != null)
            {
                foreach (var s in rawData.Stocks)
                {
                    _context.Stocks.Add(new Stock 
                    { 
                        Symbol = s.Symbol,
                        ImgUrl = "",
                        LinkUrl = "",
                        Shares = 0  // <--- NEW FIX
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok($"Success! Seeded {groupsAdded} groups.");
        }
        catch (Exception ex)
        {
            // Print the inner exception too, it usually hides the real SQL error
            var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            Console.WriteLine($"[SEED ERROR] {msg}");
            return StatusCode(500, msg);
        }
    }

    // ==========================================
    // TEMPORARY CLASSES (DTOs)
    // ==========================================

    public class JsonRoot
    {
        public List<JsonGroup>? LinkGroups { get; set; }
        public List<JsonStock>? Stocks { get; set; }
        public List<JsonCountdown>? Countdowns { get; set; }
    }

    public class JsonGroup
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public string Color { get; set; } = "blue";
        public bool IsStatic { get; set; }
        public List<JsonLink>? Links { get; set; }
    }

    public class JsonLink
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Img { get; set; } = "";
    }

    public class JsonCountdown
    {
        public string? Id { get; set; } 
        public string Name { get; set; } = "";
        public DateTime TargetDate { get; set; }
        public string LinkUrl { get; set; } = "";
        public string Img { get; set; } = "";
    }

    public class JsonStock
    {
        public string Symbol { get; set; } = "";
    }

    // ... inside AdminController class ...

    [HttpPost("fix-ordering")]
    public async Task<IActionResult> FixOrdering()
    {
        // 1. Fix Groups
        var groups = await _context.LinkGroups.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < groups.Count; i++) 
        { 
            // Only update if order is default 0 (collision)
            if (groups.Count > 1 && groups.All(g => g.Order == 0)) 
                groups[i].Order = i; 
        }

        // 2. Fix Links (per group)
        foreach (var group in groups)
        {
            var links = await _context.Links.Where(l => l.LinkGroupId == group.Id).OrderBy(l => l.Id).ToListAsync();
            for (int i = 0; i < links.Count; i++)
            {
                if (links.Count > 1 && links.All(l => l.Order == 0))
                    links[i].Order = i;
            }
        }

        // 3. Fix Stocks
        var stocks = await _context.Stocks.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks.Count > 1 && stocks.All(s => s.Order == 0))
                stocks[i].Order = i;
        }

        // 4. Fix Countdowns
        var countdowns = await _context.Countdowns.OrderBy(x => x.Id).ToListAsync();
        for (int i = 0; i < countdowns.Count; i++)
        {
            if (countdowns.Count > 1 && countdowns.All(c => c.Order == 0))
                countdowns[i].Order = i;
        }

        await _context.SaveChangesAsync();
        return Ok("Ordering Fixed!");
    }

    [HttpPost("upgrade-users")]
    public async Task<IActionResult> UpgradeUsers()
    {
        try 
        {
            // 1. Create Table using Raw SQL (Postgres syntax)
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" serial PRIMARY KEY,
                    ""Username"" text NOT NULL,
                    ""PasswordHash"" text NOT NULL,
                    ""ZipCode"" text NOT NULL,
                    ""AvatarUrl"" text NOT NULL
                );
            ");

            // 2. Create Default Admin User if table is empty
            if (!await _context.Users.AnyAsync())
            {
                var admin = new User
                {
                    Username = "admin",
                    // Default password is 'password' - change this later!
                    PasswordHash = PasswordHelper.HashPassword("password"), 
                    ZipCode = "75482",
                    AvatarUrl = "https://i.imgur.com/7Y5j5Yx.png" // Generic Avatar
                };
                _context.Users.Add(admin);
                await _context.SaveChangesAsync();
                return Ok("Users table created and Default Admin added.");
            }

            return Ok("Users table already exists.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // ... inside AdminController ...

    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromBody] JsonElement json)
    {
        try
        {
            // 1. Create Tables
            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Recipes"" (
                    ""Id"" serial PRIMARY KEY,
                    ""UserId"" integer NOT NULL,
                    ""Title"" text, ""Description"" text, ""Category"" text,
                    ""Servings"" integer, ""PrepTime"" text, ""CookTime"" text,
                    ""ImageUrl"" text, ""SourceUrl"" text, ""SourceDescription"" text,
                    ""TagsJson"" text
                );
                CREATE TABLE IF NOT EXISTS ""Ingredients"" (
                    ""Id"" serial PRIMARY KEY,
                    ""RecipeId"" integer NOT NULL,
                    ""SectionTitle"" text, ""Name"" text, ""Quantity"" text, ""Unit"" text, ""Notes"" text,
                    ""Calories"" double precision, ""Protein"" double precision, ""Carbs"" double precision, ""Fat"" double precision, ""Fiber"" double precision
                );
                CREATE TABLE IF NOT EXISTS ""Instructions"" (
                    ""Id"" serial PRIMARY KEY,
                    ""RecipeId"" integer NOT NULL,
                    ""SectionTitle"" text, ""StepText"" text, ""Order"" integer
                );
                CREATE TABLE IF NOT EXISTS ""RecipeCategories"" (
                    ""Id"" serial PRIMARY KEY, ""UserId"" integer, ""Name"" text
                );
            ");

            // 2. Parse JSON
            // We assume the user sends the raw JSON object { "recipes": [...], "categories": [...] }
            
            // Categories
            if (json.TryGetProperty("categories", out var cats))
            {
                foreach (var cat in cats.EnumerateArray())
                {
                    if (!await _context.RecipeCategories.AnyAsync(c => c.Name == cat.GetString()))
                    {
                        _context.RecipeCategories.Add(new RecipeCategory { UserId = 1, Name = cat.GetString() });
                    }
                }
            }

            // Recipes
            if (json.TryGetProperty("recipes", out var recipes))
            {
                foreach (var r in recipes.EnumerateArray())
                {
                    // Basic Info
                    var newRecipe = new Recipe
                    {
                        UserId = 1, // Default to Admin
                        Title = GetStr(r, "title"),
                        Description = GetStr(r, "description"),
                        Category = GetStr(r, "category"),
                        Servings = r.TryGetProperty("servings", out var s) ? s.GetInt32() : 0,
                        PrepTime = GetStr(r, "prepTime"),
                        CookTime = GetStr(r, "cookTime"),
                        ImageUrl = GetStr(r, "image"),
                        SourceUrl = r.GetProperty("source").GetProperty("url").GetString() ?? "",
                        SourceDescription = r.GetProperty("source").GetProperty("description").GetString() ?? "",
                        TagsJson = r.GetProperty("tags").GetRawText()
                    };

                    _context.Recipes.Add(newRecipe);
                    await _context.SaveChangesAsync(); // Save to get ID

                    // Ingredients
                    if (r.TryGetProperty("ingredients", out var ingList))
                    {
                        foreach (var section in ingList.EnumerateArray())
                        {
                            string secTitle = GetStr(section, "sectionTitle");
                            foreach (var item in section.GetProperty("list").EnumerateArray())
                            {
                                var ing = new Ingredient
                                {
                                    RecipeId = newRecipe.Id,
                                    SectionTitle = secTitle,
                                    Name = GetStr(item, "name"),
                                    Quantity = GetStr(item, "quantity"),
                                    Unit = GetStr(item, "unit"),
                                    Notes = GetStr(item, "notes")
                                };

                                // Macros
                                if (item.TryGetProperty("macros", out var m))
                                {
                                    ing.Calories = GetDouble(m, "calories");
                                    ing.Protein = GetDouble(m, "protein");
                                    ing.Carbs = GetDouble(m, "carbs");
                                    ing.Fat = GetDouble(m, "fat");
                                    ing.Fiber = GetDouble(m, "fiber");
                                }
                                _context.Ingredients.Add(ing);
                            }
                        }
                    }

                    // Instructions
                    if (r.TryGetProperty("instructions", out var instList))
                    {
                        foreach (var section in instList.EnumerateArray())
                        {
                            string secTitle = GetStr(section, "sectionTitle");
                            int order = 0;
                            foreach (var step in section.GetProperty("steps").EnumerateArray())
                            {
                                _context.Instructions.Add(new Instruction
                                {
                                    RecipeId = newRecipe.Id,
                                    SectionTitle = secTitle,
                                    StepText = step.GetString() ?? "",
                                    Order = order++
                                });
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Recipes Imported!");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // Helper functions
    private string GetStr(JsonElement el, string prop) => el.TryGetProperty(prop, out var p) ? p.GetString() ?? "" : "";
    private double GetDouble(JsonElement el, string prop) => el.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDouble() : 0;
}
