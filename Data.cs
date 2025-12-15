using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    
    public DbSet<LinkGroup> LinkGroups { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Countdown> Countdowns { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    
    public DbSet<Feed> Feeds { get; set; }
    public DbSet<StoredImage> StoredImages { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
    public DbSet<RecipeCategory> RecipeCategories { get; set; }

    // --- 1. THE CORE TABLES ---
    public DbSet<BulletItem> BulletItems { get; set; }
    public DbSet<BulletItemNote> BulletItemNotes { get; set; }

    // --- 2. THE EXTENSION TABLES (1:1 with BulletItem) ---
    public DbSet<BulletTaskDetail> BulletTaskDetails { get; set; }
    // We will add Meetings, Sports, Vacations here later...

}


// --- EXISTING MODELS ---
public class StoredImage { public int Id { get; set; } public byte[] Data { get; set; } = Array.Empty<byte>(); public string ContentType { get; set; } = "image/jpeg"; public string OriginalName { get; set; } = ""; public DateTime UploadedAt { get; set; } = DateTime.UtcNow; }
public class Recipe { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; } = ""; public string Description { get; set; } = ""; public string Category { get; set; } = ""; public int Servings { get; set; } public string? ServingSize { get; set; } public string PrepTime { get; set; } = ""; public string CookTime { get; set; } = ""; public string? ImageUrl { get; set; } public int? ImageId { get; set; } public string SourceName { get; set; } = ""; public string SourceUrl { get; set; } = ""; public string TagsJson { get; set; } = "[]"; public List<RecipeIngredient> Ingredients { get; set; } = new(); public List<RecipeInstruction> Instructions { get; set; } = new(); }
public class RecipeCategory { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public string? ImageUrl { get; set; } public int? ImageId { get; set; } }
public class RecipeIngredient { public int Id { get; set; } public int RecipeId { get; set; } public string Section { get; set; } = "Main"; public int SectionOrder { get; set; } public int Order { get; set; } public string Name { get; set; } = ""; public string Quantity { get; set; } = ""; public string Unit { get; set; } = ""; public string Notes { get; set; } = ""; public double Calories { get; set; } public double Protein { get; set; } public double Carbs { get; set; } public double Fat { get; set; } public double Fiber { get; set; } }
public class RecipeInstruction { public int Id { get; set; } public int RecipeId { get; set; } public string Section { get; set; } = "Directions"; public int SectionOrder { get; set; } public int StepNumber { get; set; } public string Text { get; set; } = ""; }
public class LinkGroup { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public string Color { get; set; } = "blue"; public bool IsStatic { get; set; } public int Order { get; set; } public List<Link> Links { get; set; } = new(); }
public class Link { public int Id { get; set; } public int UserId { get; set; } public int LinkGroupId { get; set; } public string Name { get; set; } = ""; public string Url { get; set; } = ""; public string Img { get; set; } = ""; public int Order { get; set; } }
public class Countdown { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public DateTime TargetDate { get; set; } public string LinkUrl { get; set; } = ""; public string Img { get; set; } = ""; public int Order { get; set; } }
public class Stock { public int Id { get; set; } public int UserId { get; set; } public string Symbol { get; set; } = ""; public string ImgUrl { get; set; } = ""; public string LinkUrl { get; set; } = ""; public double Shares { get; set; } public int Order { get; set; } }
public class Feed { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public string Url { get; set; } = ""; public string Category { get; set; } = "General"; public bool IsEnabled { get; set; } = false; }

public class User { 
    public int Id { get; set; } 
    public string Username { get; set; } = ""; 
    public string PasswordHash { get; set; } = ""; 
    public string ZipCode { get; set; } = "75482"; 
    public string AvatarUrl { get; set; } = ""; 
    // Health Profile
    public int Age { get; set; } = 30;
    public double HeightInches { get; set; } = 70; 
    public string Gender { get; set; } = "Male"; 
    public string ActivityLevel { get; set; } = "Sedentary"; 
}
public static class PasswordHelper { public static string HashPassword(string password) { using var sha256 = SHA256.Create(); var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password)); return Convert.ToBase64String(bytes); } public static bool VerifyPassword(string password, string storedHash) => HashPassword(password) == storedHash; }

// THE BASE ITEM (Everything has these)
public class BulletItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    public string Type { get; set; } = "task"; // task, meeting, sport, etc.
    public string Category { get; set; } = "personal"; // work, health, personal
    
    public DateTime Date { get; set; } // The primary date
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Title { get; set; } = "";
    public string Description { get; set; } = ""; // A short summary
    
    public string ImgUrl { get; set; } = ""; // Main cover image
    public string LinkUrl { get; set; } = ""; // Main action link
    
    public string OriginalStringId { get; set; } = ""; // For importing/deduping
}

// UNIVERSAL NOTES (Any item can have 0 to many of these)
public class BulletItemNote
{
    public int Id { get; set; }
    public int BulletItemId { get; set; } // Foreign Key
    public string Content { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public int Order { get; set; } = 0;
}

// --- TYPE SPECIFIC DETAILS ---

// 1. TASK DETAILS
public class BulletTaskDetail
{
    [Key]
    public int BulletItemId { get; set; } // This is both PK and FK to BulletItem
    
    public string Status { get; set; } = "Pending"; // Pending, Done
    public bool IsCompleted { get; set; } = false;
    public string Priority { get; set; } = "Normal";
    public string TicketNumber { get; set; } = "";
    public string TicketUrl { get; set; } = "";
    public DateTime? DueDate { get; set; }
}

