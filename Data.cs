using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization; // Required for JSON parsing

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Existing
    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Countdown> Countdowns { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<User> Users { get; set; }

    // New - Recipes
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Instruction> Instructions { get; set; }
    public DbSet<RecipeCategory> RecipeCategories { get; set; }
}

// --- EXISTING MODELS ---
public class LinkGroup
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "blue";
    public bool IsStatic { get; set; }
    public int Order { get; set; } 
    public List<Link> Links { get; set; } = new();
}

public class Link
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LinkGroupId { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; }
}

public class Countdown
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public DateTime TargetDate { get; set; }
    public string LinkUrl { get; set; } = "";
    public string Img { get; set; } = "";
    public int Order { get; set; }
}

public class Stock
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Symbol { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public double Shares { get; set; }
    public int Order { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string ZipCode { get; set; } = "75482"; 
    public string AvatarUrl { get; set; } = "";
}

// --- NEW RECIPE MODELS ---

public class Recipe
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public int Servings { get; set; }
    public string PrepTime { get; set; } = "";
    public string CookTime { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string SourceDescription { get; set; } = "";
    public string TagsJson { get; set; } = "[]"; // Store tags as JSON array string

    public List<Ingredient> Ingredients { get; set; } = new();
    public List<Instruction> Instructions { get; set; } = new();
}

public class Ingredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string SectionTitle { get; set; } = ""; // e.g. "Sauce"
    public string Name { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Notes { get; set; } = "";
    
    // Macros (Simplified)
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Fiber { get; set; }
}

public class Instruction
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string SectionTitle { get; set; } = "";
    public string StepText { get; set; } = "";
    public int Order { get; set; }
}

public class RecipeCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
}


// --- SECURITY HELPER ---
public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    public static bool VerifyPassword(string password, string storedHash) => HashPassword(password) == storedHash;
}