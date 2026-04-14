using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

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
    
    // Bullet Calendar Core
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
    
    // Todo items
    public DbSet<BulletTaskTodoItem> BulletTaskTodoItems { get; set; }
    
    // Health Details
    public DbSet<BulletHealthDetail> BulletHealthDetails { get; set; }
    public DbSet<BulletHealthMeal> BulletHealthMeals { get; set; }
    public DbSet<BulletHealthWorkout> BulletHealthWorkouts { get; set; }

    // Sports Data
    public DbSet<League> Leagues { get; set; }
    public DbSet<Season> Seasons { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<BulletGameDetail> BulletGameDetails { get; set; }

    // Budget Tracker
    public DbSet<BudgetPeriod> BudgetPeriods { get; set; }
    public DbSet<BudgetCycle> BudgetCycles { get; set; }
    public DbSet<BudgetItem> BudgetItems { get; set; }
    public DbSet<BudgetTransaction> BudgetTransactions { get; set; }
    public DbSet<BudgetTransactionSplit> BudgetTransactionSplits { get; set; }
    public DbSet<BudgetTransfer> BudgetTransfers { get; set; }
    public DbSet<BudgetIncomeSource> BudgetIncomeSources { get; set; }
    public DbSet<BudgetExpectedIncome> BudgetExpectedIncome { get; set; }
    public DbSet<BudgetWatchItem> BudgetWatchItems { get; set; }

    // HSA Manager
    public DbSet<HsaReceipt> HsaReceipts { get; set; }
    public DbSet<HsaDisbursement> HsaDisbursements { get; set; }

    // PTO Tracker
    public DbSet<PtoPolicy> PtoPolicies { get; set; }
    public DbSet<PtoEntry> PtoEntries { get; set; }

    //NET Haircut
    public DbSet<NETH_Phone> NETH_Phones { get; set; }
    public DbSet<NETH_Shop> NETH_Shops { get; set; }
    public DbSet<NETH_Stylist> NETH_Stylists { get; set; }
    public DbSet<NETH_Service> NETH_Services { get; set; }
    public DbSet<NETH_Client> NETH_Clients { get; set; }
    public DbSet<NETH_ClientNote> NETH_ClientNotes { get; set; }
    public DbSet<NETH_NoteTag> NETH_NoteTags { get; set; }
    public DbSet<NETH_Media> NETH_MediaItems { get; set; }
    public DbSet<NETH_Appointment> NETH_Appointments { get; set; }
    public DbSet<NETH_Media> NETH_Media { get; set; }
    public DbSet<NETH_AppointmentService> NETH_AppointmentsService { get; set; }
    public DbSet<NETH_Schedule> NETH_Schedules { get; set; }
    public DbSet<NETH_StoredImage> NETH_StoredImages { get; set; } // New NETH Specific Image Store

    // PickPrepPlate
    public DbSet<PPP_User> PPP_Users { get; set; }
    public DbSet<PPP_StoredImage> PPP_StoredImages { get; set; }
    public DbSet<PPP_Owner> PPP_Owners { get; set; }
    public DbSet<PPP_Address> PPP_Addresses { get; set; }
    public DbSet<PPP_Ingredient> PPP_Ingredients { get; set; }
    public DbSet<PPP_Recipe> PPP_Recipes { get; set; }
    public DbSet<PPP_RecipeIngredientGroup> PPP_RecipeIngredientGroups { get; set; }
    public DbSet<PPP_RecipeIngredientMapping> PPP_RecipeIngredientMappings { get; set; }
    public DbSet<PPP_RecipeInstructionGroup> PPP_RecipeInstructionGroups { get; set; }
    public DbSet<PPP_RecipeInstruction> PPP_RecipeInstructions { get; set; }
}

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
    
    [Column("Order")] 
    public int SortOrder { get; set; } = 0; 

    // Navigation Properties
    public virtual BulletTaskDetail? DbTaskDetail { get; set; }
    public virtual BulletMeetingDetail? DbMeetingDetail { get; set; }
    public virtual BulletHabitDetail? DbHabitDetail { get; set; }
    public virtual BulletMediaDetail? DbMediaDetail { get; set; }
    public virtual BulletHolidayDetail? DbHolidayDetail { get; set; }
    public virtual BulletBirthdayDetail? DbBirthdayDetail { get; set; }
    public virtual BulletAnniversaryDetail? DbAnniversaryDetail { get; set; }
    public virtual BulletVacationDetail? DbVacationDetail { get; set; }
    public virtual BulletHealthDetail? DbHealthDetail { get; set; }
    public virtual BulletGameDetail? DbSportsDetail { get; set; }

    public virtual List<BulletItemNote> Notes { get; set; } = new();
    public virtual List<BulletTaskTodoItem> Todos { get; set; } = new();
    public virtual List<BulletHealthMeal> Meals { get; set; } = new();
    public virtual List<BulletHealthWorkout> Workouts { get; set; } = new();
}

public class BulletTaskTodoItem 
{ 
    public int Id { get; set; } 
    public int BulletItemId { get; set; } 
    public string Content { get; set; } = ""; 
    public bool IsCompleted { get; set; } = false; 
    public int Order { get; set; } = 0; 
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
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!;
    public string Status { get; set; } = "Pending"; 
    public bool IsCompleted { get; set; } = false; 
    public string Priority { get; set; } = "Normal"; 
    public string TicketNumber { get; set; } = ""; 
    public string TicketUrl { get; set; } = ""; 
    public DateTime? DueDate { get; set; } 
}

public class BulletMeetingDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!;
    public DateTime? StartTime { get; set; } 
    public int DurationMinutes { get; set; } 
    public int ActualDurationMinutes { get; set; } 
    public bool IsCompleted { get; set; } = false;
}

public class BulletHabitDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public int StreakCount { get; set; } = 0; 
    public string Status { get; set; } = "Active"; 
    public bool IsCompleted { get; set; } = false; 
}

public class BulletMediaDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public int Rating { get; set; } = 0; 
    public int ReleaseYear { get; set; } = 0; 
    public string Tags { get; set; } = ""; 
}

public class BulletHolidayDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public bool IsWorkHoliday { get; set; } = false; 
}

public class BulletBirthdayDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public int? DOB_Year { get; set; } 
}

public class BulletAnniversaryDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public string AnniversaryType { get; set; } = "Other"; 
    public int? FirstYear { get; set; } 
}

public class BulletVacationDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public string VacationGroupId { get; set; } = ""; 
}

public class BulletHealthDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public double WeightLbs { get; set; } 
    public int CalculatedTDEE { get; set; } 
}

public class BulletHealthMeal 
{ 
    public int Id { get; set; } 
    public int BulletItemId { get; set; } 
    public string MealType { get; set; } = "Breakfast"; 
    public string Name { get; set; } = ""; 
    public double Calories { get; set; } 
    public double Protein { get; set; } 
    public double Carbs { get; set; } 
    public double Fat { get; set; } 
    public double Fiber { get; set; } 
}

public class BulletHealthWorkout 
{ 
    public int Id { get; set; } 
    public int BulletItemId { get; set; } 
    public string Name { get; set; } = ""; 
    public double CaloriesBurned { get; set; } 
    public int TimeSpentMinutes { get; set; } 
}

public class BulletGameDetail 
{ 
    [Key, ForeignKey("BulletItem")] 
    public int BulletItemId { get; set; } 
    public virtual BulletItem BulletItem { get; set; } = null!; 
    public int LeagueId { get; set; } 
    public int SeasonId { get; set; } 
    public int HomeTeamId { get; set; } 
    public int AwayTeamId { get; set; } 
    public int HomeScore { get; set; } 
    public int AwayScore { get; set; } 
    public bool IsComplete { get; set; } 
    public DateTime? StartTime { get; set; } 
    public string TvChannel { get; set; } = ""; 
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
    public int WeeklyCalorieDeficitGoal { get; set; } = 3500; 
    public int DailyProteinGoal { get; set; } = 150; 
    public int DailyFatGoal { get; set; } = 70; 
    public int DailyCarbGoal { get; set; } = 200; 
}

public class StoredImage 
{ 
    public int Id { get; set; } 
    public byte[] Data { get; set; } = Array.Empty<byte>(); 
    public string ContentType { get; set; } = "image/jpeg"; 
    public string OriginalName { get; set; } = ""; 
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow; 
}

public class League 
{ 
    public int Id { get; set; } 
    public int UserId { get; set; } 
    public string Name { get; set; } = ""; 
    public string ImgUrl { get; set; } = ""; 
    public string LinkUrl { get; set; } = ""; 
}

public class Season 
{ 
    public int Id { get; set; } 
    public int UserId { get; set; } 
    public int LeagueId { get; set; } 
    public string Name { get; set; } = ""; 
    public string ImgUrl { get; set; } = ""; 
}

public class Team 
{ 
    public int Id { get; set; } 
    public int UserId { get; set; } 
    public int LeagueId { get; set; } 
    public string Name { get; set; } = ""; 
    public string Abbreviation { get; set; } = ""; 
    public string LogoUrl { get; set; } = ""; 
    public bool IsFavorite { get; set; } = false; 
}

public class BudgetPeriod 
{ 
    public int Id { get; set; } 
    public int UserId { get; set; } 
    public string StringId { get; set; } = ""; 
    public string DisplayName { get; set; } = ""; 
    public DateTime StartDate { get; set; } 
    public decimal InitialBankBalance { get; set; } 
    public List<BudgetCycle> Cycles { get; set; } = new(); 
    public List<BudgetTransaction> Transactions { get; set; } = new(); 
    public List<BudgetTransfer> Transfers { get; set; } = new(); 
    public List<BudgetExpectedIncome> ExpectedIncome { get; set; } = new(); 
    public List<BudgetWatchItem> WatchList { get; set; } = new(); 
}

public class BudgetCycle 
{ 
    public int Id { get; set; } 
    public int BudgetPeriodId { get; set; } 
    public int CycleNumber { get; set; } 
    public string Label { get; set; } = ""; 
    public List<BudgetItem> Items { get; set; } = new(); 
}

public class BudgetItem 
{ 
    public int Id { get; set; } 
    public int BudgetCycleId { get; set; } 
    public string StringId { get; set; } = ""; 
    public string Name { get; set; } = ""; 
    public decimal PlannedAmount { get; set; } 
    public decimal CarriedOver { get; set; } 
    public string ImgUrl { get; set; } = ""; 
}

public class BudgetTransaction 
{ 
    public int Id { get; set; } 
    public int BudgetPeriodId { get; set; } 
    public string StringId { get; set; } = ""; 
    public DateTime Date { get; set; } 
    public string Description { get; set; } = ""; 
    public decimal Amount { get; set; } 
    public string Type { get; set; } = "expense"; 
    public string? SourceStringId { get; set; } 
    public string? LinkedBudgetItemStringId { get; set; } 
    public int? ResolvedBudgetItemId { get; set; } 
    public List<BudgetTransactionSplit> Splits { get; set; } = new(); 
}

public class BudgetTransactionSplit 
{ 
    public int Id { get; set; } 
    public int BudgetTransactionId { get; set; } 
    public string LinkedBudgetItemStringId { get; set; } = ""; 
    public decimal Amount { get; set; } 
    public string Note { get; set; } = ""; 
    public int? ResolvedBudgetItemId { get; set; } 
}

public class BudgetTransfer 
{ 
    public int Id { get; set; } 
    public int BudgetPeriodId { get; set; } 
    public DateTime Date { get; set; } 
    public string FromStringId { get; set; } = ""; 
    public string ToStringId { get; set; } = ""; 
    public decimal Amount { get; set; } 
    public string Note { get; set; } = ""; 
    public int? ResolvedFromId { get; set; } 
    public int? ResolvedToId { get; set; } 
}

public class BudgetIncomeSource 
{ 
    public int Id { get; set; } 
    public int UserId { get; set; } 
    public string StringId { get; set; } = ""; 
    public string Name { get; set; } = ""; 
    public string ImgUrl { get; set; } = ""; 
    public decimal? Amount { get; set; } 
}

public class BudgetExpectedIncome 
{ 
    public int Id { get; set; } 
    public int BudgetPeriodId { get; set; } 
    public string SourceStringId { get; set; } = ""; 
    public decimal Amount { get; set; } 
    public DateTime Date { get; set; } 
}

public class BudgetWatchItem 
{ 
    public int Id { get; set; } 
    public int BudgetPeriodId { get; set; } 
    public string Description { get; set; } = ""; 
    public decimal Amount { get; set; } 
    public DateTime? DueDate { get; set; } 
    public int? ResolvedBudgetItemId { get; set; } 
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
public class HsaDisbursement
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TransactionKey { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }
}

public class HsaReceipt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Provider { get; set; }
    public string? Patient { get; set; }
    public decimal Amount { get; set; }
    public string? Type { get; set; }
    public DateTime ServiceDate { get; set; }
    public string? Note { get; set; }
    
    // RELATIONAL LINK
    // If this is NULL, the item is unreimbursed.
    public int? DisbursementId { get; set; }
    
    public int TaxYear { get; set; }
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public static class BulletViewConfig 
{ 
    public const string ImgWidthDay = "25%"; 
    public const string ImgWidthWeek = "20%"; 
    public const string ImgWidthMonth = "15%"; 
}

// --------------------------------------------------------
// PTO Tracker Models
// --------------------------------------------------------

public class PtoPolicy
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int Year { get; set; } // e.g., 2026
    
    // The total amount of new PTO granted this year
    public decimal TotalAllowanceHours { get; set; } 
    
    // The amount rolled over from the previous year
    public decimal CarryOverHours { get; set; } 
    
    // Used to convert hours back into "Days" for the UI (e.g., 8.0)
    public decimal HoursPerWorkDay { get; set; } = 8.0m;

    // How often does the PTO hit the ledger?
    public PtoAccrualInterval Interval { get; set; }
    
    // The date the first accrual hits
    public DateTime AccrualStartDate { get; set; }

    // Navigation property to the ledger entries for this specific year/policy
    public virtual List<PtoEntry> Entries { get; set; } = new();
}

public class PtoEntry
{
    [Key]
    public int Id { get; set; }
    
    public int PtoPolicyId { get; set; }
    [ForeignKey("PtoPolicyId")]
    public virtual PtoPolicy Policy { get; set; } = null!;

    public DateTime Date { get; set; }
    
    public string Description { get; set; } = "";
    
    // Positive for Accruals/Carryover, Negative for Time Off, Zero for Holidays
    public decimal Amount { get; set; }
    
    public PtoEntryType EntryType { get; set; }
    
    public PtoEntryStatus Status { get; set; }
    
    public string Notes { get; set; } = "";
}

public enum PtoAccrualInterval
{
    Annually,
    Monthly,
    SemiMonthly,
    BiWeekly,
    Weekly
}

public enum PtoEntryType
{
    Accrual,
    TimeOff,
    Holiday,
    Adjustment
}

public enum PtoEntryStatus
{
    Planned,
    Requested,
    Approved,
    Taken
}

public class NETH_Phone
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Type { get; set; } = "Mobile";
    public bool IsBadNumber { get; set; }
}

public class NETH_Shop
{
    public int Id { get; set; }
    public string UrlKey { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string BrandColor { get; set; } = "#D2B48C";
    
    // Linked to the new binary image table
    public int? LogoId { get; set; }
    [ForeignKey("LogoId")]
    public virtual NETH_StoredImage? Logo { get; set; }

    public string StripeAccountId { get; set; } = "";
    public int PhoneId { get; set; }
    public virtual NETH_Phone Phone { get; set; } = null!;
    public virtual List<NETH_Stylist> Stylists { get; set; } = new();
}

public class NETH_Stylist
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public virtual NETH_Shop Shop { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Bio { get; set; } = "";
    public bool IsIndependent { get; set; }
    public string StripeAccountId { get; set; } = "";
    public int PhoneId { get; set; }
    public virtual NETH_Phone Phone { get; set; } = null!;
    public virtual List<NETH_Service> Services { get; set; } = new();
    public int? UserId { get; set; }

    // New Avatar Link
    public int? AvatarId { get; set; }
    [ForeignKey("AvatarId")]
    public virtual NETH_StoredImage? Avatar { get; set; }
}

public class NETH_Service
{
    public int Id { get; set; }
    
    public int StylistId { get; set; }

    [ForeignKey("StylistId")]
    public virtual NETH_Stylist? Stylist { get; set; }

    public string Name { get; set; } = "";
    
    public decimal Price { get; set; }
    
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Hexadecimal color code for calendar display (e.g., #3b82f6)
    /// </summary>
    public string Color { get; set; } = "#3b82f6";
}

public class NETH_Client
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public int PhoneId { get; set; }
    public virtual NETH_Phone Phone { get; set; } = null!;
    public int? UserId { get; set; }

    // New Avatar Link
    public int? AvatarId { get; set; }
    [ForeignKey("AvatarId")]
    public virtual NETH_StoredImage? Avatar { get; set; }
}

public class NETH_ClientNote
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int StylistId { get; set; }
    public int? ServiceId { get; set; }
    public int? MediaId { get; set; }
    public NETH_Media? AttachedMedia { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string ShortDescription { get; set; } = "";
    public string LongDescription { get; set; } = "";
    public int? EventYear { get; set; }
    public int? EventMonth { get; set; }
    public int? EventDay { get; set; }
    public List<NETH_NoteTag> Tags { get; set; } = new();
}

public class NETH_Media
{
    public int Id { get; set; }
    public string FileUrl { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UploadedDate { get; set; } = DateTime.Now;
}

public class NETH_NoteTag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string HexColor { get; set; } = "#000000";
}

public class NETH_Appointment
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int StylistId { get; set; }
    public int ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Booked";
    public List<NETH_AppointmentService> SelectedServices { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string Notes { get; set; } = "";
    public bool IsPaid { get; set; }
}

public class NETH_Schedule
{
    public int Id { get; set; }
    public int StylistId { get; set; }
    public int DayOfWeek { get; set; } // 0=Sun, 6=Sat
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsWorkDay { get; set; }
}

public class NETH_AppointmentService
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public int ServiceId { get; set; }
    public decimal PriceAtBooking { get; set; }
    public int DurationAtBooking { get; set; }
}

public class NETH_StoredImage
{
    public int Id { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public string FileName { get; set; } = "";
    public string Category { get; set; } = "General"; // e.g. "Portfolio", "ShopLogo", "UserUpload"
    public string? AltText { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

// PPP Stuff

public class PPP_StoredImage
{
    public int Id { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "image/jpeg";
    public string FileName { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class PPP_Address
{
    public int Id { get; set; }
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string? Apartment { get; set; }
    public string? DeliveryInstructions { get; set; }
}

public class PPP_User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "Customer";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RewardPoints { get; set; } = 0;
    public string? PreferredPlan { get; set; }
    public int? AvatarId { get; set; }
    [ForeignKey("AvatarId")]
    public virtual PPP_StoredImage? Avatar { get; set; }

    // New Address Link
    public int? AddressId { get; set; }
    [ForeignKey("AddressId")]
    public virtual PPP_Address? Address { get; set; }
}

public class PPP_Owner
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Bio { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? LogoId { get; set; }
    [ForeignKey("LogoId")]
    public virtual PPP_StoredImage? Logo { get; set; }

    // Changed from string to AddressId
    public int? AddressId { get; set; }
    [ForeignKey("AddressId")]
    public virtual PPP_Address? Address { get; set; }
}

public class PPP_Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Unit { get; set; }
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbs { get; set; }
    public double Fiber { get; set; }
    
    // Updated to round to 2 decimal places to prevent floating point tails
    [NotMapped]
    public double NetCarbs => Math.Round(Math.Max(0, Carbs - Fiber), 2);

    public int? OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual PPP_Owner? Owner { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PPP_Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int? OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual PPP_Owner? Owner { get; set; }
    
    // Navigation for the groups
    public virtual List<PPP_RecipeIngredientGroup> IngredientGroups { get; set; } = new();
    public virtual List<PPP_RecipeInstructionGroup> InstructionGroups { get; set; } = new();
}

public class PPP_RecipeIngredientGroup
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string GroupName { get; set; } = "Ingredients";
    public int DisplayOrder { get; set; }
    
    public virtual List<PPP_RecipeIngredientMapping> Ingredients { get; set; } = new();
}

public class PPP_RecipeIngredientMapping
{
    public int Id { get; set; }
    public int IngredientGroupId { get; set; }
    
    public int IngredientId { get; set; }
    [ForeignKey("IngredientId")]
    public virtual PPP_Ingredient MasterIngredient { get; set; } = null!;

    public double Quantity { get; set; }
    public string Unit { get; set; } = ""; // Recipe-specific unit (cups, tbsp, etc)
    public string Notes { get; set; } = ""; // "diced", "chopped", etc.
    public int DisplayOrder { get; set; }
}

public class PPP_RecipeInstructionGroup
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string GroupName { get; set; } = "Instructions";
    public int DisplayOrder { get; set; }

    public virtual List<PPP_RecipeInstruction> Instructions { get; set; } = new();
}

public class PPP_RecipeInstruction
{
    public int Id { get; set; }
    public int InstructionGroupId { get; set; }
    public string StepDescription { get; set; } = "";
    public int DisplayOrder { get; set; }
}