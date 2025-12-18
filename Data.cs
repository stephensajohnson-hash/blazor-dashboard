using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    
    // Bullet Calendar
    public DbSet<BulletItem> BulletItems { get; set; }
    public DbSet<BulletItemNote> BulletItemNotes { get; set; }
    public DbSet<BulletTaskDetail> BulletTaskDetails { get; set; }
    public DbSet<BulletMeetingDetail> BulletMeetingDetails { get; set; }
    public DbSet<BulletHabitDetail> BulletHabitDetails { get; set; }
    public DbSet<BulletMediaDetail> BulletMediaDetails { get; set; }
    public DbSet<BulletHolidayDetail> BulletHolidayDetails { get; set; }
    public DbSet<BulletBirthdayDetail> BulletBirthdayDetails { get; set; }
    public DbSet<BulletAnniversaryDetail> BulletAnniversaryDetails { get; set; }
    public DbSet<BulletVacationDetail> BulletVacationDetails { get; set; }
    
    // NEW: Health
    public DbSet<BulletHealthDetail> BulletHealthDetails { get; set; }
    public DbSet<BulletHealthMeal> BulletHealthMeals { get; set; }
    public DbSet<BulletHealthWorkout> BulletHealthWorkouts { get; set; }
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

public class User 
{ 
    public int Id { get; set; } 
    public string Username { get; set; } = ""; 
    public string PasswordHash { get; set; } = ""; 
    public string ZipCode { get; set; } = "75482"; 
    public string AvatarUrl { get; set; } = ""; 
    public int Age { get; set; } = 30; 
    public double HeightInches { get; set; } = 70; 
    public string Gender { get; set; } = "Male"; 
    public string ActivityLevel { get; set; } = "Sedentary";
    
    // NEW: Health Goals
    public int WeeklyCalorieDeficitGoal { get; set; } = 3500; // 1lb fat
    public int DailyProteinGoal { get; set; } = 150;
    public int DailyFatGoal { get; set; } = 70;
    public int DailyCarbGoal { get; set; } = 200;
}

// ... (Keep existing BulletItem, Note, Detail classes: Task, Meeting, Habit, Media, Holiday, Birthday, Anniversary, Vacation) ...
public class BulletItem { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; } = "task"; public string Category { get; set; } = "personal"; public DateTime Date { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public string Title { get; set; } = ""; public string Description { get; set; } = ""; public string ImgUrl { get; set; } = ""; public string LinkUrl { get; set; } = ""; public string OriginalStringId { get; set; } = ""; [Column("Order")] public int SortOrder { get; set; } = 0; }
public class BulletItemNote { public int Id { get; set; } public int BulletItemId { get; set; } public string Content { get; set; } = ""; public string ImgUrl { get; set; } = ""; public string LinkUrl { get; set; } = ""; public int Order { get; set; } = 0; }
public class BulletTaskDetail { [Key] public int BulletItemId { get; set; } public string Status { get; set; } = "Pending"; public bool IsCompleted { get; set; } = false; public string Priority { get; set; } = "Normal"; public string TicketNumber { get; set; } = ""; public string TicketUrl { get; set; } = ""; public DateTime? DueDate { get; set; } }
public class BulletMeetingDetail { [Key] public int BulletItemId { get; set; } public DateTime? StartTime { get; set; } public int DurationMinutes { get; set; } public int ActualDurationMinutes { get; set; } public bool IsCompleted { get; set; } }
public class BulletHabitDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public int StreakCount { get; set; } = 0; public string Status { get; set; } = "Active"; public bool IsCompleted { get; set; } = false; }
public class BulletMediaDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public int Rating { get; set; } = 0; public int ReleaseYear { get; set; } = 0; public string Tags { get; set; } = ""; }
public class BulletHolidayDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public bool IsWorkHoliday { get; set; } = false; }
public class BulletBirthdayDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public int? DOB_Year { get; set; } }
public class BulletAnniversaryDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public string AnniversaryType { get; set; } = "Other"; public int? FirstYear { get; set; } }
public class BulletVacationDetail { [Key, ForeignKey("BulletItem")] public int BulletItemId { get; set; } public virtual BulletItem BulletItem { get; set; } = null!; public string VacationGroupId { get; set; } = ""; }

// NEW: Health Detail (One per day)
public class BulletHealthDetail
{
    [Key, ForeignKey("BulletItem")]
    public int BulletItemId { get; set; }
    public virtual BulletItem BulletItem { get; set; } = null!;
    public double WeightLbs { get; set; }
    public int CalculatedTDEE { get; set; } // Saved at the time of entry
}

// NEW: Meals
public class BulletHealthMeal
{
    public int Id { get; set; }
    public int BulletItemId { get; set; } // FK to Health Item
    public string MealType { get; set; } = "Breakfast"; // Breakfast, Lunch, Dinner, Snack
    public string Name { get; set; } = "";
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Fiber { get; set; }
}

// NEW: Workouts
public class BulletHealthWorkout
{
    public int Id { get; set; }
    public int BulletItemId { get; set; } // FK to Health Item
    public string Name { get; set; } = "";
    public double CaloriesBurned { get; set; }
    public int TimeSpentMinutes { get; set; }
}

// ... (Keep StoredImage, Recipe, LinkGroup, Link, Countdown, Stock, Feed, ViewConfig) ...
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

public static class BulletViewConfig
{
    public const string ImgWidthDay = "25%";
    public const string ImgWidthWeek = "20%";
    public const string ImgWidthMonth = "15%";
}