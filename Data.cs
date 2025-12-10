using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Countdown> Countdowns { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<User> Users { get; set; }

    // Recipes
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
    public DbSet<RecipeCategory> RecipeCategories { get; set; }
}

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
    public string SourceName { get; set; } = ""; // Fixed: Was missing or named wrong
    public string SourceUrl { get; set; } = "";
    public string TagsJson { get; set; } = "[]"; 

    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<RecipeInstruction> Instructions { get; set; } = new();
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Section { get; set; } = "Main"; // Fixed: Was SectionTitle
    public string Name { get; set; } = "";
    public string Quantity { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Notes { get; set; } = "";
    
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Fiber { get; set; }
}

public class RecipeInstruction
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Section { get; set; } = "Directions"; // Fixed: Was SectionTitle
    public int StepNumber { get; set; }
    public string Text { get; set; } = "";
}

public class RecipeCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
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