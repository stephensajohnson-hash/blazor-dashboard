using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations; 
using System.Security.Cryptography;
using System.Text;

namespace Dashboard;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    
    // --- DASHBOARD & RECIPES ---
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

    // --- NEW CORE ARCHITECTURE ---
    public DbSet<BulletItem> BulletItems { get; set; }
    public DbSet<BulletItemNote> BulletItemNotes { get; set; }
    public DbSet<BulletTaskDetail> BulletTaskDetails { get; set; }

    // --- SPORTS (Required for ManageSports.razor) ---
    public DbSet<BulletLeague> BulletLeagues { get; set; }
    public DbSet<BulletSeason> BulletSeasons { get; set; }
    public DbSet<BulletTeam> BulletTeams { get; set; }

    // --- LEGACY (Keep for now to prevent build errors in old files) ---
    public DbSet<BulletTask> BulletTasks { get; set; }
    public DbSet<BulletMeeting> BulletMeetings { get; set; }
    public DbSet<BulletMedia> BulletMedia { get; set; }
    public DbSet<BulletHabit> BulletHabits { get; set; }
    public DbSet<BulletMacroTracker> BulletMacroTrackers { get; set; }
    public DbSet<BulletMeal> BulletMeals { get; set; }
    public DbSet<BulletWorkout> BulletWorkouts { get; set; }
    public DbSet<BulletHoliday> BulletHolidays { get; set; }
    public DbSet<BulletBirthday> BulletBirthdays { get; set; }
    public DbSet<BulletAnniversary> BulletAnniversaries { get; set; }
    public DbSet<BulletVacation> BulletVacations { get; set; }
    public DbSet<BulletGame> BulletGames { get; set; }
}

// --- UTILITIES ---
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

// --- NEW CORE MODELS ---
public class BulletItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Type { get; set; } = "task"; 
    public string Category { get; set; } = "personal"; 
    public DateTime Date { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public string OriginalStringId { get; set; } = "";
}

public class BulletItemNote
{
    public int Id { get; set; }
    public int BulletItemId { get; set; }
    public string Content { get; set; } = "";
    public string ImgUrl { get; set; } = "";
    public string LinkUrl { get; set; } = "";
    public int Order { get; set; } = 0;
}

public class BulletTaskDetail
{
    [Key]
    public int BulletItemId { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsCompleted { get; set; } = false;
    public string Priority { get; set; } = "Normal";
    public string TicketNumber { get; set; } = "";
    public string TicketUrl { get; set; } = "";
    public DateTime? DueDate { get; set; }
}

// --- SPORTS MODELS ---
public class BulletLeague { public int Id { get; set; } public int UserId { get; set; } public string Name { get; set; } = ""; public string LinkUrl { get; set; } = ""; public string ImgUrl { get; set; } = ""; }
public class BulletSeason { public int Id { get; set; } public int UserId { get; set; } public int BulletLeagueId { get; set; } public string Name { get; set; } = ""; public string LinkUrl { get; set; } = ""; public string ImgUrl { get; set; } = ""; }
public class BulletTeam { public int Id { get; set; } public int UserId { get; set; } public int BulletLeagueId { get; set; } public string Name { get; set; } = ""; public string Abbreviation { get; set; } = ""; public string LogoUrl { get; set; } = ""; public string LinkUrl { get; set; } = ""; public bool IsFavorite { get; set; } = false; }

// --- EXISTING MODELS ---
public class User { public int Id { get; set; } public string Username { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string ZipCode { get; set; } = "75482"; public string AvatarUrl { get; set; } = ""; public int Age { get; set; } = 30; public double HeightInches { get; set; } = 70; public string Gender { get; set; } = "Male"; public string ActivityLevel { get; set; } = "Sedentary"; }
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

// --- LEGACY STUBS (To prevent build errors in old files) ---
public class BulletTask { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; }=""; public DateTime Date { get; set; } public string Category { get; set; }=""; public string Type { get; set; }=""; public bool IsCompleted { get; set; } public string Priority { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public string TicketNumber { get; set; }=""; public string TicketUrl { get; set; }=""; public string Tags { get; set; }=""; public DateTime? DueDate { get; set; } public string? OriginalStringId { get; set; } }
public class BulletMeeting { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; }=""; public DateTime Date { get; set; } public string Category { get; set; }=""; public string Type { get; set; }=""; public string StartTime { get; set; }=""; public int DurationPlanned { get; set; } public int DurationActual { get; set; } public string Location { get; set; }=""; public string MeetingLeader { get; set; }=""; public string Attendees { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public bool IsCompleted { get; set; } public string Tags { get; set; }=""; public string Description { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletMedia { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; }=""; public DateTime Date { get; set; } public string Category { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public int Rating { get; set; } public int ReleaseYear { get; set; } public string Actors { get; set; }=""; public string Tags { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletHabit { public int Id { get; set; } public int UserId { get; set; } public string Title { get; set; }=""; public DateTime Date { get; set; } public string Category { get; set; }=""; public string Type { get; set; }=""; public int StreakCount { get; set; } public int TargetStreak { get; set; } public bool IsCompleted { get; set; } public string Notes { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Tags { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletMacroTracker { public int Id { get; set; } public int UserId { get; set; } public DateTime Date { get; set; } public double Weight { get; set; } public double TDEE { get; set; } public double WaterIntake { get; set; } public string? Notes { get; set; } public string Category { get; set; }=""; public string Type { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletMeal { public int Id { get; set; } public int BulletMacroTrackerId { get; set; } public string? Name { get; set; } public string? MealType { get; set; } public double Calories { get; set; } public double Protein { get; set; } public double Fat { get; set; } public double Carbs { get; set; } public double Fiber { get; set; } }
public class BulletWorkout { public int Id { get; set; } public int BulletMacroTrackerId { get; set; } public string? Description { get; set; } public double CaloriesBurned { get; set; } public int DurationMinutes { get; set; } }
public class BulletHoliday { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; }=""; public string Category { get; set; }=""; public string Title { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public bool IsWorkHoliday { get; set; } public DateTime Date { get; set; } public string? OriginalStringId { get; set; } }
public class BulletBirthday { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; }=""; public string Category { get; set; }=""; public string Title { get; set; }=""; public DateTime Date { get; set; } public int? BirthYear { get; set; } public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletAnniversary { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; }=""; public string Category { get; set; }=""; public string Title { get; set; }=""; public DateTime Date { get; set; } public int? FirstYear { get; set; } public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletVacation { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; }=""; public string Category { get; set; }=""; public string Title { get; set; }=""; public DateTime Date { get; set; } public string GroupId { get; set; }=""; public string Location { get; set; }=""; public string LinkUrl { get; set; }=""; public string ImgUrl { get; set; }=""; public int? ImageId { get; set; } public string Description { get; set; }=""; public string? OriginalStringId { get; set; } }
public class BulletGame { public int Id { get; set; } public int UserId { get; set; } public string Type { get; set; }="sports"; public string Category { get; set; }="sports"; public string Title { get; set; }=""; public DateTime Date { get; set; } public int BulletLeagueId { get; set; } public int BulletSeasonId { get; set; } public int HomeTeamId { get; set; } public int AwayTeamId { get; set; } public int? HomeScore { get; set; } public int? AwayScore { get; set; } public string Status { get; set; }="Scheduled"; public string StartTime { get; set; }=""; public string TvChannel { get; set; }=""; public string? OriginalStringId { get; set; } }