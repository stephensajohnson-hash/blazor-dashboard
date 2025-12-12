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
    public DbSet<Feed> Feeds { get; set; }
    public DbSet<StoredImage> StoredImages { get; set; }

    // Recipes
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<RecipeInstruction> RecipeInstructions { get; set; }
    public DbSet<RecipeCategory> RecipeCategories { get; set; }

    // Bullet Calendar
    public DbSet<BulletItem> BulletItems { get; set; } 
    public DbSet<BulletMedia> BulletMedia { get; set; }
    public DbSet<BulletTask> BulletTasks { get; set; }
    public DbSet<BulletMeeting> BulletMeetings { get; set; }
    public DbSet<BulletHabit> BulletHabits { get; set; } 
    public DbSet<BulletMacroTracker> BulletMacroTrackers { get; set; }
    public DbSet<BulletMeal> BulletMeals { get; set; }
    public DbSet<BulletWorkout> BulletWorkouts { get; set; }
}

// --- BULLET CALENDAR MODELS ---

public class BulletMacroTracker
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    // Core Stats
    public double Weight { get; set; }
    public double TDEE { get; set; } // Total Daily Energy Expenditure
    public double WaterIntake { get; set; } // Ounces/Cups
    
    public string Notes { get; set; } = "";
    public string Category { get; set; } = "health";
    public string Type { get; set; } = "health";
    public string OriginalStringId { get; set; }
}

public class BulletMeal
{
    public int Id { get; set; }
    public int BulletMacroTrackerId { get; set; } // FK
    
    public string Name { get; set; } = "";
    public string MealType { get; set; } = "Snack"; // Breakfast, Lunch, Dinner, Snack
    
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbs { get; set; }
    public double Fiber { get; set; }
}

public class BulletWorkout
{
    public int Id { get; set; }
    public int BulletMacroTrackerId { get; set; } // FK
    
    public string Description { get; set; } = "";
    public double CaloriesBurned { get; set; }
    public int DurationMinutes { get; set; } // Added field
}

public class BulletHabit
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = "";
    public string Category { get; set; } = "personal"; 
    public string Type { get; set; } = "habit";

    public int StreakCount { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;

    public string Notes { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public int? ImageId { get; set; }
    public string Tags { get; set; } = "";
    
    public string? OriginalStringId { get; set; }
}

public class BulletMeeting
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = "";
    public string Description { get; set; } = ""; 
    public string Category { get; set; } = "work"; 
    public string Type { get; set; } = "meeting";
    public string StartTime { get; set; } = ""; 
    public int DurationPlanned { get; set; } 
    public int DurationActual { get; set; } 
    public string Location { get; set; } = ""; 
    public string MeetingLeader { get; set; } = ""; 
    public string Attendees { get; set; } = ""; 
    public string LinkUrl { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public int? ImageId { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string Tags { get; set; } = "";
    public string? OriginalStringId { get; set; }
}

public class BulletTask
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; } 
    public string Category { get; set; } = "personal"; 
    public string Type { get; set; } = "task";
    public bool IsCompleted { get; set; } = false;
    public string Priority { get; set; } = "Normal"; 
    public string LinkUrl { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public int? ImageId { get; set; }
    public string TicketNumber { get; set; } = "";
    public string TicketUrl { get; set; } = "";
    public string Tags { get; set; } = "";
    public string? OriginalStringId { get; set; }
}

public class BulletMedia
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = "Movie";
    public string LinkUrl { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public int? ImageId { get; set; }
    public string Description { get; set; } = "";
    public int Rating { get; set; } = 0;
    public int ReleaseYear { get; set; }
    public string Actors { get; set; } = "";
    public string Tags { get; set; } = "";
    public string? OriginalStringId { get; set; }
}

public class BulletItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "task"; 
    public string Content { get; set; } = ""; 
    public DateTime Date { get; set; }
    public bool IsCompleted { get; set; }
    public int Order { get; set; }
    public string DataJson { get; set; } = "{}"; 
    public string? OriginalStringId { get; set; }
}

// --- SHARED/LEGACY MODELS ---

public class StoredImage
{
    public int Id { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public string OriginalName { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class Recipe
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public int Servings { get; set; }
    public string? ServingSize { get; set; }
    public string PrepTime { get; set; } = "";
    public string CookTime { get; set; } = "";
    public string? ImageUrl { get; set; } 
    public int? ImageId { get; set; }
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string TagsJson { get; set; } = "[]"; 
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<RecipeInstruction> Instructions { get; set; } = new();
}

public class RecipeCategory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; } 
    public int? ImageId { get; set; }
}

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Section { get; set; } = "Main";
    public int SectionOrder { get; set; }
    public int Order { get; set; }
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
    public string Section { get; set; } = "Directions";
    public int SectionOrder { get; set; }
    public int StepNumber { get; set; } 
    public string Text { get; set; } = "";
}

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

public class Feed
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Category { get; set; } = "General";
    public bool IsEnabled { get; set; } = false;
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